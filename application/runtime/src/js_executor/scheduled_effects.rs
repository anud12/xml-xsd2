use anyhow::Result;
use super::context_builders::{
    build_effect_context_scheduled, sync_entity_data,
    eval_reoccur_interval, collect_logs, lookup_effect,
    call_effect_prepare, call_effect_apply, sync_entity_data_back,
};

/// Fire due reoccurring effects. Prefer the persistent sim context (the module
/// is already installed there with its registered effects and a live entity
/// store), so the effect applies to the *current* accumulated entity values
/// rather than a fresh re-evaluation of the module entry (which would reset
/// the store to its initial values and discard prior increments).
pub fn process_scheduled_effects(
    _files: &std::collections::HashMap<String, String>,
    current_elapsed: i64,
) -> Result<()> {
    let due = crate::state::get_due_scheduled_effects(current_elapsed);
    if due.is_empty() { return Ok(()); }

    // The reoccur effect is applied against the live sim context. If a module
    // has not been installed (no sim ctx), there is nothing to re-fire.
    let Some(ctx) = super::sim_ctx::ctx() else { return Ok(()); };

    // Rebuild the effect context against the context's live entity data.
    sync_entity_data(ctx);

    for scheduled in due.iter() {
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
