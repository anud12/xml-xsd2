use anyhow::{anyhow, Result};
use crate::js_runtime::{create_runtime, create_context};
use rquickjs::{Context, Runtime};
use crate::js_host_api::{install_host_api, Declarations};

// Effect context JS loaded at compile time
fn effect_context_js() -> &'static str {
    include_str!("../js/scripts/effect_context.js")
}

// Simulation template JS loaded at compile time
fn sim_template_js() -> &'static str {
    include_str!("../js/scripts/sim_template.js")
}

// Globals setup JS loaded at compile time
fn globals_script() -> &'static str {
    include_str!("../js/scripts/globals.js")
}

// Module default call JS loaded at compile time
fn module_call_script() -> &'static str {
    include_str!("../js/scripts/module_call.js")
}

// Entity store setup JS loaded at compile time
fn entity_store_script() -> &'static str {
    include_str!("../js/scripts/entity_store.js")
}

fn create_rt_ctx_and_install(_source: &str) -> Result<(Runtime, Context)> {
    let rt = create_runtime()?;
    let ctx = create_context(&rt)?;
    install_host_api(&ctx)?;
    Ok((rt, ctx))
}

/// Patch user JS source to remove `string`, `number` from destructuring params.
fn patch_user_source(source: &str) -> String {
    let result = source
        .replace("({string, number, ...hostApi})", "({...hostApi})")
        .replace("({ string, number, ...hostApi })", "({...hostApi})");

    let result2 = result.replace("({string, ...hostApi})", "({...hostApi})")
                        .replace("({number, ...hostApi})", "({...hostApi})");

    result2
}

pub fn extract_from_source(source: &str) -> Result<Declarations> {
    let (_rt, ctx) = create_rt_ctx_and_install(source)?;

    // Patch user source
    let patched = patch_user_source(&source);

    // Step 1: Set up globals (string, number)
    ctx.with(|c| c.eval::<(), _>(globals_script()))
        .map_err(|e| anyhow!("globals eval error: {}", e))?;

    // Step 2: Transform user source (export default -> var __module_default)
    let transformed = if patched.contains("export default") {
        patched.replace("export default", "var __module_default =")
    } else {
        patched
    };

    // Step 3: Evaluate user module source
    ctx.with(|c| c.eval::<(), _>(transformed))
        .map_err(|e| anyhow!("user source eval error: {}", e))?;

    // Step 4: Call __module_default with hostApi
    ctx.with(|c| c.eval::<(), _>(module_call_script()))
        .map_err(|e| anyhow!("module call eval error: {}", e))?;

    // Step 5: Build entity store from __entityData
    ctx.with(|c| c.eval::<(), _>(entity_store_script()))
        .map_err(|e| anyhow!("entity store eval error: {}", e))?;

    // Step 6: Install effect context
    ctx.with(|c| c.eval::<(), _>(effect_context_js()))
        .map_err(|e| anyhow!("effect context eval error: {}", e))?;

    // Step 7: Execute registered effects and extract declarations
    let extract_script = build_extract_script();
    let json_str = ctx.with(|c| c.eval::<String, _>(extract_script))
        .map_err(|e| anyhow!("extract eval error: {}", e))?;

    if let Ok(dec) = serde_json::from_str::<Declarations>(&json_str) {
        eprintln!("[DEBUG extract_from_source] entity_data JSON: {}", dec.entity_data);
    }

    let dec: Declarations = serde_json::from_str(&json_str)
        .map_err(|e| anyhow!("extract_from_source deserialization error: {}", e))?;

    Ok(dec)
}

/// Extract script template loaded at compile time.
fn extract_script_template() -> &'static str {
    include_str!("../js/scripts/extract_script.js")
}

/// Build the script that executes effects and extracts declarations.
fn build_extract_script() -> String {
    extract_script_template().to_string()
}

fn prepare_runtime_and_ctx() -> Result<(Runtime, Context)> {
    let rt = create_runtime()?;
    let ctx = create_context(&rt)?;
    Ok((rt, ctx))
}

fn select_entry_source(files: &std::collections::HashMap<String, String>) -> String {
    use serde_json::Value;
    for (name, content) in files.iter() {
        if name.ends_with("manifest.json") || (name.to_lowercase().contains("manifest") && name.ends_with(".json")) {
            if let Ok(v) = serde_json::from_str::<Value>(content) {
                if let Some(entry) = v.get("entry").and_then(|v| v.as_str()) {
                    if let Some(src) = files.get(entry).or_else(|| {
                        if let Some(pos) = name.rfind('/') {
                            let dir = &name[..pos];
                            files.get(&format!("{}/{}", dir, entry))
                        } else { None }
                    }) { return src.clone(); }
                }
            }
        }
    }
    if let Some(src) = files.get("index.js") { return src.clone(); }
    if let Some((_k, v)) = files.iter().next() { return v.clone(); }
    "".to_string()
}

