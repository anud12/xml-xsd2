use anyhow::Result;
use super::context_builders::{
    build_effect_context_scheduled, sync_entity_data_with_initial,
    eval_reoccur_interval, collect_logs, lookup_effect,
    call_effect_prepare, call_effect_apply, sync_entity_data_back,
};
use super::sim_ctx;

pub fn process_scheduled_effects(current_elapsed: i64) -> Result<()> {
    let due = crate::state::get_due_scheduled_effects(current_elapsed);
    if due.is_empty() { return Ok(()); }

    let Some(ctx) = sim_ctx::ctx() else { return Ok(()); };

    for scheduled in due.iter() {
        // Reset per-effect scratch state so logs/queues from the module
        // install or a previous effect don't leak into this run.
        let _ = ctx.with(|c| c.eval::<(), _>(
            "globalThis.__logs=[];globalThis.__pendingEffects=[];"));

        sync_entity_data_with_initial(ctx);

        if !lookup_effect(ctx, &scheduled.name) { continue; }

        build_effect_context_scheduled(ctx);

        // Pre-gate: check reoccurAfterMs before apply
        let ri_pre = eval_reoccur_interval(ctx);
        if ri_pre <= 0.0 {
            crate::state::remove_scheduled_effect(&scheduled.name);
            continue;
        }

        call_effect_prepare(ctx);
        call_effect_apply(ctx);

        // Post-gate: re-evaluate reoccurAfterMs after apply
        let ri_post = eval_reoccur_interval(ctx);
        if ri_post > 0.0 {
            let iv = ri_post as i64;
            let next = ((current_elapsed / iv) + 1) * iv;
            crate::state::add_scheduled_effect(
                scheduled.name.clone(),
                serde_json::Value::Object(serde_json::Map::new()),
                next, iv,
            );
        }
        sync_entity_data_back(ctx);
        collect_logs(ctx);
    }
    Ok(())
}
