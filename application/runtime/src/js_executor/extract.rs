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
        script_rest::host_api_script_tail().as_str(),
    ].join("\n")
}

pub fn extract_from_source(source: &str) -> Result<Declarations> {
    let (_rt, ctx) = create_rt_ctx()?;
    let transformed = transform_source(source);

    let host_script = get_host_api_script();
    ctx.with(|ctx| {
        ctx.eval::<(), _>(host_script.clone())
    }).map_err(|e| anyhow!("host API eval failed: {}", e))?;

    ctx.with(|ctx| {
        ctx.eval::<(), _>(transformed.clone())
    }).map_err(|e| anyhow!("module eval failed: {}", e))?;

    let invoke_js = super::extract_invoke::get_invoke_js();
    ctx.with(|ctx| {
        ctx.eval::<(), _>(invoke_js.to_string())
    }).map_err(|e| anyhow!("invoke eval failed: {}", e))?;

    extract_declarations(&ctx)
}
