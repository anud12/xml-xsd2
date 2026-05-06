use anyhow::{anyhow, Result};
use crate::js_runtime::{create_runtime, create_context};
use rquickjs::{Context, Runtime};
use crate::js_host_api::{install_host_api, extract_declarations, Declarations};


/// Given JS source text, run it in an isolated QuickJS runtime and extract the
/// declared structure (events, top-level functions).
///
/// This function keeps side-effects contained to print so tests can
/// observe behavior. The returned Declarations is deserialized from a JSON
/// value produced inside the JS context.
fn create_rt_ctx_and_install(_source: &str) -> Result<(Runtime, Context)> {
    let rt = create_runtime()?;
    let ctx = create_context(&rt)?;
    install_host_api(&ctx)?;
    // minimal sanity prints kept
    if let Ok(kind) = ctx.with(|ctx| ctx.eval::<String, _>("typeof host")) { eprintln!("debug: typeof host = {}", kind); }
    if let Ok(kind) = ctx.with(|ctx| ctx.eval::<String, _>("typeof createEntity")) { eprintln!("debug: typeof createEntity = {}", kind); }
    Ok((rt, ctx))
}

fn transform_source_for_default(source: &str) -> String {
    if source.contains("export default") { source.replace("export default", "var __module_default =") } else { source.to_string() }
}

fn eval_source_in_ctx(ctx: &Context, code: &str) -> Result<()> {
    let res = ctx.with(|ctx| ctx.eval::<(), _>(code.to_string()));
    if let Err(e) = res {
        eprintln!("debug: eval failed: {:?}", e);
        return Err(anyhow!("QuickJS eval error: {}", e));
    }
    Ok(())
}

fn call_module_default_if_present(ctx: &Context, transformed: &str) {
    if transformed.contains("__module_default") {
        let call_snippet = r#"try {
            if (typeof __module_default === 'function') {
                __module_default({
                    string: { of: s => s },
                    number: { of: n => n },
                    entity: { create: function(){ return { withTextMap: function(tm){ return tm; } }; } },
                    textMap: { create: function(){ return { put: function(k,v){ const o = {}; o[k]=v; return o; } }; } },
                    texture: { of: function(p){ return p; } },
                    emitEvent: host.emitEvent,
                    registerEvent: host.registerEvent,
                    registerAction: host.registerAction,
                    registerEffect: host.registerEffect,
                    registerPanel: host.registerPanel,
                    setEntity: host.setEntity,
                    log: host.log
                });
            }
        } catch(e) { }
        "#;
        let _ = ctx.with(|ctx| ctx.eval::<(), _>(call_snippet));
    }
}

pub fn extract_from_source(source: &str) -> Result<Declarations> {
    let (_rt, ctx) = create_rt_ctx_and_install(source)?;
    let transformed = transform_source_for_default(source);
    eval_source_in_ctx(&ctx, &transformed)?;
    call_module_default_if_present(&ctx, &transformed);
    let dec = extract_declarations(&ctx)?;
    Ok(dec)
}

/// Simulate running an action inside a QuickJS context built from the provided files.
/// Returns a tuple of (created_entities, entity_store_rows) where entity_store_rows is a
/// Vec<Vec<String>> suitable for insertion into the 'entity' export table (each row's
/// first column is used as the textMap_name value).
fn select_entry_source(files: &std::collections::HashMap<String, String>) -> String {
    use serde_json::Value;
    runtime_log!("DEBUG_SELECT: select_entry_source called with {} files", files.len());
    for (name, content) in files.iter() {
        runtime_log!("DEBUG_SELECT: checking file '{}' ({} chars)", name, content.len());
        if name.ends_with("manifest.json") || (name.to_lowercase().contains("manifest") && name.ends_with(".json")) {
            runtime_log!("DEBUG_SELECT: found manifest file: {}", name);
            if let Ok(v) = serde_json::from_str::<Value>(content) {
                if let Some(entry) = v.get("entry").and_then(|v| v.as_str()) {
                    runtime_log!("DEBUG_SELECT: manifest.entry = {}", entry);
                    if let Some(src) = files.get(entry) {
                        runtime_log!("DEBUG_SELECT: returning entry '{}' with {} chars", entry, src.len());
                        return src.clone();
                    } else {
                        runtime_log!("DEBUG_SELECT: entry '{}' not found in files!", entry);
                    }
                } else {
                    runtime_log!("DEBUG_SELECT: manifest has no 'entry' field");
                }
            } else {
                runtime_log!("DEBUG_SELECT: failed to parse manifest as JSON");
            }
        }
    }
    if let Some(src) = files.get("index.js") {
        runtime_log!("DEBUG_SELECT: returning fallback index.js ({} chars)", src.len());
        return src.clone();
    }
    runtime_log!("DEBUG_SELECT: no index.js found, returning first file or empty");
    if let Some((_k, v)) = files.iter().next() { return v.clone(); }
    "".to_string()
}

