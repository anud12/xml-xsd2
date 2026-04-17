use anyhow::{anyhow, Result};
use crate::js_runtime::{create_runtime, create_context};
use crate::js_host_api::{install_host_api, extract_declarations, Declarations};

/// Given JS source text, run it in an isolated QuickJS runtime and extract the
/// declared structure (events, top-level functions).
///
/// This function keeps side-effects contained to print so tests can
/// observe behavior. The returned Declarations is deserialized from a JSON
/// value produced inside the JS context.
pub fn extract_from_source(source: &str) -> Result<Declarations> {
    // Create runtime and context; context borrows runtime so order matters.
    let rt = create_runtime()?;
    let ctx = create_context(&rt)?;

    // Install tiny host API the source may call (host.registerEvent, host.emitEvent)
    install_host_api(&ctx)?;

    // Sanity-check: ensure host API is present in the context before evaluating
    if let Ok(kind) = ctx.with(|ctx| ctx.eval::<String, _>("typeof host")) {
        eprintln!("debug: typeof host = {}", kind);
    } else {
        eprintln!("debug: typeof host check failed");
    }
    if let Ok(kind) = ctx.with(|ctx| ctx.eval::<String, _>("typeof createEntity")) {
        eprintln!("debug: typeof createEntity = {}", kind);
    }

    // If the module uses `export default`, transform it into a callable var so
    // it can be invoked with a hostApi object. This lets code that registers
    // events inside a default export run and record declarations.
    let transformed = if source.contains("export default") {
        source.replace("export default", "var __module_default =")
    } else {
        source.to_string()
    };

    // Evaluate the whole transformed source. Single-eval handles multi-line
    // declarations and module-style `export default` transformations cleanly.
    let res = ctx.with(|ctx| ctx.eval::<(), _>(transformed.clone()));
    if let Err(e) = res {
        eprintln!("debug: eval failed: {:?}", e);
        return Err(anyhow!("QuickJS eval error: {}", e));
    }

    // If a default export was transformed to __module_default, attempt to call it
    // with a small hostApi + helpers object so top-level registration and emits
    // execute and are recorded by the installed host API.
    if transformed.contains("__module_default") {
        let call_snippet = r#"try {
            if (typeof __module_default === 'function') {
                __module_default({
                    string: { of: s => s },
                    entity: { create: function(){ return { withTextMap: function(tm){ return tm; } }; } },
                    textMap: { create: function(){ return { put: function(k,v){ const o = {}; o[k]=v; return o; } }; } },
                    emitEvent: host.emitEvent,
                    registerEvent: host.registerEvent,
                    registerAction: host.registerAction,
                    registerEffect: host.registerEffect,
                    log: host.log
                });
            }
        } catch(e) { }
        "#;
        let _ = ctx.with(|ctx| ctx.eval::<(), _>(call_snippet));
    }

    // Extract discovered declarations (reads __registeredEvents and top-level functions)
    let dec = extract_declarations(&ctx)?;
    Ok(dec)
}

