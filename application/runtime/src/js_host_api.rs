use anyhow::Result;
use rquickjs::Context;
use serde::Deserialize;

/// Minimal shape describing declarations discovered in a script/context.
#[derive(Debug, Deserialize, serde::Serialize)]
pub struct Declarations {
    pub events: Vec<String>,
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

                    // Try to inspect the apply function body to heuristically discover
                    // created entities (string.of usages inside handler source). This
                    // preserves the previous ability to detect created entities when
                    // they are only present inside handler functions.
                    try {
                        if (ev && typeof ev === 'object' && ev.apply && typeof ev.apply === 'function') {
                            let src = ev.apply.toString();
                            const re = /string\.of\(\s*"([^\"]+)"\s*\)/g;
                            let m;
                            while ((m = re.exec(src)) !== null) {
                                globalThis.__createdEntities = globalThis.__createdEntities || [];
                                globalThis.__createdEntities.push(m[1]);
                            }
                        }
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
/// representation of discovered declarations (events, functions, entities).
///
/// Implementation evaluates a small JS snippet that reads the sentinel
/// __registeredEvents and __createdEntities and top-level functions, returning
/// a JSON string which is deserialized into `Declarations`.
pub fn extract_declarations(ctx: &Context) -> Result<Declarations> {
    let json = ctx.with(|ctx| {
        ctx.eval::<String, _>(
            r#"(function(){
                const out = { events: [], functions: [], entities: [] };
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
                return JSON.stringify(out);
            })()"#,
        )
    })?;

    let dec: Declarations = serde_json::from_str(&json)?;
    Ok(dec)
}