fn eval_entry_in_ctx(ctx: &Context, source: &str) -> Result<String> {
    let transformed = if source.contains("export default") { source.replace("export default", "var __module_default =") } else { source.to_string() };
    ctx.with(|ctx| ctx.eval::<(), _>(transformed.clone()))?;
    if transformed.contains("__module_default") {
        let call_snippet = r#"try {
            if (typeof __module_default === 'function') {
                __module_default({
                    string: { of: s => s },
                    number: { of: n => n },
                    entity: { create: function(){ return { withTextMap: function(tm){ return tm; } }; } },
                    textMap: { create: function(){ return { put: function(k,v){ const o = {}; o[k]=v; return o; } }; } },
                    texture: { of: function(p){ return p; } },
                    emitEvent: host.emitEvent,
                    registerEvent: host.registerEvent,
                    registerAction: host.registerAction,
                    registerEffect: host.registerEffect,
                    registerPanel: host.registerPanel,
                    setEntity: host.setEntity,
                    log: host.log
                });
            }
        } catch(e) { }"#;
        let _ = ctx.with(|ctx| ctx.eval::<(), _>(call_snippet));
    }
    Ok(transformed)
}

fn sim_template_js() -> &'static str {
    r#"(function(actionName, initialStore){
  globalThis.__entityStore = initialStore || [];
  globalThis.__createdEntities = globalThis.__createdEntities || [];
  const acts = globalThis.__registeredActions || [];
  globalThis.__logs = [];  // Clear previous logs to avoid duplication
  globalThis.__logs.push('DEBUG_TEMPLATE: registered actions count=' + acts.length);
  globalThis.__logs.push('DEBUG_TEMPLATE: action name to find=' + actionName);
  const evs = globalThis.__registeredEvents || [];
  function recordCreated(obj) {
    if (obj && typeof obj === 'object') {
      const keys = Object.keys(obj);
      if (keys.length === 1) {
        const k = keys[0];
        const v = String(obj[k]);
        const o = {}; o[k]=v; globalThis.__entityStore.push(o);
        globalThis.__createdEntities.push(v);
        return;
      }
      if (typeof obj.firstName === 'string') {
        globalThis.__entityStore.push({ firstName: obj.firstName });
        globalThis.__createdEntities.push(obj.firstName);
        return;
      }
      try { globalThis.__entityStore.push({ textMap_name: JSON.stringify(obj) }); globalThis.__createdEntities.push(JSON.stringify(obj)); } catch(e) { globalThis.__entityStore.push({ textMap_name: String(obj) }); globalThis.__createdEntities.push(String(obj)); }
    } else {
      globalThis.__entityStore.push({ textMap_name: String(obj) });
      globalThis.__createdEntities.push(String(obj));
    }
  }
  function findEffectByName(name) {
    for (let e of evs) {
      if (typeof e === 'string') { if (e === name) return e; }
      else if (e && typeof e === 'object') {
        if (typeof e.name === 'string' && e.name === name) return e;
        if (e.apply && typeof e.apply === 'function' && e.apply.name === name) return e;
      }
    }
    return null;
  }
  function buildEventContext() {
    return {
      createEntity: recordCreated,
      getEntityBy: function(filter) {
        return {
          randomElement: function() {
            return {
              ifPresent: function(cb) {
                let found = null;
                try {
                  let src = filter.toString();
                  let m = src.match(/isContainingExactly\(hostApi\.string\.of\("([^\"]+)"\)\)/);
                  if (m) {
                    const v = m[1];
                    for (let i=0;i<globalThis.__entityStore.length;i++) {
                      const e = globalThis.__entityStore[i];
                      for (let key in e) { if (String(e[key]).includes(v)) { found = e; break; } }
                      if (found) break;
                    }
                  }
                } catch(e) {}
                if (!found && globalThis.__entityStore.length>0) found = globalThis.__entityStore[0];
                if (!found) return cb(null);
                const wrapper = {
                  getText: function(key) {
                    return {
                      ifPresent: function(cb2) {
                        const nameObj = {
                          concat: function(s) {
                            try {
                              if (found && typeof found === 'object') {
                                if (key in found) { found[key] = String(found[key]) + String(s); }
                                else { const propKeys = Object.keys(found); if (propKeys.length>0) { const pk = propKeys[0]; found[pk] = String(found[pk]) + String(s); } }
                              }
                            } catch(e) {}
                          }
                        };
                        cb2(nameObj);
                      }
                    };
                  },
                  ifPresent: function(cb3) { cb3(wrapper); }
                };
                cb(wrapper);
              }
            };
          }
        };
      }
    };
  }
  function applyEffectByName(name, payload) {
    const ef = findEffectByName(name);
    if (!ef) return;
    let prepared;
    if (typeof ef.prepare === 'function') { try { prepared = ef.prepare(payload); } catch(e) {} }
    if (typeof ef.apply === 'function') { try { ef.apply(buildEventContext(), prepared); } catch(e) {} }
  }
  function emitEvent(name, payload) {
    try {
      globalThis.__logs = globalThis.__logs || [];
      globalThis.__logs.push('DEBUG_TEMPLATE: emitEvent called with name=' + String(name));
      globalThis.__pendingEffects = globalThis.__pendingEffects || [];
      globalThis.__pendingEffects.push({ name: name, payload: payload });
      globalThis.__logs.push('DEBUG_TEMPLATE: pendingEffects length=' + globalThis.__pendingEffects.length);
    } catch (err) {
      globalThis.__logs = globalThis.__logs || [];
      globalThis.__logs.push('DEBUG_TEMPLATE: ERROR in emitEvent: ' + String(err));
    }
  }
  globalThis.__processPendingEffects = function() {
    const pending = globalThis.__pendingEffects || [];
    globalThis.__pendingEffects = [];
    for (let i = 0; i < pending.length; i++) {
      applyEffectByName(pending[i].name, pending[i].payload);
    }
  };

  let actionObj = null;
  for (let a of acts) {
    if (typeof a === 'string') { if (a === actionName) { actionObj = a; break; } }
    else if (a && typeof a === 'object') {
      if (typeof a.name === 'string' && a.name === actionName) { actionObj = a; break; }
      if (a.apply && typeof a.apply === 'function' && a.apply.name === actionName) { actionObj = a; break; }
    }
  }
  if (actionObj) {
    globalThis.__logs.push('DEBUG_TEMPLATE: found action, about to execute');
    const wrappedEmitEvent = function(name, payload) {
      globalThis.__logs.push('DEBUG_TEMPLATE: wrappedEmitEvent called with name=' + String(name));
      return emitEvent(name, payload);
    };
    globalThis.__logs.push('DEBUG_TEMPLATE: wrappedEmitEvent type: ' + typeof wrappedEmitEvent);
    const ctx = { emitEffect: wrappedEmitEvent, emitEvent: wrappedEmitEvent, createEntity: recordCreated, entity: { create: ()=>({ withTextMap: tm => tm }) }, textMap: { create: ()=>({ put: (k,v)=>{ const o={}; o[k]=v; return o; } }) }, string: { of: s => s } };
    globalThis.__logs.push('DEBUG_TEMPLATE: ctx.emitEffect type: ' + typeof ctx.emitEffect);
    globalThis.__logs.push('DEBUG_TEMPLATE: ctx.emitEffect === wrappedEmitEvent: ' + (ctx.emitEffect === wrappedEmitEvent));
    globalThis.__logs.push('DEBUG_TEMPLATE: actionObj type: ' + typeof actionObj);
    globalThis.__logs.push('DEBUG_TEMPLATE: actionObj.apply type: ' + typeof actionObj.apply);
    if (actionObj.apply) {
      globalThis.__logs.push('DEBUG_TEMPLATE: actionObj.apply.length: ' + actionObj.apply.length);
    }
    try {
      if (typeof actionObj === 'object' && typeof actionObj.apply === 'function') { 
        globalThis.__logs.push('DEBUG_TEMPLATE: calling actionObj.apply(ctx)');
        actionObj.apply(ctx); 
        globalThis.__logs.push('DEBUG_TEMPLATE: actionObj.apply returned');
      }
      else if (typeof actionObj === 'function') { 
        globalThis.__logs.push('DEBUG_TEMPLATE: calling actionObj(ctx) as function');
        try { actionObj(ctx); } catch(e) { globalThis.__logs.push('DEBUG_TEMPLATE: action function threw: ' + String(e)); } 
      }
    } catch(e) { globalThis.__logs.push('DEBUG_TEMPLATE: action threw: ' + String(e)); }
    globalThis.__logs.push('DEBUG_TEMPLATE: action execution completed');
    globalThis.__logs.push('DEBUG_TEMPLATE: pendingEffects after action: ' + JSON.stringify(globalThis.__pendingEffects || []));
  }

  return JSON.stringify({ created: globalThis.__createdEntities, store: globalThis.__entityStore, pendingEffects: globalThis.__pendingEffects || [] });
})(ACTION_PLACEHOLDER, STORE_PLACEHOLDER)"#
}