fn build_initial_store_json(initial_store: &[Vec<String>]) -> Result<String> {
    use serde_json::Value;
    let mut store_array: Vec<Value> = Vec::new();
    for row in initial_store.iter() {
        if !row.is_empty() {
            let key = row[0].clone();
            let mut map = serde_json::Map::new();
            map.insert("textMap_name".to_string(), Value::String(key.clone()));
            map.insert(key.clone(), Value::String(key.clone()));
            store_array.push(Value::Object(map));
        }
    }
    Ok(serde_json::to_string(&store_array)?)
}

fn run_simulation_and_collect(ctx: &Context, script: &str) -> Result<(String, String)> {
    let r = ctx.with(|c| c.eval::<String, _>(script))?;
    let l = ctx.with(|c| c.eval::<String, _>("JSON.stringify(globalThis.__logs||[])")).unwrap_or_default();
    Ok((r, l))
}

fn convert_store_values(values: &[serde_json::Value]) -> Vec<Vec<String>> {
    let mut store_rows = Vec::new();
    for obj in values.iter() {
        if let Some(map) = obj.as_object() {
            if !map.is_empty() {
                let id = map.get("textMap_name")
                    .and_then(|v| v.as_str())
                    .map(|s| s.to_string())
                    .or_else(|| {
                        map.iter().next().map(|(_, v)| v.to_string())
                    });
                if let Some(s) = id {
                    store_rows.push(vec![s]);
                } else {
                    store_rows.push(vec!["".to_string()]);
                }
            } else {
                store_rows.push(vec!["".to_string()]);
            }
        } else {
            store_rows.push(vec![obj.to_string()]);
        }
    }
    store_rows
}

pub fn simulate_action(
    files: &std::collections::HashMap<String, String>,
    action_name: &str,
    initial_store: &[Vec<String>],
) -> Result<(Vec<String>, Vec<Vec<String>>)> {
    let (_rt, ctx) = prepare_runtime_and_ctx()?;
    install_host_api(&ctx)?;

    // Set up globals.
    let _ = ctx.with(|ctx| ctx.eval::<(), _>(globals_script()));

    let source = select_entry_source(files);
    // Evaluate user source with globals available.
    let transformed = if source.contains("export default") {
        source.replace("export default", "var __module_default =")
    } else {
        source.clone()
    };
    ctx.with(|ctx| ctx.eval::<(), _>(transformed))?;

    // Call __module_default
    let _ = ctx.with(|ctx| ctx.eval::<(), _>(module_call_script()));

    // Build entity store
    let _ = ctx.with(|ctx| ctx.eval::<(), _>(entity_store_script()));

    // Install effect context
    let _ = ctx.with(|ctx| ctx.eval::<(), _>(effect_context_js()));

    let store_json = build_initial_store_json(initial_store)?;
    let action_js = serde_json::to_string(action_name)?;
    let script = sim_template_js()
        .replace("ACTION_PLACEHOLDER", &action_js)
        .replace("STORE_PLACEHOLDER", &store_json);

    let (result_json, logs_json) = run_simulation_and_collect(&ctx, &script)?;
    if let Ok(logs_vec) = serde_json::from_str::<Vec<String>>(&logs_json) {
        for l in logs_vec.iter() {
            if !l.starts_with("DEBUG_TEMPLATE:") {
                runtime_log!("{}", l);
            }
        }
    }

    let sim_val: serde_json::Value = serde_json::from_str(&result_json)?;
    let created: Vec<String> = sim_val.get("created")
        .and_then(|v| v.as_array())
        .map(|a| a.iter().filter_map(|v| v.as_str()).map(|s| s.to_string()).collect())
        .unwrap_or_default();
    let store: Vec<serde_json::Value> = sim_val.get("store")
        .and_then(|v| v.as_array())
        .map(|a| a.clone())
        .unwrap_or_default();
    let mut pe: Vec<String> = Vec::new();
    if let Some(p) = sim_val.get("pendingEffects").and_then(|v| v.as_array()) {
        for e in p {
            if let Some(n) = e.get("name").and_then(|v| v.as_str()) {
                pe.push(n.to_string());
            }
        }
    }
    if !pe.is_empty() {
        crate::state::set_pending_effects(pe);
    }

    Ok((created, convert_store_values(&store)))
}

