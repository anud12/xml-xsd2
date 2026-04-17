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
}

/// Install a minimal, explicit host API into the provided QuickJS Context.
///
/// This keeps host wiring in JS (small, reviewable) and avoids complex Rust
/// lifetime-managed JS function closures. The installed API is intentionally
/// tiny: it logs event registration/emission and exposes a simple `string_of`.
pub fn install_host_api(ctx: &Context) -> Result<()> {
    ctx.with(|ctx| {
        ctx.eval::<(), _>(
            r#"
            // Minimal host API expected by tests. All side-effects are explicit
            // console.log calls so test harness can observe them.
            globalThis.host = {
                emitEvent(name) {
                    if (name && typeof name === 'object' && typeof name.name === 'string') {
                        globalThis.__logs = globalThis.__logs || [];
                        globalThis.__logs.push(`event: ${name.name}`);
                    } else {
                        globalThis.__logs = globalThis.__logs || [];
                        globalThis.__logs.push(`event: ${String(name)}`);
                    }
                },
                registerEvent(ev) {
                    let n = 'unknown';
                    if (ev && typeof ev === 'object') {
                        if (typeof ev.name === 'string') n = ev.name;
                        else if (ev.apply && typeof ev.apply === 'function' && ev.apply.name) n = ev.apply.name;
                    } else if (typeof ev === 'string') {
                        n = ev;
                    }
                    globalThis.__logs = globalThis.__logs || [];
                    globalThis.__logs.push(`event registered: ${n}`);
                    globalThis.__registeredEvents = globalThis.__registeredEvents || [];
                    globalThis.__registeredEvents.push(ev);

                    // Try to inspect the prepare/apply function bodies to heuristically discover
                    // created entities (string.of usages inside handler source) and emitted
                    // events. This preserves the previous ability to detect created entities when
                    // they are only present inside handler functions.
                    try {
                        const scanFn = (fn, owner) => {
                            if (fn && typeof fn === 'function') {
                                let src = fn.toString();
                                const re = /string\.of\(\s*"([^\"]+)"\s*\)/g;
                                let m;
                                while ((m = re.exec(src)) !== null) {
                                    globalThis.__createdEntitiesFor = globalThis.__createdEntitiesFor || {};
                                    globalThis.__createdEntitiesFor[owner] = globalThis.__createdEntitiesFor[owner] || [];
                                    globalThis.__createdEntitiesFor[owner].push(m[1]);
                                }
                                const emitRe = /emitEvent\(\s*['\"]([^'\"]+)['\"]/g;
                                let em;
                                while ((em = emitRe.exec(src)) !== null) {
                                    globalThis.__emitsMap = globalThis.__emitsMap || {};
                                    globalThis.__emitsMap[owner] = globalThis.__emitsMap[owner] || [];
                                    globalThis.__emitsMap[owner].push(em[1]);
                                }
                            }
                        };
                        let owner = n;
                        scanFn(ev.prepare, owner);
                        scanFn(ev.apply, owner);
                    } catch(e) { /* ignore */ }
                },
                registerAction(ev) {
                    let n = 'unknown';
                    if (ev && typeof ev === 'object') {
                        if (typeof ev.name === 'string') n = ev.name;
                        else if (ev.apply && typeof ev.apply === 'function' && ev.apply.name) n = ev.apply.name;
                    } else if (typeof ev === 'string') {
                        n = ev;
                    }
                    globalThis.__logs = globalThis.__logs || [];
                    globalThis.__logs.push(`action registered: ${n}`);
                    globalThis.__registeredActions = globalThis.__registeredActions || [];
                    globalThis.__registeredActions.push(ev);
                    try {
                        const scanFn = (fn, owner) => {
                            if (fn && typeof fn === 'function') {
                                let src = fn.toString();
                                const re = /string\.of\(\s*"([^\"]+)"\s*\)/g;
                                let m;
                                while ((m = re.exec(src)) !== null) {
                                    globalThis.__createdEntitiesFor = globalThis.__createdEntitiesFor || {};
                                    globalThis.__createdEntitiesFor[owner] = globalThis.__createdEntitiesFor[owner] || [];
                                    globalThis.__createdEntitiesFor[owner].push(m[1]);
                                }
                                const emitRe = /emitEvent\(\s*['\"]([^'\"]+)['\"]/g;
                                let em;
                                while ((em = emitRe.exec(src)) !== null) {
                                    globalThis.__emitsMap = globalThis.__emitsMap || {};
                                    globalThis.__emitsMap[owner] = globalThis.__emitsMap[owner] || [];
                                    globalThis.__emitsMap[owner].push(em[1]);
                                }
                            }
                        };
                        let owner = n;
                        scanFn(ev.prepare, owner);
                        scanFn(ev.apply, owner);
                    } catch(e) { /* ignore */ }
                },
                registerEffect(ev) {
                    let n = 'unknown';
                    if (ev && typeof ev === 'object') {
                        if (typeof ev.name === 'string') n = ev.name;
                        else if (ev.apply && typeof ev.apply === 'function' && ev.apply.name) n = ev.apply.name;
                    } else if (typeof ev === 'string') {
                        n = ev;
                    }
                    globalThis.__logs = globalThis.__logs || [];
                    globalThis.__logs.push(`effect registered: ${n}`);
                    globalThis.__registeredEvents = globalThis.__registeredEvents || [];
                    globalThis.__registeredEvents.push(ev);
                    try {
                        const scanFn = (fn, owner) => {
                            if (fn && typeof fn === 'function') {
                                let src = fn.toString();
                                const re = /string\.of\(\s*"([^\"]+)"\s*\)/g;
                                let m;
                                while ((m = re.exec(src)) !== null) {
                                    globalThis.__createdEntitiesFor = globalThis.__createdEntitiesFor || {};
                                    globalThis.__createdEntitiesFor[owner] = globalThis.__createdEntitiesFor[owner] || [];
                                    globalThis.__createdEntitiesFor[owner].push(m[1]);
                                }
                                const emitRe = /emitEvent\(\s*['\"]([^'\"]+)['\"]/g;
                                let em;
                                while ((em = emitRe.exec(src)) !== null) {
                                    globalThis.__emitsMap = globalThis.__emitsMap || {};
                                    globalThis.__emitsMap[owner] = globalThis.__emitsMap[owner] || [];
                                    globalThis.__emitsMap[owner].push(em[1]);
                                }
                            }
                        };
                        let owner = n;
                        scanFn(ev.prepare, owner);
                        scanFn(ev.apply, owner);
                    } catch(e) { /* ignore */ }
                },
                createEntity(obj) {
                    globalThis.__createdEntities = globalThis.__createdEntities || [];
                    try {
                        if (obj && typeof obj === 'object' && typeof obj.firstName === 'string') {
                            globalThis.__createdEntities.push({ firstName: obj.firstName });
                            globalThis.__logs = globalThis.__logs || [];
                            globalThis.__logs.push(`entity created: ${obj.firstName}`);
                        } else {
                            globalThis.__createdEntities.push(obj);
                            globalThis.__logs = globalThis.__logs || [];
                            globalThis.__logs.push(`entity created: ${String(obj)}`);
                        }
                    } catch(e) { globalThis.__createdEntities.push(String(obj)); globalThis.__logs = globalThis.__logs || []; globalThis.__logs.push(`entity created: ${String(obj)}`); }
                },
                // Simple logging function that modules can call via host.log or when passed
                // into default exports as 'log' in the hostApi parameter.
                log(msg) {
                    try {
                        globalThis.__logs = globalThis.__logs || [];
                        globalThis.__logs.push(String(msg));
                    } catch(e) { }
                }
            };

            // Provide convenient aliases that scripts sometimes use
            globalThis.createEntity = function(o) { return globalThis.host.createEntity(o); };
            globalThis.entity = globalThis.entity || {};
            globalThis.entity.create = function(o) { return globalThis.host.createEntity(o); };

            // Simple string utility exposed as `string_of` to mirror existing tests
            function string_of(s) { return s; }
            "#,
        )
    })?;
    Ok(())
}

/// Inspect the QuickJS global scope and return a JSON-deserializable
/// representation of discovered declarations (events, actions, functions, entities).
///
/// Implementation evaluates a small JS snippet that reads the sentinel
/// __registeredEvents and __createdEntities and top-level functions, returning
/// a JSON string which is deserialized into `Declarations`.
pub fn extract_declarations(ctx: &Context) -> Result<Declarations> {
    let json = ctx.with(|ctx| {
        ctx.eval::<String, _>(
            r#"(function(){
                const out = { events: [], actions: [], functions: [], entities: [], creators: {}, emits: {} };
                const re = globalThis.__registeredEvents || [];
                out.events = re.map(ev => {
                    if (typeof ev === 'string') return ev;
                    if (ev && typeof ev === 'object') {
                        if (typeof ev.name === 'string') return ev.name;
                        if (ev.apply && typeof ev.apply === 'function' && ev.apply.name) return ev.apply.name;
                        try { return JSON.stringify(ev); } catch(e) { return String(ev); }
                    }
                    return String(ev);
                });
                const ra = globalThis.__registeredActions || [];
                out.actions = ra.map(ev => {
                    if (typeof ev === 'string') return ev;
                    if (ev && typeof ev === 'object') {
                        if (typeof ev.name === 'string') return ev.name;
                        if (ev.apply && typeof ev.apply === 'function' && ev.apply.name) return ev.apply.name;
                        try { return JSON.stringify(ev); } catch(e) { return String(ev); }
                    }
                    return String(ev);
                });
                const ce = globalThis.__createdEntities || [];
                out.entities = ce.map(en => {
                    if (typeof en === 'string') return en;
                    if (en && typeof en === 'object') {
                        if (typeof en.firstName === 'string') return en.firstName;
                        try { return JSON.stringify(en); } catch(e) { return String(en); }
                    }
                    return String(en);
                });
                out.logs = globalThis.__logs || [];
                out.functions = Object.getOwnPropertyNames(globalThis).filter(k => {
                    try { return typeof globalThis[k] === 'function' && !k.startsWith('_') && k !== 'host'; }
                    catch(e) { return false; }
                }).sort();
                out.creators = globalThis.__createdEntitiesFor || {};
                out.emits = globalThis.__emitsMap || {};
                return JSON.stringify(out);
            })()"#,
        )
    })?;

    let dec: Declarations = serde_json::from_str(&json)?;
    Ok(dec)
}
