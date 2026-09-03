use anyhow::Result;
use rquickjs::{Context, Runtime};
use crate::js_runtime::{create_runtime, create_context};
use crate::js_host_api::install_host_api;

// Re-exports for backward compatibility
pub use super::sim_entry::{select_entry_source, eval_entry_in_ctx};
pub use super::sim_store::convert_store_values;

pub fn prepare_runtime_and_ctx() -> Result<(Runtime, Context)> {
    let rt = create_runtime()?;
    let ctx = create_context(&rt)?;
    Ok((rt, ctx))
}

fn run_sim_collect(ctx: &Context, script: &str) -> Result<(String, String)> {
    let rj = ctx.with(|ctx| ctx.eval::<String, _>(script))?;
    let lj = ctx.with(|ctx| ctx.eval::<String, _>(
        "JSON.stringify(globalThis.__logs || [])"))
        .unwrap_or_else(|_| "[]".into());
    Ok((rj, lj))
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
    let sj = super::sim_store::build_initial_store_json(initial_store)?;
    let aj = serde_json::to_string(action_name)?;
    let template = super::sim_template::sim_template_js();
    let script = template
        .replace("ACTION_PLACEHOLDER", &aj)
        .replace("STORE_PLACEHOLDER", &sj);
    let (rj, lj) = run_sim_collect(&ctx, &script)?;
    if let Ok(lv) = serde_json::from_str::<Vec<String>>(&lj) {
        for l in lv.iter() { runtime_log!("{}", l); }
    }
    let sim: Sr = serde_json::from_str(&rj)?;
    if !sim.pending_effects.is_empty() {
        crate::state::set_pending_effects(sim.pending_effects.iter()
            .map(|e| e.name.clone()).collect());
    }
    if !sim.containers.is_empty() {
        crate::state::set_last_containers(sim.containers.clone());
    }
    Ok((sim.created, convert_store_values(&sim.store)))
}

#[derive(serde::Deserialize)]
struct Pe { name: String, #[allow(dead_code)] payload: serde_json::Value }

#[derive(serde::Deserialize)]
struct Sr {
    created: Vec<String>,
    store: Vec<serde_json::Value>,
    #[serde(default)]
    #[serde(rename = "pendingEffects")]
    pending_effects: Vec<Pe>,
    #[serde(default)]
    containers: Vec<String>,
}
