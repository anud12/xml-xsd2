use anyhow::{anyhow, Result};
use crate::js_runtime::{create_runtime, create_context};
use rquickjs::{Context, Runtime};
use crate::js_host_api::{install_host_api, Declarations};


fn create_rt_ctx_and_install(_source: &str) -> Result<(Runtime, Context)> {
    let rt = create_runtime()?;
    let ctx = create_context(&rt)?;
    install_host_api(&ctx)?;
    Ok((rt, ctx))
}

pub fn extract_from_source(source: &str) -> Result<Declarations> {
    let (_rt, ctx) = create_rt_ctx_and_install(source)?;

    // Patch user source to remove destructured `string`/`number` from parameters.
    let patched_source = patch_user_source(&source);
    eprintln!("[DEBUG] patched source first 200 chars: {}", &patched_source[..patched_source.len().min(200)]);

    // Build unified script that does EVERYTHING in a single eval() call.
    let unified_script = build_unified_flat(&patched_source);

    let json_str = ctx.with(|ctx| ctx.eval::<String, _>(unified_script.clone()))
        .map_err(|e| anyhow!("extract_from_source JS error: {}", e))?;

    // Debug: show first 500 chars of unified script for debugging.
    {
        let preview = &unified_script[..unified_script.len().min(500)];
        eprintln!("[DEBUG] unified_script preview:\n{}\n---END PREVIEW---", preview);
    }

    if let Ok(dec) = serde_json::from_str::<Declarations>(&json_str) {
        eprintln!("[DEBUG extract_from_source] entity_data JSON: {}", dec.entity_data);
    }

    let dec: Declarations = serde_json::from_str(&json_str)
        .map_err(|e| anyhow!("extract_from_source deserialization error: {}", e))?;

    Ok(dec)
}

