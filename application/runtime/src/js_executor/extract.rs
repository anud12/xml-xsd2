use anyhow::{anyhow, Result};
use crate::js_runtime::{create_runtime, create_context};
use rquickjs::{Context, Runtime};
use crate::js_host_api::{extract_declarations, Declarations};
use crate::js_host_api::script_emit;
use crate::js_host_api::script_rest;

fn create_rt_ctx() -> Result<(Runtime, Context)> {
    let rt = create_runtime()?;
    let ctx = create_context(&rt)?;
    Ok((rt, ctx))
}

fn transform_source(source: &str) -> String {
    if source.contains("export default") {
        source.replace("export default", "var __module_default =")
    } else { source.to_string() }
}

fn get_host_api_script() -> String {
    [
        script_emit::host_api_script_part1(),
        script_emit::host_api_script_emit(),
        script_rest::host_api_script_rest().as_str(),
        script_rest::host_api_script_tail(),
    ].join("\n")
}

pub fn extract_from_source(source: &str) -> Result<Declarations> {
    let (_rt, ctx) = create_rt_ctx()?;
    let transformed = transform_source(source);

    // Step 1: Install host API
    let host_script = get_host_api_script();
    ctx.with(|ctx| {
        ctx.eval::<(), _>(host_script.clone())
    }).map_err(|e| anyhow!("host API eval failed: {}", e))?;

    // Step 2: Eval module source
    ctx.with(|ctx| {
        ctx.eval::<(), _>(transformed.clone())
    }).map_err(|e| anyhow!("module source eval failed: {}", e))?;

    // Step 3: Build hostApi and call __module_default
    let invoke_js = r#"
var h=globalThis.host;
if(!h){throw new Error("globalThis.host is undefined");}
var hostApi={
  string:{of:function(s){return s;}},
  number:{of:function(n){return n;}},
  texture:{of:function(p){return p;}},
  emitEvent:h.emitEvent,
  registerEvent:h.registerEvent,
  registerAction:h.registerAction,
  registerEffect:h.registerEffect,
  registerPanel:h.registerPanel,
  setEntity:h.setEntity,
  log:h.log,
  maybe:{of:function(v){return{value:v};},none:function(){return{value:undefined};}},
  condition:{of:function(v){return{value:v,ifTrue:function(cb){if(v&&typeof cb==='function')cb();},ifFalse:function(cb){if(!v&&typeof cb==='function')cb();}};}}
};
globalThis.hostApi=hostApi;
var __mod=globalThis.__module_default||__module_default;
if(typeof __mod==='function'){__mod(hostApi);}
"#;
    ctx.with(|ctx| {
        ctx.eval::<(), _>(invoke_js.to_string())
    }).map_err(|e| anyhow!("invoke eval failed: {}", e))?;

    extract_declarations(&ctx)
}
