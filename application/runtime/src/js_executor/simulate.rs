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
    actor: &str,
    initial_store: &[Vec<String>],
    args: &[(String, f64)],
) -> Result<(Vec<String>, Vec<Vec<String>>)> {
    let (_rt, ctx) = prepare_runtime_and_ctx()?;
    install_host_api(&ctx)?;
    let source = select_entry_source(files);
    let _transformed = eval_entry_in_ctx(&ctx, &source)?;
    let sj = serde_json::to_string(initial_store)?;
    let aj = serde_json::to_string(action_name)?;
    let arg_json = serde_json::to_string(&args.iter()
        .map(|(k, v)| (k.clone(), *v)).collect::<Vec<_>>())?;
    let template = super::sim_template::sim_template_js();
    let script = template
        .replace("ACTION_PLACEHOLDER", &aj)
        .replace("STORE_PLACEHOLDER", &sj)
        .replace("ARGS_PLACEHOLDER", &arg_json);
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
    match sim.active_plan {
        Some(plan) => {
            let wait = plan.get("wait").and_then(|w| w.as_i64()).unwrap_or(0);
            let mut steps = plan.get("steps")
                .and_then(|s| s.as_array())
                .cloned()
                .unwrap_or_default();
            let resume_at = crate::state::get_elapsed_time_units() + wait;
            let interruptible = plan.get("interruptible").and_then(|b| b.as_bool()).unwrap_or(false);
            // When the action was not bound to an actor explicitly, bind the
            // parked plan to the entity the first move/teleport step targets,
            // so per-actor busy/interruptible lookups match the entity.
            let bound_actor = if actor.is_empty() {
                steps.iter().find_map(|s| {
                    s.get("move").and_then(|m| m.get("entityId"))
                        .or_else(|| s.get("teleport").and_then(|t| t.get("entityId")))
                        .and_then(|e| e.as_str()).map(|s2| s2.to_string())
                })
            } else {
                Some(actor.to_string())
            };
            let bound_actor = bound_actor.unwrap_or_default();
            runtime_log!(
                "plan: action '{}' (actor: {:?}) parked for {} GTU (resume_at={}) interruptible={}",
                action_name, bound_actor, wait, resume_at, interruptible);
            crate::state::set_active_plan(
                action_name.to_string(), bound_actor, steps, resume_at, interruptible);
        }
        None => {
            if actor.is_empty() {
                crate::state::remove_active_plan(action_name);
            } else {
                // A non-parking action for an actor replaces any plan it had:
                // the prior plan is discarded, never queued.
                crate::state::remove_active_plans_for_actor(actor);
            }
        }
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
    #[serde(default)]
    #[serde(rename = "activePlan")]
    active_plan: Option<serde_json::Value>,
}