pub fn process_pending_effects(files: &std::collections::HashMap<String, String>) -> Result<()> {
    let effects = crate::state::pending_effects().lock().unwrap().clone();
    eprintln!("[DEBUG process_pending_effects] effects count={}", effects.len());
    if effects.is_empty() { return Ok(()); }
    crate::state::clear_pending_effects();
    let (_rt, ctx) = prepare_runtime_and_ctx()?;
    install_host_api(&ctx)?;

    // Set up globals for effect execution.
    let _ = ctx.with(|c| c.eval::<(), _>(globals_script()));

    // Evaluate module source
    let source = select_entry_source(files);
    if !source.is_empty() {
        let patched = patch_user_source(&source);
        let transformed = if patched.contains("export default") {
            patched.replace("export default", "var __module_default =")
        } else {
            patched
        };
        ctx.with(|ctx| ctx.eval::<(), _>(transformed))?;
        let _ = ctx.with(|ctx| ctx.eval::<(), _>(module_call_script()));
    }

    // Restore entity data from Rust state into JS context so effects can mutate entities
    let text_data = crate::state::last_entity_data().lock().unwrap().clone();
    let number_data = crate::state::last_entity_number_data().lock().unwrap().clone();
    if !text_data.is_empty() || !number_data.is_empty() {
        let mut all_ids: std::collections::HashSet<String> = std::collections::HashSet::new();
        for id in text_data.keys() { all_ids.insert(id.clone()); }
        for id in number_data.keys() { all_ids.insert(id.clone()); }

        let mut entity_data_map = serde_json::Map::new();
        for entity_id in all_ids {
            let mut entity_obj = serde_json::Map::new();
            if let Some(tm) = text_data.get(&entity_id) {
                let mut tm_obj = serde_json::Map::new();
                for (k, v) in tm {
                    tm_obj.insert(k.clone(), serde_json::Value::String(v.clone()));
                }
                entity_obj.insert("textMap".to_string(), serde_json::Value::Object(tm_obj));
            }
            if let Some(nm) = number_data.get(&entity_id) {
                let mut nm_obj = serde_json::Map::new();
                for (k, v) in nm {
                    nm_obj.insert(k.clone(), serde_json::Value::Number(serde_json::Number::from_f64(*v).unwrap()));
                }
                entity_obj.insert("numberMap".to_string(), serde_json::Value::Object(nm_obj));
            }
            entity_data_map.insert(entity_id, serde_json::Value::Object(entity_obj));
        }
        let entity_json = serde_json::to_string(&serde_json::Value::Object(entity_data_map)).unwrap_or_else(|_| "{}".to_string());
        let eval_str = format!("globalThis.__entityData = {};", entity_json);
        let _ = ctx.with(|c| c.eval::<(), _>(eval_str));
    }

    // Install effect context
    let _ = ctx.with(|ctx| ctx.eval::<(), _>(effect_context_js()));

    // Build entity store
    let _ = ctx.with(|ctx| ctx.eval::<(), _>(entity_store_script()));

   for effect_name in effects.iter() {
        // Debug: test direct mutation without getEntityBy
        let debug_script = debug_effect_template()
            .replace("EFFECT_NAME_PLACEHOLDER", effect_name);
        let _ = ctx.with(|c| c.eval::<(), _>(debug_script.as_str()));

        let effect_script = build_effect_script(effect_name)?;
        let result = ctx.with(|c| c.eval::<(), _>(effect_script.as_str()));
        if let Err(e) = result {
            eprintln!("[DEBUG process_pending_effects] error executing effect '{}': {}", effect_name, e);
        }

        // Capture reoccurAfterMs and schedule next execution
        match ctx.with(|c| c.eval::<Option<f64>, _>("globalThis.__lastEffectReoccurAfterMs")) {
            Ok(Some(reoccur_ms)) => {
                let reoccur_ms = reoccur_ms as u64;
                if reoccur_ms > 0 {
                    let next_at = crate::state::get_effect_next_scheduled(effect_name, reoccur_ms);
                    crate::state::push_scheduled_effect(crate::state::ScheduledEffect {
                        name: effect_name.clone(),
                        scheduled_at_ms: next_at,
                        reoccur_after_ms: reoccur_ms,
                    });
                    eprintln!("[DEBUG process_pending_effects] scheduled effect '{}' at game_time={} (reoccurAfterMs={})", effect_name, next_at, reoccur_ms);
                }
            }
            Ok(None) => {}
            Err(e) => {
                eprintln!("[DEBUG process_pending_effects] error reading reoccurAfterMs: {}", e);
            }
        }
    }

    // Collect logs from effect execution and send to logger
    let logs_json = ctx.with(|c| c.eval::<String, _>("JSON.stringify(globalThis.__logs||[])"))
        .unwrap_or_default();
    if let Ok(logs_vec) = serde_json::from_str::<Vec<String>>(&logs_json) {
        for l in logs_vec.iter() {
            runtime_log!("{}", l);
        }
    }

   // Sync __entityStore mutations back to __entityData
    let _ = ctx.with(|c| c.eval::<(), _>(entity_store_sync_script()));

    // Read back entity_data mutations and update Rust state
    let entity_data_json = ctx.with(|c| c.eval::<String, _>("JSON.stringify(globalThis.__entityData||{})"))
        .unwrap_or_else(|_| "{}".to_string());
    eprintln!("[DEBUG process_pending_effects] entity_data after effects: {}", entity_data_json);
    if let Ok(entities_val) = serde_json::from_str::<serde_json::Value>(&entity_data_json) {
        if let Some(entities_obj) = entities_val.as_object() {
            let mut text_data: std::collections::HashMap<String, std::collections::HashMap<String, String>> =
                crate::state::last_entity_data().lock().unwrap().clone();
            let mut number_data: std::collections::HashMap<String, std::collections::HashMap<String, f64>> =
                crate::state::last_entity_number_data().lock().unwrap().clone();

            for (entity_id, entity_val) in entities_obj {
                if let Some(text_map) = entity_val.get("textMap").and_then(|v| v.as_object()) {
                    let tm = text_data.entry(entity_id.clone()).or_insert_with(std::collections::HashMap::new);
                    for (k, v) in text_map {
                        if let Some(s) = v.as_str() { tm.insert(k.clone(), s.to_string()); }
                    }
                }
                if let Some(number_map) = entity_val.get("numberMap").and_then(|v| v.as_object()) {
                    let nm = number_data.entry(entity_id.clone()).or_insert_with(std::collections::HashMap::new);
                    for (k, v) in number_map {
                        if let Some(n) = v.as_f64() { nm.insert(k.clone(), n); }
                    }
                }
            }

            crate::state::set_last_entity_data(text_data);
            crate::state::set_last_entity_number_data(number_data);
        }
    }

    Ok(())
}

