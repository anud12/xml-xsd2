use anyhow::Result;
use rquickjs::Context;
use serde::Deserialize;
use std::collections::HashMap;

/// Minimal shape describing declarations discovered in a script/context.
#[derive(Debug, Deserialize, serde::Serialize)]
pub struct Declarations {
    pub events: Vec<String>,
    pub actions: Vec<String>,
    pub creators: HashMap<String, Vec<String>>,
    pub emits: HashMap<String, Vec<String>>,
    pub functions: Vec<String>,
    pub entities: Vec<String>,
    pub logs: Vec<String>,
    pub panels: Vec<String>,
    #[serde(default)]
    pub entity_data: serde_json::Value,
}

/// Install a minimal, explicit host API into the provided QuickJS Context.
///
/// This keeps host wiring in JS (small, reviewable) and avoids complex Rust
/// lifetime-managed JS function closures. The installed API is intentionally
/// tiny: it logs event registration/emission and exposes a simple `string_of`.
fn host_api_script_part1() -> &'static str {
    "// Minimal host API expected by tests. All side-effects are explicit\n// console.log calls so test harness can observe them.\nglobalThis.host = {"
}

fn host_api_script_emit() -> &'static str {
    r#"emitEvent(name) { globalThis.__logs = globalThis.__logs || []; globalThis.__logs.push('DEBUG: emitEvent called'); globalThis.__pendingEffects = globalThis.__pendingEffects || []; globalThis.__pendingEffects.push({ name: (name && typeof name === 'object' && typeof name.name === 'string') ? name.name : String(name), payload: {} }); if (name && typeof name === 'object' && typeof name.name === 'string') { globalThis.__logs.push(`event: ${name.name}`); } else { globalThis.__logs.push(`event: ${String(name)}`); } },"#
}

fn host_api_script_scan_fn() -> &'static str {
    r#"const scanFn = (fn, owner) => { if (fn && typeof fn === 'function') { let src = fn.toString(); const re = /string\.of\(\s*\"([^\"]+)\"\s*\)/g; let m; while ((m = re.exec(src)) !== null) { globalThis.__createdEntitiesFor = globalThis.__createdEntitiesFor || {}; globalThis.__createdEntitiesFor[owner] = globalThis.__createdEntitiesFor[owner] || []; globalThis.__createdEntitiesFor[owner].push(m[1]); } const emitRe = /emitEvent\(\s*['\"]([^'\"]+)['\"]/g; let em; while ((em = emitRe.exec(src)) !== null) { globalThis.__emitsMap = globalThis.__emitsMap || {}; globalThis.__emitsMap[owner] = globalThis.__emitsMap[owner] || []; globalThis.__emitsMap[owner].push(em[1]); } } };"#
}

fn host_api_script_register_block(kind: &str) -> String {
    let label = if kind == "registerEvent" { "Events" } else if kind == "registerAction" { "Actions" } else { "Events" };
    let mut s = String::new();
    s.push_str(kind);
    s.push_str("(ev) { let n = 'unknown'; if (ev && typeof ev === 'object') { if (typeof ev.name === 'string') n = ev.name; else if (ev.apply && typeof ev.apply === 'function' && ev.apply.name) n = ev.apply.name; } else if (typeof ev === 'string') { n = ev; } globalThis.__logs = globalThis.__logs || []; ");
    s.push_str("globalThis.__logs.push(`");
    s.push_str(label);
    s.push_str(" registered: ${n}`); ");
    s.push_str(&format!("globalThis.__registered{} = globalThis.__registered{} || []; globalThis.__registered{}.push(ev); ", label, label, label));
    s.push_str("try { ");
    s.push_str(host_api_script_scan_fn());
    s.push_str(" let owner = n; scanFn(ev.prepare, owner); scanFn(ev.apply, owner); } catch(e) { /* ignore */ } },");
    s
}

fn host_api_script_panel() -> &'static str {
    r#"registerPanel(p) { try { var toPush = p; if (p && typeof p === 'object') { toPush = JSON.stringify(p); } else if (typeof p === 'string') { toPush = JSON.stringify({ id: p }); } else { toPush = JSON.stringify({ id: String(p) }); } globalThis.__registeredPanels = globalThis.__registeredPanels || []; globalThis.__registeredPanels.push(toPush); } catch(e) { /* ignore */ } },"#
}