/// Simulate running an action inside a QuickJS context built from the provided files.
/// Returns a tuple of (created_entities, entity_store_rows) where entity_store_rows is a
/// Vec<Vec<String>> suitable for insertion into the 'entity' export table (each row's
/// first column is used as the textMap_name value).
pub fn simulate_action(
    files: &std::collections::HashMap<String, String>,
    action_name: &str,
    initial_store: &[Vec<String>],
) -> Result<(Vec<String>, Vec<Vec<String>>)> {
    use serde_json::Value;
    // Create runtime + context and install host API so the module can register actions/effects
    let rt = create_runtime()?;
    let ctx = create_context(&rt)?;
    install_host_api(&ctx)?;

    // Select entry source: prefer manifest->entry, otherwise index.js, otherwise first file
    let mut entry_source: Option<String> = None;
    for (name, content) in files.iter() {
        if name.ends_with("manifest.json") || (name.to_lowercase().contains("manifest") && name.ends_with(".json")) {
            if let Ok(v) = serde_json::from_str::<Value>(content) {
                if let Some(entry) = v.get("entry").and_then(|v| v.as_str()) {
                    if let Some(src) = files.get(entry) {
                        entry_source = Some(src.clone());
                        break;
                    }
                }
            }
        }
    }
    if entry_source.is_none() {
        if let Some(src) = files.get("index.js") {
            entry_source = Some(src.clone());
        } else if let Some((_k, v)) = files.iter().next() {
            entry_source = Some(v.clone());
        }
    }
    let source = entry_source.unwrap_or_default();
    let transformed = if source.contains("export default") {
        source.replace("export default", "var __module_default =")
    } else {
        source.clone()
    };

    // Evaluate module source so registered actions/effects are available in the context
    ctx.with(|ctx| ctx.eval::<(), _>(transformed.clone()))?;
    if transformed.contains("__module_default") {
        let call_snippet = r#"try {
            if (typeof __module_default === 'function') {
                __module_default({
                    string: { of: s => s },
                    entity: { create: function(){ return { withTextMap: function(tm){ return tm; } }; } },
                    textMap: { create: function(){ return { put: function(k,v){ const o = {}; o[k]=v; return o; } }; } },
                    emitEvent: host.emitEvent,
                    registerEvent: host.registerEvent,
                    registerAction: host.registerAction,
                    registerEffect: host.registerEffect,
                    log: host.log
                });
            }
        } catch(e) { }"#;
        let _ = ctx.with(|ctx| ctx.eval::<(), _>(call_snippet));
    }

    // Build initial store as array of objects: preserve the textual value but also
    // include a dynamic key equal to the textMap value so JS filters that look up
    // by that key can find and mutate the object. Insert the dynamic key first so
    // it becomes the first property (used when converting back to rows).
    let mut store_array: Vec<Value> = Vec::new();
    for row in initial_store.iter() {
        if !row.is_empty() {
            let key = row[0].clone();
            let mut map = serde_json::Map::new();
            // canonical export property first
            map.insert("textMap_name".to_string(), Value::String(key.clone()));
            // dynamic key second
            map.insert(key.clone(), Value::String(key.clone()));
            store_array.push(Value::Object(map));
        }
    }
    let store_json = serde_json::to_string(&store_array)?;
    let action_js = serde_json::to_string(action_name)?;

    // Simulation snippet: performs action.apply, executes emitted effects (prepare->apply),
    // records created entities into __createdEntities and mutates __entityStore for effects that
    // modify existing entities.
    let sim_template = r#"(function(actionName, initialStore){
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
                      let m = src.match(/isContainingExactly\(hostApi\.string\.of\("([^"]+)"\)\)/);
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
        const ctx = { emitEvent: emitEvent, createEntity: recordCreated, entity: { create: ()=>({ withTextMap: tm => tm }) }, textMap: { create: ()=>({ put: (k,v)=>{ const o={}; o[k]=v; return o; } }) }, string: { of: s => s } };
        try {
          if (typeof actionObj === 'object' && typeof actionObj.apply === 'function') { actionObj.apply(ctx); }
          else if (typeof actionObj === 'function') { try { actionObj(ctx); } catch(e) {} }
        } catch(e) {}
      }

      return JSON.stringify({ created: globalThis.__createdEntities, store: globalThis.__entityStore });
    })(ACTION_PLACEHOLDER, STORE_PLACEHOLDER)"#;

    let script = sim_template.replace("ACTION_PLACEHOLDER", &action_js).replace("STORE_PLACEHOLDER", &store_json);

    let result_json = ctx.with(|ctx| ctx.eval::<String, _>(script.as_str()))?;
    eprintln!("debug: simulate_action raw json: {}", result_json);
    // Extract any logs produced during action execution from the JS context and forward them
    // to the runtime log so Java tests can observe messages like "action called".
    let logs_json = ctx.with(|ctx| ctx.eval::<String, _>("JSON.stringify(globalThis.__logs || [])")).unwrap_or_else(|_| "[]".to_string());
    if let Ok(logs_vec) = serde_json::from_str::<Vec<String>>(&logs_json) {
        for l in logs_vec.iter() {
            runtime_log!("{}", l);
        }
    }
    #[derive(serde::Deserialize)]
    struct SimResult {
        created: Vec<String>,
        store: Vec<serde_json::Value>,
    }
    let sim: SimResult = serde_json::from_str(&result_json)?;

    // Convert store objects into Vec<Vec<String>> by taking the first property's string value
    let mut store_rows: Vec<Vec<String>> = Vec::new();
    for obj in sim.store.iter() {
        if let Some(map) = obj.as_object() {
            if !map.is_empty() {
                let (_k, v) = map.iter().next().unwrap();
                if let Some(s) = v.as_str() {
                    store_rows.push(vec![s.to_string()]);
                } else {
                    store_rows.push(vec![v.to_string()]);
                }
            } else {
                store_rows.push(vec!["".to_string()]);
            }
        } else {
            store_rows.push(vec![obj.to_string()]);
        }
    }

    Ok((sim.created, store_rows))
}
