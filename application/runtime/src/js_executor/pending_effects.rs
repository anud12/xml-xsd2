use anyhow::Result;
use crate::js_host_api::install_host_api;
use super::simulate::{
    prepare_runtime_and_ctx, select_entry_source, eval_entry_in_ctx,
};
use super::context_builders::{
    build_effect_context_pending, sync_entity_data,
    eval_reoccur_interval, collect_logs, lookup_effect,
    call_effect_prepare, call_effect_apply, sync_entity_data_back,
};

pub fn process_pending_effects(
    files: &std::collections::HashMap<String, String>,
    current_elapsed: i64,
) -> Result<()> {
    let effects = crate::state::pending_effects().lock().unwrap().clone();
    if effects.is_empty() { return Ok(()); }
    crate::state::clear_pending_effects();

    let (_rt, ctx) = prepare_runtime_and_ctx()?;
    install_host_api(&ctx)?;
    let source = select_entry_source(files);
    let _transformed = eval_entry_in_ctx(&ctx, &source)?;

    // Ensure hostApi has entity.filter for effect closures
    let _ = ctx.with(|c| c.eval::<(), _>(
        "if(globalThis.hostApi&&!globalThis.hostApi.entity){\
         globalThis.hostApi.entity=globalThis.host.entity;}"
    ));

    sync_entity_data(&ctx);

    for effect_name in effects.iter() {
        if !lookup_effect(&ctx, effect_name) { continue; }

        build_effect_context_pending(&ctx);
        call_effect_prepare(&ctx);
        call_effect_apply(&ctx);

        let ri = eval_reoccur_interval(&ctx);
        if ri > 0.0 {
            let iv = ri as i64;
            let next = ((current_elapsed / iv) + 1) * iv;
            crate::state::add_scheduled_effect(
                effect_name.clone(),
                serde_json::Value::Object(serde_json::Map::new()),
                next, iv,
            );
        }

        sync_entity_data_back(&ctx);
        collect_logs(&ctx);
    }
    Ok(())
}