pub fn simulate_action(
    files: &std::collections::HashMap<String, String>,
    action_name: &str,
    initial_store: &[Vec<String>],
) -> Result<(Vec<String>, Vec<Vec<String>>)> {
    eprintln!("DEBUG: simulate_action called");
    eprintln!("DEBUG: files in archive:");
    for (path, _) in files.iter() {
        eprintln!("  - {}", path);
    }
    
    let (_rt, ctx) = prepare_runtime_and_ctx()?;
    install_host_api(&ctx)?;
    let source = select_entry_source(files);
    
    // Debug: log which module is being loaded
    eprintln!("DEBUG: Loading module, length={}, first 300 chars:", source.len());
    if source.len() > 300 {
        eprintln!("{}", &source[..300]);
    } else {
        eprintln!("{}", &source);
    }
    
    // Debug: check if this is the AndTriggerEffect module
    if source.contains("emitEffect") {
        eprintln!("DEBUG: Loading module that uses emitEffect");
        eprintln!("DEBUG: Module source length: {}", source.len());
        let effect_count = source.matches("emitEffect").count();
        eprintln!("DEBUG: emitEffect appears {} times in source", effect_count);
        runtime_log!("DEBUG_MODULE: Loading AndTriggerEffect module with emitEffect");
    }
    
    let _transformed = eval_entry_in_ctx(&ctx, &source)?;

    let store_json = build_initial_store_json(initial_store)?;
    let action_js = serde_json::to_string(action_name)?;
    let script = sim_template_js().replace("ACTION_PLACEHOLDER", &action_js).replace("STORE_PLACEHOLDER", &store_json);
    eprintln!("debug: running simulation script for action: {}", action_name);
    eprintln!("debug: script length: {}", script.len());

    let (result_json, logs_json) = run_simulation_and_collect(&ctx, &script)?;
    eprintln!("debug: simulate_action raw json: {}", result_json);

    if let Ok(logs_vec) = serde_json::from_str::<Vec<String>>(&logs_json) {
        eprintln!("DEBUG: About to log {} entries from JavaScript", logs_vec.len());
        for (i, l) in logs_vec.iter().enumerate() {
            // Skip DEBUG_TEMPLATE logs
            if !l.starts_with("DEBUG_TEMPLATE:") {
                eprintln!("DEBUG: Logging entry {}: {}", i, l);
                runtime_log!("{}", l);
            }
        }
    }

    #[derive(serde::Deserialize)]
    struct PendingEffect {
        name: String,
        #[allow(dead_code)]
        payload: serde_json::Value,
    }
    
    #[derive(serde::Deserialize)]
    struct SimResult {
        created: Vec<String>,
        store: Vec<serde_json::Value>,
        #[serde(default)]
        #[serde(rename = "pendingEffects")]
        pending_effects: Vec<PendingEffect>,
    }
    let sim: SimResult = serde_json::from_str(&result_json)?;
    
    // Store pending effects in state
    if !sim.pending_effects.is_empty() {
        let effect_names: Vec<String> = sim.pending_effects.iter().map(|e| e.name.clone()).collect();
        runtime_log!("DEBUG: storing {} pending effects: {:?}", effect_names.len(), effect_names);
        crate::state::set_pending_effects(effect_names);
    } else {
        runtime_log!("DEBUG: no pending effects to store");
    }

    let store_rows = convert_store_values(&sim.store);
    Ok((sim.created, store_rows))
}

