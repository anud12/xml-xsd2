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
                    registerEvent: host.registerEvent
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