/// Patch user JS source to remove `string`, `number` from destructuring params only.
fn patch_user_source(source: &str) -> String {
    // Replace ({string, number, ...hostApi}) with (...hostApi, string, number) 
    // so that hostApi is preserved but string/number don't shadow globals.
    // Actually simpler: just remove string and number from the destructuring while keeping hostApi.
    let result = source
        .replace("({string, number, ...hostApi})", "({...hostApi})")
        .replace("({ string, number, ...hostApi })", "({...hostApi})");

    // Also handle cases where only one is present.
    let result2 = result.replace("({string, ...hostApi})", "({...hostApi})")
                        .replace("({number, ...hostApi})", "({...hostApi})");

    result2
}fn build_unified_flat(source: &str) -> String {
    // Convert export default to var for QuickJS compatibility.
    let transformed = if source.contains("export default") {
        source.replace("export default", "var __module_default =")
    } else {
        source.to_string()
    };

    let mut script = String::new();

    // Step 0: Define host API inline so it lives in the same eval context as user code.
    // This ensures globals like __registeredEvents persist across registerEffect calls
    // since everything runs within ONE ctx.eval() call.
    script.push_str(r#"globalThis.host = {
emitEvent(name) { globalThis.__pendingEffects = globalThis.__pendingEffects || []; globalThis.__pendingEffects.push({ name: (name && typeof name === 'object' && typeof name.name === 'string') ? name.name : String(name), payload: {} }); },
registerEvent(ev) { let n='unknown'; if(ev&&typeof ev==='object'){if(typeof ev.name==='string')n=ev.name;else if(ev.apply&&typeof ev.apply==='function'&&ev.apply.name)n=ev.apply.name;} globalThis.__registeredEvents = globalThis.__registeredEvents || []; globalThis.__registeredEvents.push(ev); },
registerAction(ev) { let n='unknown'; if(ev&&typeof ev==='object'){if(typeof ev.name==='string')n=ev.name;else if(ev.apply&&typeof ev.apply==='function'&&ev.apply.name)n=ev.apply.name;} globalThis.__registeredActions = globalThis.__registeredActions || []; globalThis.__registeredActions.push(ev); },
registerEffect(ev) { let n='unknown'; if(ev&&typeof ev==='object'){if(typeof ev.name==='string')n=ev.name;else if(ev.apply&&typeof ev.apply==='function'&&ev.apply.name)n=ev.apply.name;} globalThis.__registeredEvents = globalThis.__registeredEvents || []; globalThis.__registeredEvents.push(ev); },
registerPanel(p) { try{var t=p;if(p&&typeof p==='object')t=JSON.stringify(p);else if(typeof p==='string')t=JSON.stringify({id:p});else t=JSON.stringify({id:String(p)});globalThis.__registeredPanels=globalThis.__registeredPanels||[];globalThis.__registeredPanels.push(t);}catch(e){}},
createEntity(obj) { globalThis.__createdEntities = globalThis.__createdEntities || []; try{if(obj&&typeof obj==='object'&&typeof obj.firstName==='string')globalThis.__createdEntities.push({firstName:obj.firstName});else globalThis.__createdEntities.push(obj);}catch(e){} },
setEntity(id,data) { globalThis.__entityData=globalThis.__entityData||{}; if(typeof id==='string'&&data&&typeof data==='object')globalThis.__entityData[id]=data; },
log(msg) { try{globalThis.__logs=globalThis.__logs||[];globalThis.__logs.push(String(msg));}catch(e){} },
number:{of:function(n){return n;}}, string:{of:function(s){return s;}}, texture:{of:function(t){return t;}} };

// Top-level globals for user module code that references string.of() / number.of() directly.
globalThis.string = { of: function(s) { return s; } };
globalThis.number = { of: function(n) { return n; } };
"#);

    // Step 1: Define string/number as top-level globals for closure capture.
    // Step 2: Evaluate user module source. Effects registered here capture the global `string`/`number`.
    script.push_str(&transformed);
    script.push('\n');

    // Step 3: Call __module_default and run effects - all in same scope as step 1-2.
    let effect_context = effect_context_js();

    script.push_str(r#"globalThis.__entityData = globalThis.__entityData || {};
(globalThis.__logs||[]).push("DEBUG: about to call localSetEntity");
function localSetEntity(id, data) {
  (globalThis.__logs||[]).push("DEBUG: localSetEntity called with id=" + id);
  if (typeof id === 'string' && data && typeof data === 'object') {
    globalThis.__entityData[id] = JSON.parse(JSON.stringify(data));
  }
}

(globalThis.__logs||[]).push("DEBUG: __module_default type=" + (typeof __module_default) + ", isFunction=" + (typeof __module_default === 'function'));
if (typeof __module_default === 'function') {
  try {
    var unifiedHostApi = {
      string: globalThis.string,
      number: globalThis.number,
      entity: { create: function() { return { withTextMap: function(tm){ return tm; } }; }, filter: { create: function() { return { byId: function(fn){ return fn; } }; } } },
      textMap: { create: function() { return { put: function(k,v){ const o={}; o[k]=v; return o; } }; } },
      texture: { of: function(p){ return p; } },
      emitEvent: host.emitEvent, registerEvent: host.registerEvent, registerAction: host.registerAction,
      registerEffect: host.registerEffect, registerPanel: host.registerPanel,
      setEntity: localSetEntity, log: host.log
    };
    globalThis.__moduleHostApi = unifiedHostApi;
    (globalThis.__logs||[]).push("DEBUG: calling __module_default with unifiedHostApi");
    __module_default(unifiedHostApi);
    (globalThis.__logs||[]).push("DEBUG: after __module_default, registeredEvents=" + JSON.stringify(globalThis.__registeredEvents || []));
  } catch(e) {
    (globalThis.__logs||[]).push("DEBUG ERROR calling __module_default: " + e.message);
  }
} else { globalThis.__moduleHostApi = host; }

// Build entity store from __entityData. Use direct refs for mutation propagation.
globalThis.__entityStore = [];
if (globalThis.__entityData) {
  for (var id in globalThis.__entityData) {
    var entryObj = globalThis.__entityData[id];
    entryObj.textMap_name = id;
    globalThis.__entityStore.push(entryObj);
  }
}

"#);

    script.push_str(effect_context_js());

    script.push_str(r#"// Execute all registered effects.
var regEffects = globalThis.__registeredEvents || [];
(globalThis.__logs||[]).push("DEBUG_EFFECTS: registered count=" + (regEffects ? regEffects.length : "null"));
for (var i = 0; i < regEffects.length; i++) {
  (globalThis.__logs||[]).push("DEBUG_EFFECTS: entry[" + i + "]=" + JSON.stringify(regEffects[i]));
  if (regEffects[i] && typeof regEffects[i].apply === 'function') {
    try {
      var efCtx = buildEffectContext();
      var prepared = null;
      if (typeof regEffects[i].prepare === 'function') {
        try { prepared = regEffects[i].prepare({}); } catch(ex) {}
      }
      (globalThis.__logs||[]).push("DEBUG_EFFECTS: calling apply for '" + regEffects[i].name + "'");
      regEffects[i].apply(efCtx, prepared);
      (globalThis.__logs||[]).push("DEBUG_EFFECTS: __entityData after effect=" + JSON.stringify(globalThis.__entityData));
    } catch(e) { (globalThis.__logs||[]).push("DEBUG_EFFECTS: error in effect '" + regEffects[i].name + "': " + e.message); }
  } else { (globalThis.__logs||[]).push("DEBUG_EFFECTS: skipping entry, hasApply=" + (regEffects[i] && typeof regEffects[i].apply === 'function')); }
}

// Sync entity store changes back to __entityData after effects ran.
if (globalThis.__entityData && globalThis.__entityStore) {
  for (var k in globalThis.__entityData) {
    for (var i = 0; i < globalThis.__entityStore.length; i++) {
      var entry = globalThis.__entityStore[i];
      if (entry && entry.textMap_name === k) {
        globalThis.__entityData[k] = JSON.parse(JSON.stringify(entry));
      }
    }
  }
}

// Extract declarations and return as JSON string.
var out = { events: [], actions: [], functions: [], entities: [], creators: {}, emits: {}, panels: [], entity_data: {} };
var re = globalThis.__registeredEvents || [];
out.events = re.map(function(ev) { if (typeof ev === 'string') return ev; if (ev && typeof ev === 'object') { if (typeof ev.name === 'string') return ev.name; if (ev.apply && typeof ev.apply === 'function' && ev.apply.name) return ev.apply.name; try { return JSON.stringify(ev); } catch(e) { return String(ev); } } return String(ev); });
var ra = globalThis.__registeredActions || [];
out.actions = ra.map(function(ev) { if (typeof ev === 'string') return ev; if (ev && typeof ev === 'object') { if (typeof ev.name === 'string') return ev.name; if (ev.apply && typeof ev.apply === 'function' && ev.apply.name) return ev.apply.name; try { return JSON.stringify(ev); } catch(e) { return String(ev); } } return String(ev); });
var ce = globalThis.__createdEntities || [];
out.entities = ce.map(function(en) { if (typeof en === 'string') return en; if (en && typeof en === 'object') { if (typeof en.firstName === 'string') return en.firstName; try { return JSON.stringify(en); } catch(e) { return String(en); } } return String(en); });
out.logs = globalThis.__logs || [];
out.functions = Object.getOwnPropertyNames(globalThis).filter(function(k) { try { return typeof globalThis[k] === 'function' && !k.startsWith('_') && k !== 'host'; } catch(e) { return false; } }).sort();
out.creators = globalThis.__createdEntitiesFor || {};
out.emits = globalThis.__emitsMap || {};
out.panels = globalThis.__registeredPanels || [];
out.entity_data = globalThis.__entityData || {};

JSON.stringify(out)
"#);

    script
}

fn effect_context_js() -> &'static str {
    r#"function buildEffectContext() {
  var hostApi = globalThis.host || {};
  function getEntityBy(filterFn) {
    var targetIds = [];
    if (filterFn && typeof filterFn.toString === 'function') {
      var src = filterFn.toString();
      var re1 = /string\.of\(\s*["']([^"']+)["']\s*\)/g;
      var m;
      while ((m = re1.exec(src)) !== null) targetIds.push(m[1]);
    }

    // Return DIRECT REFERENCES so mutations propagate.
    var matchedEntities = [];
    for (var i = 0; i < globalThis.__entityStore.length; i++) {
      var e = globalThis.__entityStore[i];
      if (!e || !e.textMap_name) continue;
      if (targetIds.length === 0 || targetIds.indexOf(e.textMap_name) >= 0) {
        matchedEntities.push(e);
      }
    }

    return {
      map: function(cb) {
        for (var j = 0; j < matchedEntities.length; j++) cb(makeEntityWrapper(matchedEntities[j]));
      },
      randomElement: function() {
        var ent = matchedEntities.length > 0 ? matchedEntities[0] : null;
        return { ifPresent: function(cb2) {
          if (ent) cb2(makeEntityWrapper(ent)); else cb2(null);
        }};
      }
    };
  }

  function makeValueWrapper(v, entityRef, keyRef) {
    return {
      map: function(cb) { cb(v); },
      sum: function(addend) {
        var nv = (v || 0) + addend;
        if (entityRef && entityRef.numberMap && keyRef) entityRef.numberMap[keyRef] = nv;
        return makeValueWrapper(nv, entityRef, keyRef);
      }
    };
  }

  function makeEntityWrapper(entity) {
    return {
      getNumber: function(key) {
        var val = null;
        if (entity.numberMap && entity.numberMap[key] !== undefined) val = entity.numberMap[key];
        return makeValueWrapper(val, entity, key);
      },
      getText: function(key) {
        var val = null;
        if (entity.textMap && entity.textMap[key] !== undefined) val = entity.textMap[key];
        return { concat: function(s) { return String(val || '') + s; }, map: function(cb) { cb(val); }, ifPresent: function(cb2) { if (val != null) cb2(String(val)); else cb2(null); }};
      }
    };
  }

  return {
    getEntityBy: getEntityBy,
    emitEffect: function(name, payload) { globalThis.__pendingEffects = globalThis.__pendingEffects || []; globalThis.__pendingEffects.push({ name: name, payload: payload }); },
    createEntity: function(obj) { globalThis.__createdEntities = globalThis.__createdEntities || []; globalThis.__entityStore.push(JSON.parse(JSON.stringify(obj))); },
    entity: { filter: { create: function() { return { byId: function(fn) { return fn; } }; } } },
    string: { of: function(s) { return s; } },
    number: { of: function(n) { return n; } }
  };
}"#
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
      if (keys.length === 1) { const k=keys[0]; const v=String(obj[k]); const o={};o[k]=v;globalThis.__entityStore.push(o);globalThis.__createdEntities.push(v);return; }
      if (typeof obj.firstName==='string'){globalThis.__entityStore.push({firstName:obj.firstName});globalThis.__createdEntities.push(obj.firstName);return;}
    } else { globalThis.__entityStore.push({textMap_name:String(obj)});globalThis.__createdEntities.push(String(obj)); }
  }

  function logFn(msg){globalThis.__logs=globalThis.__logs||[];globalThis.__logs.push(String(msg));}

  function findEffectByName(name) { for(let e of evs){if(typeof e==='string'){if(e===name)return e;}else if(e&&typeof e==='object'){if(typeof e.name==='string'&&e.name===name)return e;}} return null;}

  function buildEventContext() {
    return { createEntity: recordCreated, log:logFn,getEntityBy: function(filter) { return { randomElement: function(){return{ifPresent:function(cb){let found=null;try{let src=filter.toString();let m=src.match(/isContainingExactly\(hostApi\.string\.of\("([^"]+)"\)\)/);if(m){const v=m[1];for(let i=0;i<globalThis.__entityStore.length;i++){const e=globalThis.__entityStore[i];for(let key in e){if(String(e[key]).includes(v)){found=e;break;}}if(found)break;}}}catch(e){}if(!found&&globalThis.__entityStore.length>0)found=globalThis.__entityStore[0];if(!found)return cb(null);const wrapper={getText:function(key){return{ifPresent:function(cb2){const nameObj={concat:function(s){try{if(found&&typeof found==='object'){if(key in found){found[key]=String(found[key])+String(s);}else{const pk=Object.keys(found)[0];if(pk)found[pk]=String(found[pk])+String(s);}}}catch(e){}}};cb2(nameObj);}}},ifPresent:function(cb3){cb3(wrapper);}};cb(wrapper);}}}};}};}

  function applyEffectByName(name, payload) { const ef=findEffectByName(name);if(!ef)return;let prepared;if(typeof ef.prepare==='function'){try{prepared=ef.prepare(payload);}catch(e){}}if(typeof ef.apply==='function'){try{ef.apply(buildEventContext(),prepared);}catch(e){}}}

  function emitEvent(name, payload) { globalThis.__pendingEffects=globalThis.__pendingEffects||[];globalThis.__pendingEffects.push({name: name,payload:payload}); }
  globalThis.__processPendingEffects=function(){const p=globalThis.__pendingEffects||[];globalThis.__pendingEffects=[];for(let i=0;i<p.length;i++)applyEffectByName(p[i].name,p[i].payload);};

  let actionObj=null;for(let a of acts){if(typeof a==='string'){if(a===actionName){actionObj=a;break;}}else if(a&&typeof a==='object'){if(typeof a.name==='string'&&a.name===actionName){actionObj=a;break;}}}

  if(actionObj){const wef=function(n,p){return emitEvent(n,p);};const ctx_obj={emitEffect:wef,emitEvent:wef,createEntity:recordCreated,log:logFn,entity:{create:()=>({withTextMap:tm=>tm}),filter:{create:()=>({byId:fn=>fn})}},textMap:{create:()=>({put:(k,v)=>{const o={};o[k]=v;return o}})},string:{of:s=>s},number:{of:n=>n}};try{if(typeof actionObj==='object'&&typeof actionObj.apply==='function')actionObj.apply(ctx_obj);else if(typeof actionObj==='function')actionObj(ctx_obj);}catch(e){}}

  return JSON.stringify({created:globalThis.__createdEntities,store:globalThis.__entityStore,pendingEffects:globalThis.__pendingEffects||[]});
})(ACTION_PLACEHOLDER, STORE_PLACEHOLDER)"#
}


pub fn simulate_action(
    files: &std::collections::HashMap<String, String>,
    action_name: &str,
    initial_store: &[Vec<String>],
) -> Result<(Vec<String>, Vec<Vec<String>>)> {
    let (_rt, ctx) = prepare_runtime_and_ctx()?;
    install_host_api(&ctx)?;

    // Set up globals.
    let _ = ctx.with(|ctx| ctx.eval::<(), _>(r#"
      var string = { of: function(s){return s;} };
      var number = { of: function(n){return n;} };
    "#));

    let source = select_entry_source(files);
    // Evaluate user source with globals available.
    let transformed = if source.contains("export default") { source.replace("export default", "var __module_default =") } else { source.clone() };
    ctx.with(|ctx| ctx.eval::<(), _>(transformed))?;

    // Call __module_default to register actions, events, panels etc. into the JS context
    let _ = ctx.with(|ctx| ctx.eval::<(), _>(r#"if(typeof __module_default==='function'){try{__module_default(globalThis.host);}catch(e){}}"#));

    // Build entity store from __entityData for the simulation.
    let _ = ctx.with(|ctx| ctx.eval::<(), _>(r#"globalThis.__entityStore=[];if(globalThis.__entityData){for(var id in globalThis.__entityData){var e=JSON.parse(JSON.stringify(globalThis.__entityData[id]));e.textMap_name=id;globalThis.__entityStore.push(e);}}"#));

    let store_json = build_initial_store_json(initial_store)?;
    let action_js = serde_json::to_string(action_name)?;
    let script = sim_template_js().replace("ACTION_PLACEHOLDER", &action_js).replace("STORE_PLACEHOLDER", &store_json);

    let (result_json, logs_json) = run_simulation_and_collect(&ctx, &script)?;
    if let Ok(logs_vec) = serde_json::from_str::<Vec<String>>(&logs_json) { for l in logs_vec.iter() { if !l.starts_with("DEBUG_TEMPLATE:") { runtime_log!("{}", l); } } }

    let sim_val: serde_json::Value = serde_json::from_str(&result_json)?;
    let created: Vec<String> = sim_val.get("created").and_then(|v| v.as_array()).map(|a| a.iter().filter_map(|v| v.as_str()).map(|s| s.to_string()).collect()).unwrap_or_default();
    let store: Vec<serde_json::Value> = sim_val.get("store").and_then(|v| v.as_array()).map(|a| a.clone()).unwrap_or_default();
    let mut pe: Vec<String> = Vec::new();
    if let Some(p) = sim_val.get("pendingEffects").and_then(|v| v.as_array()) { for e in p { if let Some(n) = e.get("name").and_then(|v| v.as_str()) { pe.push(n.to_string()); } } }
    if !pe.is_empty() { crate::state::set_pending_effects(pe); }

    Ok((created, convert_store_values(&store)))
}

fn prepare_runtime_and_ctx() -> Result<(Runtime, Context)> { let rt = create_runtime()?; let ctx = create_context(&rt)?; Ok((rt, ctx)) }
fn build_initial_store_json(initial_store: &[Vec<String>]) -> Result<String> {
    use serde_json::Value;
    let mut store_array: Vec<Value> = Vec::new();
    for row in initial_store.iter() { if !row.is_empty() { let key=row[0].clone();let mut map=serde_json::Map::new();map.insert("textMap_name".to_string(),Value::String(key.clone()));map.insert(key.clone(),Value::String(key.clone()));store_array.push(Value::Object(map)); } }
    Ok(serde_json::to_string(&store_array)?)
}
fn run_simulation_and_collect(ctx: &Context, script: &str) -> Result<(String, String)> { let r=ctx.with(|c|c.eval::<String,_>(script))?;let l=ctx.with(|c|c.eval::<String,_>("JSON.stringify(globalThis.__logs||[])")).unwrap_or_default();Ok((r,l)) }

pub fn process_pending_effects(files: &std::collections::HashMap<String, String>) -> Result<()> {
    let effects = crate::state::pending_effects().lock().unwrap().clone(); if effects.is_empty() { return Ok(()); }
    crate::state::clear_pending_effects();
    let (_rt, ctx) = prepare_runtime_and_ctx()?; install_host_api(&ctx)?;
    // Set up globals for effect execution.
    let _ = ctx.with(|c| c.eval::<(),_>(r#"var string={of:function(s){return s;}};var number={of:function(n){return n;}}"#));

    // Evaluate module source to register effects/actions in this context
    let source = select_entry_source(files);
    if !source.is_empty() {
        let transformed = if source.contains("export default") { source.replace("export default", "var __module_default =") } else { source.clone() };
        ctx.with(|ctx| ctx.eval::<(), _>(transformed))?;
        // Call module entry point to register effects/actions
        let _ = ctx.with(|ctx| ctx.eval::<(), _>(r#"if(typeof __module_default==='function'){try{__module_default(globalThis.host);}catch(e){}}"#));
    }

    for effect_name in effects.iter() {
        // Build and execute the effect using buildEffectContext (avoid format! to prevent brace conflicts with JS code)
        let mut script = String::from("var buildEffectContext=");
        script.push_str(effect_context_js());
        script.push_str("(function(){var evs=globalThis.__registeredEvents||[];var f=null;for(var i=0;i<evs.length;i++){if(evs[i]&&typeof evs[i]==='object'&&evs[i].name==='");
        script.push_str(effect_name);
        script.push_str("'){f=evs[i];break;}}}if(f){if(typeof f.prepare==='function')try{f.prepare({})}catch(ex){}if(typeof f.apply==='function')try{f.apply(buildEffectContext())}catch(e){}}}");
        let _ = ctx.with(|c| c.eval::<(),_>(script));
    }

    // Read back entity_data mutations and update Rust state
    let entity_data_json = ctx.with(|c| c.eval::<String, _>("JSON.stringify(globalThis.__entityData||{})")).unwrap_or_else(|_| "{}".to_string());
    if let Ok(entities_val) = serde_json::from_str::<serde_json::Value>(&entity_data_json) {
        if let Some(entities_obj) = entities_val.as_object() {
            let mut text_data: std::collections::HashMap<String, std::collections::HashMap<String, String>> = crate::state::last_entity_data().lock().unwrap().clone();
            let mut number_data: std::collections::HashMap<String, std::collections::HashMap<String, f64>> = crate::state::last_entity_number_data().lock().unwrap().clone();

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

fn convert_store_values(values: &[serde_json::Value]) -> Vec<Vec<String>> {
