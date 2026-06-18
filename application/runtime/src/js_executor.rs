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
            var hostApi = {
                string: { of: s => s },
                number: { of: n => n },
                entity: {
                    create: function(){ return { withTextMap: function(tm){ return tm; } }; },
                    filter: {
                        create: function() {
                            return {
                                byId: function(fn) {
                                    return { fn: fn };
                                }
                            };
                        }
                    }
                },
                textMap: { create: function(){ return { put: function(k,v){ const o = {}; o[k]=v; return o; } }; } },
                texture: { of: function(p){ return p; } },
                emitEvent: host.emitEvent,
                registerEvent: host.registerEvent,
                registerAction: host.registerAction,
                registerEffect: host.registerEffect,
                registerPanel: host.registerPanel,
                setEntity: host.setEntity,
                log: host.log,
                maybe: { of: function(v) { return { value: v }; }, none: function() { return { value: undefined }; } },
                condition: { of: function(v) {
                    return {
                        value: v,
                        ifTrue: function(cb) { if (v && typeof cb === 'function') cb(); },
                        ifFalse: function(cb) { if (!v && typeof cb === 'function') cb(); }
                    };
                }}
            };
            globalThis.hostApi = hostApi;
            if (typeof __module_default === 'function') {
                __module_default(hostApi);
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
                    if let Some(src) = files.get(entry) {
                        return src.clone();
                    }
                }
            }
        }
    }
    if let Some(src) = files.get("index.js") {
        return src.clone();
    }
    if let Some((_k, v)) = files.iter().next() { return v.clone(); }
    "".to_string()
}