/// Debug effect script template loaded at compile time.
fn debug_effect_template() -> &'static str {
    include_str!("../js/scripts/debug_effect.js")
}

/// Entity store sync script loaded at compile time.
fn entity_store_sync_script() -> &'static str {
    include_str!("../js/scripts/entity_store_sync.js")
}

/// Effect script template loaded at compile time.
fn effect_script_template() -> &'static str {
    include_str!("../js/scripts/effect_script.js")
}

/// Build script to execute a specific effect by name.
fn build_effect_script(effect_name: &str) -> Result<String> {
    let quoted_name = format!("'{}'", effect_name);
    Ok(effect_script_template()
        .replace("EFFECT_NAME_PLACEHOLDER", &quoted_name))
}

#[cfg(test)]
mod tests {
    use super::*;
    use crate::state;
    use std::collections::HashMap;

    fn setup_test_environment() {
        state::clear_state();
    }

    fn build_test_files_with_effect() -> HashMap<String, String> {
        let mut files = HashMap::new();
        files.insert(
            "manifest.json".to_string(),
            r#"{"id":"test","version":"1.0.0","name":"Test","entry":"index.js"}"#.to_string(),
        );
        files.insert(
            "index.js".to_string(),
            r#"
export default (hostApi) => {
    const {number, string} = hostApi;
    hostApi.setEntity(string.of("entity_id"), {
        numberMap: {
            "counter": number.of(1)
        }
    });
    hostApi.registerEffect({
        name: "increment_counter",
        reoccurAfterMs: (context, executionCount, input, output) => {
            return hostApi.maybe.of(number.of(100));
        },
        apply: context => {
            context.getEntityBy(hostApi.entity.filter.create()
                .byId(id => id.isContainingExactly(string.of("entity_id"))))
                .map(elementExpr => elementExpr.getNumber(string.of("counter")).map(v => v.sum(number.of(1))));
        }
    });
};
"#.to_string(),
        );
        files
    }

