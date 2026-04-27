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
    for (name, content) in files.iter() {
        if name.ends_with("manifest.json") || (name.to_lowercase().contains("manifest") && name.ends_with(".json")) {
            if let Ok(v) = serde_json::from_str::<Value>(content) {
                if let Some(entry) = v.get("entry").and_then(|v| v.as_str()) {
                    if let Some(src) = files.get(entry) { return src.clone(); }
                }
            }
        }
    }
    if let Some(src) = files.get("index.js") { return src.clone(); }
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
  function emitEvent(name, payload) { applyEffectByName(name, payload); }

  let actionObj = null;
  for (let a of acts) {
    if (typeof a === 'string') { if (a === actionName) { actionObj = a; break; } }
    else if (a && typeof a === 'object') {
      if (typeof a.name === 'string' && a.name === actionName) { actionObj = a; break; }
      if (a.apply && typeof a.apply === 'function' && a.apply.name === actionName) { actionObj = a; break; }
    }
  }
  if (actionObj) {
    const ctx = { emitEffect: emitEvent, emitEvent: emitEvent, createEntity: recordCreated, entity: { create: ()=>({ withTextMap: tm => tm }) }, textMap: { create: ()=>({ put: (k,v)=>{ const o={}; o[k]=v; return o; } }) }, string: { of: s => s } };
    try {
      if (typeof actionObj === 'object' && typeof actionObj.apply === 'function') { actionObj.apply(ctx); }
      else if (typeof actionObj === 'function') { try { actionObj(ctx); } catch(e) {} }
    } catch(e) {}
  }

  return JSON.stringify({ created: globalThis.__createdEntities, store: globalThis.__entityStore });
})(ACTION_PLACEHOLDER, STORE_PLACEHOLDER)"#
}


pub fn simulate_action(
    files: &std::collections::HashMap<String, String>,
    action_name: &str,
    initial_store: &[Vec<String>],
) -> Result<(Vec<String>, Vec<Vec<String>>)> {
    let (_rt, ctx) = prepare_runtime_and_ctx()?;
    install_host_api(&ctx)?;
    let source = select_entry_source(files);
    let _transformed = eval_entry_in_ctx(&ctx, &source)?;

    let store_json = build_initial_store_json(initial_store)?;
    let action_js = serde_json::to_string(action_name)?;
    let script = sim_template_js().replace("ACTION_PLACEHOLDER", &action_js).replace("STORE_PLACEHOLDER", &store_json);

    let (result_json, logs_json) = run_simulation_and_collect(&ctx, &script)?;
    eprintln!("debug: simulate_action raw json: {}", result_json);

    if let Ok(logs_vec) = serde_json::from_str::<Vec<String>>(&logs_json) {
        for l in logs_vec.iter() { runtime_log!("{}", l); }
    }

    #[derive(serde::Deserialize)]
    struct SimResult { created: Vec<String>, store: Vec<serde_json::Value> }
    let sim: SimResult = serde_json::from_str(&result_json)?;

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
    let result_json = ctx.with(|ctx| ctx.eval::<String, _>(script))?;
    let logs_json = ctx.with(|ctx| ctx.eval::<String, _>("JSON.stringify(globalThis.__logs || [])")).unwrap_or_else(|_| "[]".to_string());
    Ok((result_json, logs_json))
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