fn eval_entry_in_ctx(ctx: &Context, source: &str) -> Result<String> {
    let transformed = if source.contains("export default") { source.replace("export default", "var __module_default =") } else { source.to_string() };
    ctx.with(|ctx| ctx.eval::<(), _>(transformed.clone()))?;
    if transformed.contains("__module_default") {
        let call_snippet = r#"try {
            var hostApi = {
                string: { of: s => s },
                number: { of: n => n },
                entity: {
                    create: function(){ return { withTextMap: function(tm){ return tm; } }; },
                    filter: {
                        create: function() {
                            return {
                                byId: function(fn) {
                                    return { fn: fn };
                                }
                            };
                        }
                    }
                },
                textMap: { create: function(){ return { put: function(k,v){ const o = {}; o[k]=v; return o; } }; } },
                texture: { of: function(p){ return p; } },
                emitEvent: host.emitEvent,
                registerEvent: host.registerEvent,
                registerAction: host.registerAction,
                registerEffect: host.registerEffect,
                registerPanel: host.registerPanel,
                setEntity: host.setEntity,
                log: host.log,
                maybe: { of: function(v) { return { value: v }; }, none: function() { return { value: undefined }; } },
                condition: { of: function(v) {
                    return {
                        value: v,
                        ifTrue: function(cb) { if (v && typeof cb === 'function') cb(); },
                        ifFalse: function(cb) { if (!v && typeof cb === 'function') cb(); }
                    };
                }}
            };
            globalThis.hostApi = hostApi;
            if (typeof __module_default === 'function') {
                __module_default(hostApi);
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
  globalThis.__logs = [];
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
    globalThis.__pendingEffects = globalThis.__pendingEffects || [];
    globalThis.__pendingEffects.push({ name: name, payload: payload });
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
    const wrappedEmitEvent = function(name, payload) {
      return emitEvent(name, payload);
    };
    const ctx = { emitEffect: wrappedEmitEvent, emitEvent: wrappedEmitEvent, createEntity: recordCreated, entity: { create: ()=>({ withTextMap: tm => tm }) }, textMap: { create: ()=>({ put: (k,v)=>{ const o={}; o[k]=v; return o; } }) }, string: { of: s => s } };
    try {
      if (typeof actionObj === 'object' && typeof actionObj.apply === 'function') { 
        actionObj.apply(ctx); 
      }
      else if (typeof actionObj === 'function') { 
        try { actionObj(ctx); } catch(e) {} 
      }
    } catch(e) {}
  }

  return JSON.stringify({ created: globalThis.__createdEntities, store: globalThis.__entityStore, pendingEffects: globalThis.__pendingEffects || [] });
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
    if let Ok(logs_vec) = serde_json::from_str::<Vec<String>>(&logs_json) {
        for l in logs_vec.iter() {
            runtime_log!("{}", l);
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

    if !sim.pending_effects.is_empty() {
        let effect_names: Vec<String> = sim.pending_effects.iter().map(|e| e.name.clone()).collect();
        crate::state::set_pending_effects(effect_names);
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
    let result_json = ctx.with(|ctx| ctx.eval::<String, _>(script))?;
    let logs_json = ctx.with(|ctx| ctx.eval::<String, _>("JSON.stringify(globalThis.__logs || [])")).unwrap_or_else(|_| "[]".to_string());
    Ok((result_json, logs_json))
}

pub fn process_pending_effects(files: &std::collections::HashMap<String, String>, current_elapsed: i64) -> Result<()> {
    let effects = crate::state::pending_effects().lock().unwrap().clone();

    if effects.is_empty() {
        return Ok(());
    }

    crate::state::clear_pending_effects();

    let (_rt, ctx) = prepare_runtime_and_ctx()?;
    install_host_api(&ctx)?;
    let source = select_entry_source(files);

    let _transformed = eval_entry_in_ctx(&ctx, &source)?;

    let number_data = crate::state::last_entity_number_data().lock().unwrap().clone();
    let entity_store_json: Vec<serde_json::Value> = number_data.iter().map(|(entity_id, props)| {
            let mut obj = serde_json::Map::new();
            obj.insert(entity_id.clone(), serde_json::Value::String(entity_id.clone()));
            if let Some(number_map) = props.get("key") {
                obj.insert("key".to_string(), serde_json::Value::Number(serde_json::Number::from_f64(*number_map).unwrap_or(serde_json::Number::from(0))));
            }
            serde_json::Value::Object(obj)
        }).collect();
        let entity_store_json_str = serde_json::to_string(&entity_store_json).unwrap_or_else(|_| "[]".to_string());
        let _ = ctx.with(|ctx| ctx.eval::<(), _>(format!("globalThis.__entityStore = {}; ", entity_store_json_str)));

        // Also sync to __entityData for numberMap and textMap access
        let text_data = crate::state::last_entity_data().lock().unwrap().clone();
        let entity_data_json: std::collections::HashMap<String, serde_json::Value> = number_data.iter().map(|(entity_id, props)| {
            let mut nm = serde_json::Map::new();
            for (k, v) in props.iter() {
                if let Some(n) = serde_json::Number::from_f64(*v) {
                    nm.insert(k.clone(), serde_json::Value::Number(n));
                }
            }
            let mut obj = serde_json::Map::new();
            obj.insert("numberMap".to_string(), serde_json::Value::Object(nm));
            // Also include textMap if present
            if let Some(text_props) = text_data.get(entity_id) {
                let mut tm = serde_json::Map::new();
                for (k, v) in text_props.iter() {
                    tm.insert(k.clone(), serde_json::Value::String(v.clone()));
                }
                obj.insert("textMap".to_string(), serde_json::Value::Object(tm));
            }
            (entity_id.clone(), serde_json::Value::Object(obj))
        }).collect();
        let entity_data_json_str = serde_json::to_string(&entity_data_json).unwrap_or_else(|_| "{}".to_string());
        let _ = ctx.with(|ctx| ctx.eval::<(), _>(format!("globalThis.__entityData = {}; ", entity_data_json_str)));

// Make hostApi available globally for effect's apply function
            let _ = ctx.with(|ctx| ctx.eval::<(), _>(r#"
                globalThis.hostApi = {
                    entity: {
                        filter: {
                            create: function() {
                                return {
                                    byId: function(fn) {
                                        return { fn: fn };
                                    }
                                };
                            }
                        }
                    },
                    string: { of: function(s) { return s; } },
                    number: { of: function(n) { return n; } },
                    maybe: { of: function(v) { return { value: v }; }, none: function() { return { value: undefined }; } },
                    condition: { of: function(v) {
                        return {
                            value: v,
                            ifTrue: function(cb) { if (v && typeof cb === 'function') cb(); },
                            ifFalse: function(cb) { if (!v && typeof cb === 'function') cb(); }
                        };
                    }}
                };
            "#));

// For each pending effect, execute it
        for effect_name in effects.iter() {
            // Find the effect and store it globally
            let lookup_script = format!(r#"
                (function() {{
                    var evs = globalThis.__registeredEvents || [];
                    for (var i = 0; i < evs.length; i++) {{
                        if (evs[i] && evs[i].name === '{}') {{
                            globalThis.__foundEffect = evs[i];
                            break;
                        }}
                    }}
                }})();
            "#, effect_name);
            if let Err(_e) = ctx.with(|ctx| ctx.eval::<(), _>(lookup_script)) {
                continue;
            }

            let found_check = ctx.with(|ctx| ctx.eval::<bool, _>("globalThis.__foundEffect !== undefined")).unwrap_or(false);
            if !found_check {
                continue;
            }

            // Build the context
            let _ = ctx.with(|ctx| ctx.eval::<(), _>(r#"
                (function() {
                    var found_entity = null;
                    for (var eid in globalThis.__entityData) {
                        found_entity = globalThis.__entityData[eid];
                        break;
                    }
                    globalThis.__context = {
                        getEntityBy: function(filter) {
                            return {
map: function(cb) {
                                      if (!found_entity) return;
                                      cb({
                                         getNumber: function(key) {
                                            return {
map: function(cb3) {
                                                      if (!found_entity.numberMap || found_entity.numberMap[key] === undefined) return { orElse: function(def) { return def; } };
                                                       var result = cb3({
                                                   sum: function(s) {
                                                                        found_entity.numberMap[key] = Number(found_entity.numberMap[key]) + Number(s);
                                                                    },
                                                                  divide: function(d) {
                                                                      var numVal = found_entity.numberMap[key];
                                                                      var remainder = Number(numVal) % Number(d);
                                                                      return {
                                                                          isEqualTo: function(target) {
                                                                              var isEqual = (remainder === Number(target));
                                                                              return {
                                                                                  ifTrue: function(cb) { if (isEqual && typeof cb === 'function') cb(); },
                                                                                  ifFalse: function(cb) { if (!isEqual && typeof cb === 'function') cb(); },
                                                                                  orElse: function(def) { return isEqual ? { value: true, ifTrue: function(cb) { if (typeof cb === 'function') cb(); }, ifFalse: function(cb) {} } : def; }
                                                                              };
                                                                          }
                                                                      };
                                                                  },
                                                                  isLessOrEqualTo: function(target) {
                                                                      return Number(found_entity.numberMap[key]) <= Number(target);
                                                                  }
                                                              });
                                                    return result || { orElse: function(def) { return def; } };
                                                }
                                            };
                                        },
                                        getText: function(key) {
                                            return {
                                                ifPresent: function(cb) {
                                                    if (found_entity.textMap && found_entity.textMap[key] !== undefined) {
                                                        cb({
                                                            get: function() { return found_entity.textMap[key]; },
                                                            set: function(val) {
                                                                if (!found_entity.textMap) found_entity.textMap = {};
                                                                found_entity.textMap[key] = val;
                                                            }
                                                        });
                                                    }
                                                }
                                            };
                                        }
                                    });
                                },
get: function(index) {
                                    return {
                                        flatMap: function(fn) {
                                            if (!found_entity) return { orElse: function(def) { return def; } };
                                            var flatMapKey = null;
                                            var entityExpr = {
                                                getNumber: function(key) {
                                                    flatMapKey = key;
                                                    return {
                                                        map: function(cb3) {
                                                            if (!found_entity.numberMap || found_entity.numberMap[key] === undefined) return { orElse: function(def) { return def; } };
                                                           var result = cb3({
                                                                 sum: function(s) {
                                                                     found_entity.numberMap[key] = Number(found_entity.numberMap[key]) + Number(s);
                                                                 },
                                                                 divide: function(d) {
                                                                     var numVal = found_entity.numberMap[key];
                                                                     var remainder = Number(numVal) % Number(d);
                                                                     return {
                                                                         isEqualTo: function(target) {
                                                                             var isEqual = (remainder === Number(target));
                                                                             return {
                                                                                 ifTrue: function(cb) { if (isEqual && typeof cb === 'function') cb(); },
                                                                                 ifFalse: function(cb) { if (!isEqual && typeof cb === 'function') cb(); },
                                                                                 orElse: function(def) { return isEqual ? { value: true, ifTrue: function(cb) { if (typeof cb === 'function') cb(); }, ifFalse: function(cb) {} } : def; }
                                                                             };
                                                                         }
                                                                     };
                                                                 },
                                                                 isLessOrEqualTo: function(target) {
                                                                      return Number(found_entity.numberMap[key]) <= Number(target);
                                                                  },
                                                                  modulo: function(d) {
                                                                      var remainder = Number(found_entity.numberMap[key]) % Number(d);
                                                                      return {
                                                                          isEqualTo: function(target) {
                                                                              var isEqual = (remainder === Number(target));
                                                                              return {
                                                                                  ifTrue: function(cb) { if (isEqual && typeof cb === 'function') cb(); },
                                                                                  ifFalse: function(cb) { if (!isEqual && typeof cb === 'function') cb(); },
                                                                                  orElse: function(def) { return isEqual ? { value: true, ifTrue: function(cb) { if (typeof cb === 'function') cb(); }, ifFalse: function(cb) {} } : def; }
                                                                              };
                                                                          }
                                                                      };
                                                                  }
                                                              });
                                                             return result || { orElse: function(def) { return def; } };
                                                         }
                                                     };
                                                 }
                                             };
                                             var result = fn(entityExpr);
                                           return {
                                                 map: function(cb) {
                                                     if (result && typeof result.map === 'function') {
                                                         return result.map(cb);
                                                     }
                                                     return { orElse: function(def) { return def; } };
                                                 },
                                                 orElse: function(def) { return def; },
isCondition: function(condFn) {
                                                       var numVal = found_entity.numberMap && flatMapKey !== null ? found_entity.numberMap[flatMapKey] : 0;
                                                      var numWrapper = { isLessOrEqualTo: function(t) { return Number(numVal) < Number(t); } };
                                                     var isTrue = condFn(numWrapper);
                                                     return {
                                                         getOnTrueOrFalse: function(trueVal, falseVal) {
                                                             return isTrue ? trueVal : falseVal;
                                                         }
                                                     };
                                                 }
                                             };
                                        },
                                        map: function(cb) {
                                            if (!found_entity) return;
                                            cb({
                                                getText: function(key) {
                                                    return {
                                                        ifPresent: function(cb2) {
                                                            if (found_entity.textMap && found_entity.textMap[key] !== undefined) {
                                                                cb2({
                                                                    get: function() { return found_entity.textMap[key]; },
                                                                    set: function(val) {
                                                                        if (!found_entity.textMap) found_entity.textMap = {};
                                                                        found_entity.textMap[key] = val;
                                                                    }
                                                                });
                                                            }
                                                        }
                                                    };
                                                }
                                            });
                                        }
                                    };
                                }
                            };
                        },
                        emitEvent: function(name, payload) {
                            globalThis.__pendingEffects = globalThis.__pendingEffects || [];
                            globalThis.__pendingEffects.push(name);
                            // Try to execute the effect immediately to get its condition result
                            var target = null;
                            var evs = globalThis.__registeredEvents || [];
                            for (var i = 0; i < evs.length; i++) {
                                if (evs[i] && evs[i].name === name) { target = evs[i]; break; }
                            }
                            var conditionMet = false;
                            if (target) {
                                try {
                                    var prepared = null;
                                    if (typeof target.prepare === 'function') {
                                        prepared = target.prepare(globalThis.__context);
                                    }
                                    // Check if prepared has a condition value (truthy check)
                                    if (prepared && typeof prepared === 'object' && prepared.value) {
                                        // Execute apply if it exists
                                        if (typeof target.apply === 'function') {
                                            target.apply(globalThis.__context, prepared);
                                        }
                                    }
                                    // For condition check: if prepare returned something with a value, condition is met
                                    conditionMet = (prepared && typeof prepared === 'object' && prepared.value) || false;
                                } catch(e) {}
                            }
                            var _state = [conditionMet];
                            return {
                                ifTrue: function(cb) {
                                    if (_state[0] && typeof cb === 'function') { cb(); _state[0] = false; }
                                },
                                ifFalse: function(cb) {
                                    if (!_state[0] && typeof cb === 'function') cb();
                                }
                            };
                        }
                    };
                    })();
            "#)).ok();

            // Call prepare if exists
            ctx.with(|ctx| ctx.eval::<(), _>(r#"
                (function() {
                    if (globalThis.__foundEffect && typeof globalThis.__foundEffect.prepare === 'function') {
                        try { globalThis.__prepared = globalThis.__foundEffect.prepare(globalThis.__context); } catch(e) {}
                    }
                })();
            "#)).ok();

            // Call apply
            ctx.with(|ctx| ctx.eval::<(), _>(r#"
                (function() {
                    if (globalThis.__foundEffect && typeof globalThis.__foundEffect.apply === 'function') {
                        try { globalThis.__foundEffect.apply(globalThis.__context, globalThis.__prepared); } catch(e) {}
                    }
                })();
            "#)).ok();

            // Re-evaluate reoccurrence info after apply
            let reoccur_interval: f64 = ctx.with(|ctx| ctx.eval::<f64, _>(r#"
                (function() {
                    if (globalThis.__foundEffect && typeof globalThis.__foundEffect.reoccurAfterMs === 'function') {
                        try {
                            var result = globalThis.__foundEffect.reoccurAfterMs(globalThis.__context);
                            if (result && typeof result === 'object') {
                                if (typeof result.value === 'number') return result.value;
                                if (result.value === undefined) return -1;
                            }
if (typeof result === 'number') return result;
                         } catch(e) {}
                    }
                    return -1;
                })();
            "#)).unwrap_or(-1.0);

            // Read modified entity data from JS context
            let entity_data_updated = ctx.with(|ctx| ctx.eval::<String, _>("JSON.stringify(globalThis.__entityData || {})")).unwrap_or_else(|_| "{}".to_string());
            if let Ok(updated_data) = serde_json::from_str::<std::collections::HashMap<String, serde_json::Value>>(&entity_data_updated) {
                let mut number_data = crate::state::last_entity_number_data().lock().unwrap();
                let mut text_data = crate::state::last_entity_data().lock().unwrap();
                for (entity_id, entity_val) in updated_data.iter() {
                    let entity_map = number_data.entry(entity_id.clone()).or_insert_with(std::collections::HashMap::new);
                    if let Some(number_map) = entity_val.get("numberMap").and_then(|v| v.as_object()) {
                        for (k, v) in number_map.iter() {
                            if let Some(n) = v.as_f64() {
                                entity_map.insert(k.clone(), n);
                            }
                        }
                    }
                    // Also sync textMap back
                    if let Some(text_map) = entity_val.get("textMap").and_then(|v| v.as_object()) {
                        let text_entity_map = text_data.entry(entity_id.clone()).or_insert_with(std::collections::HashMap::new);
                        for (k, v) in text_map.iter() {
                            if let Some(s) = v.as_str() {
                                text_entity_map.insert(k.clone(), s.to_string());
                            }
                        }
                    }
                }
            }

  // Schedule reoccurrence if applicable
            if reoccur_interval > 0.0 {
                let interval = reoccur_interval as i64;
                let next_exec_time = ((current_elapsed / interval) + 1) * interval;
                crate::state::add_scheduled_effect(
                    effect_name.clone(),
                    serde_json::Value::Object(serde_json::Map::new()),
                    next_exec_time,
                    interval,
                );
            }

            // Collect logs from effect execution
            let logs_json = ctx.with(|ctx| ctx.eval::<String, _>("JSON.stringify(globalThis.__logs || [])")).unwrap_or_else(|_| "[]".to_string());
            if let Ok(logs_vec) = serde_json::from_str::<Vec<String>>(&logs_json) {
                for l in logs_vec.iter() {
                    runtime_log!("{}", l);
                }
            }
        }

        Ok(())
}

pub fn process_scheduled_effects(files: &std::collections::HashMap<String, String>, current_elapsed: i64) -> Result<()> {
    let due_effects = crate::state::get_due_scheduled_effects(current_elapsed);

    if due_effects.is_empty() {
        return Ok(());
    }

    for scheduled in due_effects.iter() {
        // Create a runtime and context
        let (_rt, ctx) = prepare_runtime_and_ctx()?;
        install_host_api(&ctx)?;
        let source = select_entry_source(files);
        let _transformed = eval_entry_in_ctx(&ctx, &source)?;

        // Sync entity data from Rust to JS
        let number_data = crate::state::last_entity_number_data().lock().unwrap().clone();
        // Reset textMap to initial state before scheduled effects (so child effects can set it fresh)
        let initial_text_data = crate::state::initial_entity_data().lock().unwrap().clone();
        *crate::state::last_entity_data().lock().unwrap() = initial_text_data.clone();
        let text_data = crate::state::last_entity_data().lock().unwrap().clone();
        let entity_data_json: std::collections::HashMap<String, serde_json::Value> = number_data.iter().map(|(entity_id, props)| {
            let mut nm = serde_json::Map::new();
            for (k, v) in props.iter() {
                if let Some(n) = serde_json::Number::from_f64(*v) {
                    nm.insert(k.clone(), serde_json::Value::Number(n));
                }
            }
            let mut obj = serde_json::Map::new();
            obj.insert("numberMap".to_string(), serde_json::Value::Object(nm));
            // Also include textMap if present
            if let Some(text_props) = text_data.get(entity_id) {
                let mut tm = serde_json::Map::new();
                for (k, v) in text_props.iter() {
                    tm.insert(k.clone(), serde_json::Value::String(v.clone()));
                }
                obj.insert("textMap".to_string(), serde_json::Value::Object(tm));
            }
            (entity_id.clone(), serde_json::Value::Object(obj))
        }).collect();
        let entity_data_json_str = serde_json::to_string(&entity_data_json).unwrap_or_else(|_| "{}".to_string());
        ctx.with(|ctx| ctx.eval::<(), _>(format!("globalThis.__entityData = {}; ", entity_data_json_str))).ok();

        // Find the effect and store it globally
        let lookup_script = format!(r#"
            (function() {{
                var evs = globalThis.__registeredEvents || [];
                for (var i = 0; i < evs.length; i++) {{
                    if (evs[i] && evs[i].name === '{}') {{
                        globalThis.__foundEffect = evs[i];
                        break;
                    }}
                }}
            }})();
        "#, scheduled.name);
        if let Err(_e) = ctx.with(|ctx| ctx.eval::<(), _>(lookup_script)) {
            continue;
        }

        let found_check = ctx.with(|ctx| ctx.eval::<bool, _>("globalThis.__foundEffect !== undefined")).unwrap_or(false);
        if !found_check {
            continue;
        }

        // Build the context
        ctx.with(|ctx| ctx.eval::<(), _>(r#"
            (function() {
                var found_entity = null;
                for (var eid in globalThis.__entityData) {
                    found_entity = globalThis.__entityData[eid];
                    break;
                }
                globalThis.__context = {
                    getEntityBy: function(filter) {
                        return {
                            map: function(cb) {
                                if (!found_entity) return;
                                cb({
                                    getNumber: function(key) {
                                        return {
                                            map: function(cb3) {
                                                if (!found_entity.numberMap || found_entity.numberMap[key] === undefined) return { orElse: function(def) { return def; } };
                                                var result = cb3({
                                                    sum: function(s) {
                                                        found_entity.numberMap[key] = Number(found_entity.numberMap[key]) + Number(s);
                                                    },
                                                    isLessOrEqualTo: function(target) {
                                                        return Number(found_entity.numberMap[key]) <= Number(target);
                                                    }
                                                });
                                                return result || { orElse: function(def) { return def; } };
                                            }
                                        };
                                    },
                                    getText: function(key) {
                                        return {
                                            ifPresent: function(cb2) {
                                                if (found_entity.textMap && found_entity.textMap[key] !== undefined) {
                                                    cb2({
                                                        get: function() { return found_entity.textMap[key]; },
                                                        set: function(val) {
                                                            if (!found_entity.textMap) found_entity.textMap = {};
                                                            found_entity.textMap[key] = val;
                                                        }
                                                    });
                                                }
                                            }
                                        };
                                    }
                                });
                            },
                            get: function(index) {
                                return {
                                    flatMap: function(fn) {
                                        if (!found_entity) return { orElse: function(def) { return def; } };
                                        var schedFlatMapKey = null;
                                        var entityExpr = {
                                            getNumber: function(key) {
                                                schedFlatMapKey = key;
                                                return {
                                                    map: function(cb3) {
                                                        if (!found_entity.numberMap || found_entity.numberMap[key] === undefined) return { orElse: function(def) { return def; } };
                                                        var result = cb3({
                                                            sum: function(s) {
                                                                found_entity.numberMap[key] = Number(found_entity.numberMap[key]) + Number(s);
                                                            },
                                                            divide: function(d) {
                                                                var numVal = found_entity.numberMap[key];
                                                                var remainder = Number(numVal) % Number(d);
                                                                return {
                                                                    isEqualTo: function(target) {
                                                                        var isEqual = (remainder === Number(target));
                                                                        return {
                                                                            ifTrue: function(cb) { if (isEqual && typeof cb === 'function') cb(); },
                                                                            ifFalse: function(cb) { if (!isEqual && typeof cb === 'function') cb(); },
                                                                            orElse: function(def) { return isEqual ? { value: true, ifTrue: function(cb) { if (typeof cb === 'function') cb(); }, ifFalse: function(cb) {} } : def; }
                                                                        };
                                                                    }
                                                                };
                                                            },
                                                            isLessOrEqualTo: function(target) {
                                                                return Number(found_entity.numberMap[key]) <= Number(target);
                                                            },
                                                            modulo: function(d) {
                                                                var remainder = Number(found_entity.numberMap[key]) % Number(d);
                                                                return {
                                                                    isEqualTo: function(target) {
                                                                        var isEqual = (remainder === Number(target));
                                                                        return {
                                                                            ifTrue: function(cb) { if (isEqual && typeof cb === 'function') cb(); },
                                                                            ifFalse: function(cb) { if (!isEqual && typeof cb === 'function') cb(); },
                                                                            orElse: function(def) { return isEqual ? { value: true, ifTrue: function(cb) { if (typeof cb === 'function') cb(); }, ifFalse: function(cb) {} } : def; }
                                                                        };
                                                                    }
                                                                };
                                                            }
                                                        });
                                                        return result || { orElse: function(def) { return def; } };
                                                    }
                                                };
                                            }
                                        };
                                        var result = fn(entityExpr);
                                        return {
                                            map: function(cb) {
                                                if (result && typeof result.map === 'function') {
                                                    return result.map(cb);
                                                }
                                                return { orElse: function(def) { return def; } };
                                            },
                                            orElse: function(def) { return def; },
isCondition: function(condFn) {
                                                 var numVal = found_entity.numberMap && schedFlatMapKey !== null ? found_entity.numberMap[schedFlatMapKey] : 0;
                                                 var numWrapper = { isLessOrEqualTo: function(t) { return Number(numVal) < Number(t); } };
                                                var isTrue = condFn(numWrapper);
                                                return {
                                                    getOnTrueOrFalse: function(trueVal, falseVal) {
                                                        return isTrue ? trueVal : falseVal;
                                                    }
                                                };
                                            }
                                        };
                                    },
                                    map: function(cb) {
                                        if (!found_entity) return;
                                        cb({
                                            getText: function(key) {
                                                return {
                                                    ifPresent: function(cb2) {
                                                        if (found_entity.textMap && found_entity.textMap[key] !== undefined) {
                                                            cb2({
                                                                get: function() { return found_entity.textMap[key]; },
                                                                set: function(val) {
                                                                    if (!found_entity.textMap) found_entity.textMap = {};
                                                                    found_entity.textMap[key] = val;
                                                                }
                                                            });
                                                        }
                                                    }
                                                };
                                            }
                                        });
                                    }
                                };
                            }
                        };
                    },
                    emitEvent: function(name, payload) {
                        globalThis.__pendingEffects = globalThis.__pendingEffects || [];
                        globalThis.__pendingEffects.push(name);
                        // Try to evaluate child effect inline to determine condition
                        var target = null;
                        var evs = globalThis.__registeredEvents || [];
                        for (var i = 0; i < evs.length; i++) {
                            if (evs[i] && evs[i].name === name) { target = evs[i]; break; }
                        }
                        var _cond = false;
                        if (target) {
                            try {
                                var childPrepared = null;
                                if (typeof target.prepare === 'function') {
                                    childPrepared = target.prepare(globalThis.__context);
                                }
                                if (childPrepared && typeof childPrepared === 'object' && childPrepared.value) {
                                    _cond = true;
                                    if (typeof target.apply === 'function') {
                                        target.apply(globalThis.__context, childPrepared);
                                    }
                                }
                            } catch(e) {}
                        }
                        var _state = [_cond];
                        return {
                            ifTrue: function(cb) { if (_state[0] && typeof cb === 'function') { cb(); _state[0] = false; } },
                            ifFalse: function(cb) { if (!_state[0] && typeof cb === 'function') cb(); }
                        };
                    }
                };
            })();
        "#)).ok();

        // Evaluate reoccurAfterMs BEFORE apply (pre-gate)
        let reoccur_interval_pre: f64 = ctx.with(|ctx| ctx.eval::<f64, _>(r#"
            (function() {
                if (globalThis.__foundEffect && typeof globalThis.__foundEffect.reoccurAfterMs === 'function') {
                    try {
                        var result = globalThis.__foundEffect.reoccurAfterMs(globalThis.__context);
                        if (result && typeof result === 'object') {
                            if (typeof result.value === 'number') return result.value;
                            if (result.value === undefined) return -1;
                        }
                        if (typeof result === 'number') return result;
                    } catch(e) {}
                }
                return -1;
            })();
        "#)).unwrap_or(-1.0);

        // If reoccurAfterMs returns <= 0, skip and remove from scheduled
        if reoccur_interval_pre <= 0.0 {
            crate::state::remove_scheduled_effect(&scheduled.name);
            continue;
        }

        // Call prepare
        ctx.with(|ctx| ctx.eval::<(), _>(r#"
            (function() {
                if (globalThis.__foundEffect && typeof globalThis.__foundEffect.prepare === 'function') {
                    try { globalThis.__prepared = globalThis.__foundEffect.prepare(globalThis.__context); } catch(e) {}
                }
            })();
        "#)).ok();

        // Call apply
        ctx.with(|ctx| ctx.eval::<(), _>(r#"
            (function() {
                if (globalThis.__foundEffect && typeof globalThis.__foundEffect.apply === 'function') {
                    try { globalThis.__foundEffect.apply(globalThis.__context, globalThis.__prepared); } catch(e) {}
                }
            })();
        "#)).ok();

        // Re-evaluate reoccurAfterMs after apply (post-gate) to determine if we should reschedule
        let reoccur_interval_post: f64 = ctx.with(|ctx| ctx.eval::<f64, _>(r#"
            (function() {
                if (globalThis.__foundEffect && typeof globalThis.__foundEffect.reoccurAfterMs === 'function') {
                    try {
                        var result = globalThis.__foundEffect.reoccurAfterMs(globalThis.__context);
                        if (result && typeof result === 'object') {
                            if (typeof result.value === 'number') return result.value;
                            if (result.value === undefined) return -1;
                        }
                        if (typeof result === 'number') return result;
                    } catch(e) {}
                }
                return -1;
            })();
        "#)).unwrap_or(-1.0);

        // Only reschedule if post-apply reoccurAfterMs is positive
        if reoccur_interval_post > 0.0 {
            let current_elapsed_for_scheduling = current_elapsed;
            let interval_for_scheduling = reoccur_interval_post as i64;
            let next_exec_time = ((current_elapsed_for_scheduling / interval_for_scheduling) + 1) * interval_for_scheduling;
            crate::state::add_scheduled_effect(
                scheduled.name.clone(),
                serde_json::Value::Object(serde_json::Map::new()),
                next_exec_time,
                interval_for_scheduling,
            );
        }

        // Read modified entity data
        let entity_data_updated = ctx.with(|ctx| ctx.eval::<String, _>("JSON.stringify(globalThis.__entityData || {})")).unwrap_or_else(|_| "{}".to_string());
        if let Ok(updated_data) = serde_json::from_str::<std::collections::HashMap<String, serde_json::Value>>(&entity_data_updated) {
            let mut number_data = crate::state::last_entity_number_data().lock().unwrap();
            let mut text_data = crate::state::last_entity_data().lock().unwrap();
            for (entity_id, entity_val) in updated_data.iter() {
                let entity_map = number_data.entry(entity_id.clone()).or_insert_with(std::collections::HashMap::new);
                if let Some(number_map) = entity_val.get("numberMap").and_then(|v| v.as_object()) {
                    for (k, v) in number_map.iter() {
                        if let Some(n) = v.as_f64() {
                            entity_map.insert(k.clone(), n);
                        }
                    }
                }
                // Also sync textMap back
                if let Some(text_map) = entity_val.get("textMap").and_then(|v| v.as_object()) {
                    let text_entity_map = text_data.entry(entity_id.clone()).or_insert_with(std::collections::HashMap::new);
                    for (k, v) in text_map.iter() {
                        if let Some(s) = v.as_str() {
                            text_entity_map.insert(k.clone(), s.to_string());
                        }
                    }
                }
            }
        }

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


