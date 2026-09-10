use anyhow::Result;
use super::context_builders::{
    build_effect_context_pending, sync_entity_data,
    eval_reoccur_interval, collect_logs, lookup_effect,
    call_effect_prepare, call_effect_apply, sync_entity_data_back,
};
use super::sim_ctx;

pub fn process_pending_effects(current_elapsed: i64) -> Result<()> {
    let effects = crate::state::pending_effects().lock().unwrap().clone();
    if effects.is_empty() { return Ok(()); }
    crate::state::clear_pending_effects();

    let Some(ctx) = sim_ctx::ctx() else { return Ok(()); };

    sync_entity_data(ctx);

    for effect_name in effects.iter() {
        // Reset per-effect scratch state so logs/queues from the module
        // install or a previous effect don't leak into this run.
        let _ = ctx.with(|c| c.eval::<(), _>(
            "globalThis.__logs=[];globalThis.__pendingEffects=[];"));

        if !lookup_effect(ctx, effect_name) { continue; }

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