fn host_api_script_create_entity() -> &'static str {
    r#"createEntity(obj) { globalThis.__createdEntities = globalThis.__createdEntities || []; try { if (obj && typeof obj === 'object' && typeof obj.firstName === 'string') { globalThis.__createdEntities.push({ firstName: obj.firstName }); globalThis.__logs = globalThis.__logs || []; globalThis.__logs.push(`entity created: ${obj.firstName}`); } else { globalThis.__createdEntities.push(obj); globalThis.__logs = globalThis.__logs || []; globalThis.__logs.push(`entity created: ${String(obj)}`); } } catch(e) { globalThis.__createdEntities.push(String(obj)); globalThis.__logs = globalThis.__logs || []; globalThis.__logs.push(`entity created: ${String(obj)}`); } },"#
}

fn host_api_script_set_entity() -> &'static str {
    r#"setEntity(id, data) { globalThis.__entityData = globalThis.__entityData || {}; if (typeof id === 'string' && data && typeof data === 'object') { globalThis.__entityData[id] = data; } },"#
}

fn host_api_script_log() -> &'static str {
    r#"log(msg) { try { globalThis.__logs = globalThis.__logs || []; globalThis.__logs.push(String(msg)); } catch(e) { } }, number: { of: function(n) { return n; } }, string: { of: function(s) { return s; } }, texture: { of: function(t) { return t; } }"#
}

fn host_api_script_rest() -> String {
    let mut parts: Vec<String> = Vec::new();
    parts.push(host_api_script_register_block("registerEvent"));
    parts.push(host_api_script_register_block("registerAction"));
    parts.push(host_api_script_register_block("registerEffect"));
    parts.push(host_api_script_panel().to_string());
    parts.push(host_api_script_create_entity().to_string());
    parts.push(host_api_script_set_entity().to_string());
    parts.push(host_api_script_log().to_string());
    let mut s = parts.join("");
    s.push_str(" }"); // close globalThis.host object
    s
}
fn host_api_script_tail() -> &'static str {
    "\n// Provide convenient aliases that scripts sometimes use\nglobalThis.createEntity = function(o) { return globalThis.host.createEntity(o); };\nglobalThis.entity = globalThis.entity || {};\nglobalThis.entity.create = function(o) { return globalThis.host.createEntity(o); };\nfunction string_of(s) { return s; }"
}

pub fn install_host_api(ctx: &Context) -> Result<()> {
    let script = [
        host_api_script_part1(),
        host_api_script_emit(),
        host_api_script_rest().as_str(),
        host_api_script_tail(),
    ].join("\n");
    ctx.with(|ctx| { ctx.eval::<(), _>(script) })?;
    Ok(())
}

/// Inspect the QuickJS global scope and return a JSON-deserializable
/// representation of discovered declarations (events, actions, functions, entities).
///
/// Implementation evaluates a small JS snippet that reads the sentinel
/// __registeredEvents and __createdEntities and top-level functions, returning
/// a JSON string which is deserialized into `Declarations`.
fn extract_declarations_script() -> &'static str {
    r#"(function(){ const out = { events: [], actions: [], functions: [], entities: [], creators: {}, emits: {}, panels: [], entity_data: {} }; const re = globalThis.__registeredEvents || []; out.events = re.map(ev => { if (typeof ev === 'string') return ev; if (ev && typeof ev === 'object') { if (typeof ev.name === 'string') return ev.name; if (ev.apply && typeof ev.apply === 'function' && ev.apply.name) return ev.apply.name; try { return JSON.stringify(ev); } catch(e) { return String(ev); } } return String(ev); }); const ra = globalThis.__registeredActions || []; out.actions = ra.map(ev => { if (typeof ev === 'string') return ev; if (ev && typeof ev === 'object') { if (typeof ev.name === 'string') return ev.name; if (ev.apply && typeof ev.apply === 'function' && ev.apply.name) return ev.apply.name; try { return JSON.stringify(ev); } catch(e) { return String(ev); } } return String(ev); }); const ce = globalThis.__createdEntities || []; out.entities = ce.map(en => { if (typeof en === 'string') return en; if (en && typeof en === 'object') { if (typeof en.firstName === 'string') return en.firstName; try { return JSON.stringify(en); } catch(e) { return String(en); } } return String(en); }); out.logs = globalThis.__logs || []; out.functions = Object.getOwnPropertyNames(globalThis).filter(k => { try { return typeof globalThis[k] === 'function' && !k.startsWith('_') && k !== 'host'; } catch(e) { return false; } }).sort(); out.creators = globalThis.__createdEntitiesFor || {}; out.emits = globalThis.__emitsMap || {}; out.panels = globalThis.__registeredPanels || []; out.entity_data = globalThis.__entityData || {}; return JSON.stringify(out); })()"#
}

pub fn extract_declarations(ctx: &Context) -> Result<Declarations> {
    let json = ctx.with(|ctx| ctx.eval::<String, _>(extract_declarations_script()))?;
    let dec: Declarations = serde_json::from_str(&json)?;
    Ok(dec)
}