    #[test]
    fn test_value_wrapper_sum_via_map() {
        setup_test_environment();
        let files = build_test_files_with_effect();

        // Initialize entity data via extract_from_source
        let source = files.get("index.js").unwrap();
        let dec = extract_from_source(source).unwrap();

        // Verify entity data was extracted
        let entity_data = &dec.entity_data;
        assert!(entity_data.get("entity_id").is_some(), "entity_id should exist in entity_data");

        if let Some(entity_val) = entity_data.as_object() {
            if let Some(entity_id_val) = entity_val.get("entity_id") {
                if let Some(number_map) = entity_id_val.get("numberMap").and_then(|v| v.as_object()) {
                    let counter = number_map.get("counter").and_then(|v| v.as_f64());
                    assert_eq!(counter, Some(1.0), "counter should be 1 initially");
                }
            }
        }
    }

    #[test]
    fn test_process_pending_effects_increments_counter() {
        setup_test_environment();
        let files = build_test_files_with_effect();

        // Initialize entity data
        let source = files.get("index.js").unwrap();
        let _dec = extract_from_source(source).unwrap();

        // Set pending effects
        state::set_pending_effects(vec!["increment_counter".to_string()]);

        // Process pending effects
        process_pending_effects(&files).unwrap();

        // Verify counter was incremented
        let number_data = state::last_entity_number_data().lock().unwrap();
        let counter = number_data
            .get("entity_id")
            .and_then(|map| map.get("counter"));
        assert_eq!(counter, Some(&2.0), "counter should be 2 after one effect execution");
    }

    #[test]
    fn test_multiple_effect_executions_increment_counter() {
        setup_test_environment();
        let files = build_test_files_with_effect();

        // Initialize entity data
        let source = files.get("index.js").unwrap();
        let _dec = extract_from_source(source).unwrap();

        // Process effect multiple times
        for _ in 0..5 {
            state::set_pending_effects(vec!["increment_counter".to_string()]);
            process_pending_effects(&files).unwrap();
        }

        // Verify counter was incremented 5 times
        let number_data = state::last_entity_number_data().lock().unwrap();
        let counter = number_data
            .get("entity_id")
            .and_then(|map| map.get("counter"));
        assert_eq!(counter, Some(&6.0), "counter should be 6 after 5 effect executions (1 + 5)");
    }

    #[test]
    fn test_effect_reoccurrence_scheduling() {
        setup_test_environment();
        let files = build_test_files_with_effect();

        // Initialize entity data
        let source = files.get("index.js").unwrap();
        let _dec = extract_from_source(source).unwrap();

        // Set initial game time
        state::set_game_time_ms(0);

        // Set pending effects
        state::set_pending_effects(vec!["increment_counter".to_string()]);

        // Process pending effects (should schedule next execution at game_time=100)
        process_pending_effects(&files).unwrap();

        // Verify scheduled effect was created
        let scheduled = state::scheduled_effects().lock().unwrap();
        assert!(!scheduled.is_empty(), "should have scheduled effect for reoccurrence");
        assert_eq!(scheduled[0].name, "increment_counter");
        assert_eq!(scheduled[0].scheduled_at_ms, 100);
        assert_eq!(scheduled[0].reoccur_after_ms, 100);
    }

    #[test]
    fn test_entity_number_data_persistence_across_effects() {
        setup_test_environment();
        let files = build_test_files_with_effect();

        // Initialize entity data
        let source = files.get("index.js").unwrap();
        let _dec = extract_from_source(source).unwrap();

        // First effect execution
        state::set_pending_effects(vec!["increment_counter".to_string()]);
        process_pending_effects(&files).unwrap();

        let number_data = state::last_entity_number_data().lock().unwrap();
        assert_eq!(
            number_data.get("entity_id").and_then(|m| m.get("counter")),
            Some(&2.0),
            "counter should be 2 after first execution"
        );

        // Second effect execution - should read updated value and increment
        drop(number_data);
        state::set_pending_effects(vec!["increment_counter".to_string()]);
        process_pending_effects(&files).unwrap();

        let number_data = state::last_entity_number_data().lock().unwrap();
        assert_eq!(
            number_data.get("entity_id").and_then(|m| m.get("counter")),
            Some(&3.0),
            "counter should be 3 after second execution"
        );
    }

    #[test]
    fn test_get_entity_number_map_value_after_effect() {
        setup_test_environment();
        let files = build_test_files_with_effect();

        // Initialize entity data
        let source = files.get("index.js").unwrap();
        let _dec = extract_from_source(source).unwrap();

        // Process effect
        state::set_pending_effects(vec!["increment_counter".to_string()]);
        process_pending_effects(&files).unwrap();

        // Read value via FFI-like access
        let number_data = state::last_entity_number_data().lock().unwrap();
        let value = number_data
            .get("entity_id")
            .and_then(|map| map.get("counter"))
            .map(|n| n.to_string());

        assert_eq!(value, Some("2".to_string()), "FFI read should return '2'");
    }
}