fn prepare_runtime_and_ctx() -> Result<(Runtime, Context)> {
    let rt = create_runtime()?;
    let ctx = create_context(&rt)?;
    Ok((rt, ctx))
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
    eprintln!("debug: run_simulation_and_collect: executing script");
    let result_json = ctx.with(|ctx| ctx.eval::<String, _>(script))?;
    eprintln!("debug: run_simulation_and_collect: script returned: {}", result_json);
    let logs_json = ctx.with(|ctx| ctx.eval::<String, _>("JSON.stringify(globalThis.__logs || [])")).unwrap_or_else(|_| "[]".to_string());
    eprintln!("debug: run_simulation_and_collect: logs: {}", logs_json);
    Ok((result_json, logs_json))
}

pub fn process_pending_effects(files: &std::collections::HashMap<String, String>) -> Result<()> {
    // Get pending effects from state
    let effects = crate::state::pending_effects().lock().unwrap().clone();
    
    if effects.is_empty() {
        return Ok(());
    }
    
    // Clear the pending effects queue
    crate::state::clear_pending_effects();
    
    // Create a runtime and context
    let (_rt, ctx) = prepare_runtime_and_ctx()?;
    install_host_api(&ctx)?;
    let source = select_entry_source(files);
    
    let _transformed = eval_entry_in_ctx(&ctx, &source)?;
    
    // For each effect, execute it
    for effect_name in effects.iter() {
        // Build the effect execution script
        let script = format!(r#"
            const evs = globalThis.__registeredEvents || [];
            let found = null;
            for (let i = 0; i < evs.length; i++) {{
                const e = evs[i];
                if (e && typeof e === 'object' && e.name === "{}") {{
                    found = e;
                    break;
                }}
            }}
            
            if (found) {{
                let prepared = null;
                if (typeof found.prepare === 'function') {{
                    prepared = found.prepare({{}});
                }}
                
                if (typeof found.apply === 'function') {{
                    found.apply();
                }}
            }}
        "#, effect_name);
        
        let _ = ctx.with(|ctx| ctx.eval::<(), _>(script));
        
        // Collect logs
        let logs_json = ctx.with(|ctx| ctx.eval::<String, _>("JSON.stringify(globalThis.__logs || [])")).unwrap_or_else(|_| "[]".to_string());
        if let Ok(logs_vec) = serde_json::from_str::<Vec<String>>(&logs_json) {
            for l in logs_vec.iter() {
                runtime_log!("{}", l);
            }
        }
    }
    
    Ok(())
}

fn convert_store_values(values: &[serde_json::Value]) -> Vec<Vec<String>> {
    let mut store_rows: Vec<Vec<String>> = Vec::new();
    for obj in values.iter() {
        if let Some(map) = obj.as_object() {
            if !map.is_empty() {
                let (_k, v) = map.iter().next().unwrap();
                if let Some(s) = v.as_str() { store_rows.push(vec![s.to_string()]); }
                else { store_rows.push(vec![v.to_string()]); }
            } else { store_rows.push(vec!["".to_string()]); }
        } else { store_rows.push(vec![obj.to_string()]); }
    }
    store_rows
}


