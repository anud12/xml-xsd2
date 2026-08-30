use anyhow::{anyhow, Result};
use crate::js_runtime::{create_runtime, create_context};
use crate::js_host_api::{extract_declarations, Declarations};
use crate::js_host_api::script_emit;
use crate::js_host_api::script_rest;
use super::strip_export::strip_export_prefix;
use super::import_resolver::resolve_and_fetch;

fn create_rt_ctx() -> Result<(rquickjs::Runtime, rquickjs::Context)> {
    let rt = create_runtime()?;
    let ctx = create_context(&rt)?;
    Ok((rt, ctx))
}

fn get_host_api_script() -> String {
    [
        script_emit::host_api_script_part1(),
        script_emit::host_api_script_emit(),
        script_rest::host_api_script_rest().as_str(),
        script_rest::host_api_script_tail().as_str(),
    ].join("\n")
}

fn bundle_imports(source: &str) -> String {
    let mut result = String::new();
    let mut pending = vec![("index.js".to_string(), source.to_string())];
    let mut visited = std::collections::HashSet::new();
    visited.insert("index.js".to_string());
    while let Some((path, src)) = pending.pop() {
        let dir = path.rfind('/').map(|s| path[..s].to_string())
            .unwrap_or_default();
        for line in src.lines() {
            let trimmed = line.trim();
            if trimmed.starts_with("import ") {
                if let Some((target, content)) = resolve_and_fetch(&dir, trimmed) {
                    if visited.insert(target.clone()) {
                        pending.push((target, content));
                    }
                }
                continue;
            }
            result.push_str(&strip_export_prefix(line));
            result.push('\n');
        }
    }
    result
}

fn transform_source(source: &str) -> String {
    if source.contains("export default") {
        source.replace("export default", "var __module_default =")
    } else { source.to_string() }
}

pub fn extract_from_source(source: &str) -> Result<Declarations> {
    let (_rt, ctx) = create_rt_ctx()?;
    let bundled = bundle_imports(source);
    let transformed = transform_source(&bundled);
    let host_script = get_host_api_script();
    ctx.with(|c| {
        rquickjs::CaughtError::catch(&c, c.eval::<(), _>(host_script.clone()))
            .map_err(|ce| ce.to_string())
    }).map_err(|msg| anyhow!("host API eval failed: {}", msg))?;
    ctx.with(|c| {
        rquickjs::CaughtError::catch(&c, c.eval::<(), _>(transformed.clone()))
            .map_err(|ce| ce.to_string())
    }).map_err(|msg| anyhow!("module eval failed: {}", msg))?;
    let invoke_js = super::extract_invoke::get_invoke_js();
    ctx.with(|c| {
        rquickjs::CaughtError::catch(&c, c.eval::<(), _>(invoke_js.to_string()))
            .map_err(|ce| ce.to_string())
    }).map_err(|msg| anyhow!("invoke eval failed: {}", msg))?;
    extract_declarations(&ctx)
}
