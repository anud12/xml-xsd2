use anyhow::Result;
use super::context_builders::{
    build_effect_context_pending, sync_entity_data,
    eval_reoccur_interval, collect_logs, lookup_effect,
    call_effect_prepare, call_effect_apply, sync_entity_data_back,
};

pub fn process_pending_effects(
    _files: &std::collections::HashMap<String, String>,
    current_elapsed: i64,
) -> Result<()> {
    let effects = crate::state::pending_effects().lock().unwrap().clone();
    if effects.is_empty() { return Ok(()); }
    crate::state::clear_pending_effects();

    // Prefer the persistent sim context: the module is already installed
    // there with its registered effects and a live hostApi, so firing an
    // effect does not require re-evaluating the module entry (which would
    // re-run UI node declarations and can throw in a bare context).
    if let Some(ctx) = super::sim_ctx::ctx() {
        return run_pending_effects_in_sim(ctx, &effects, current_elapsed);
    }

    // Fallback: no sim context installed — build a fresh one and re-evaluate
    // the module entry (the legacy extraction path).
    let (_rt, ctx) = crate::js_executor::simulate::prepare_runtime_and_ctx()?;
    crate::js_host_api::install_host_api(&ctx)?;
    let source = crate::js_executor::simulate::select_entry_source(_files);
    let _ = crate::js_executor::simulate::eval_entry_in_ctx(&ctx, &source);
    let _ = ctx.with(|c| c.eval::<(), _>(
        "if(globalThis.hostApi&&!globalThis.hostApi.entity){\
         globalThis.hostApi.entity=globalThis.host.entity;}"
    ));
    run_pending_effects_in_sim(&ctx, &effects, current_elapsed)
}

/// Fire a batch of pending effects in the given context. Looks each effect up
/// in `__registeredEvents`, builds the effect ctx, runs prepare/apply, and
/// surfaces the effect's log lines. Returns after the batch (the caller owns
/// context lifecycle).
fn run_pending_effects_in_sim(
    ctx: &rquickjs::Context,
    effects: &[String],
    current_elapsed: i64,
) -> Result<()> {
    // Rebuild the effect context against the context's live entity data.
    sync_entity_data(ctx);

    for effect_name in effects.iter() {
        let found = lookup_effect(ctx, effect_name);
        if !found { continue; }

        build_effect_context_pending(ctx);
        call_effect_prepare(ctx);
        call_effect_apply(ctx);

        let ri = eval_reoccur_interval(ctx);
        if ri > 0.0 {
            let iv = ri as i64;
            let next = ((current_elapsed / iv) + 1) * iv;
            crate::state::add_scheduled_effect(
                effect_name.clone(),
                serde_json::Value::Object(serde_json::Map::new()),
                next, iv,
            );
        }

        sync_entity_data_back(ctx);
        collect_logs(ctx);
    }
    Ok(())
}
