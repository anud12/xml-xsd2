use anyhow::Result;
use crate::js_host_api::install_host_api;
use super::simulate::{
    prepare_runtime_and_ctx, select_entry_source, eval_entry_in_ctx,
};
use super::context_builders::{
    build_effect_context_scheduled, sync_entity_data_with_initial,
    eval_reoccur_interval, collect_logs, lookup_effect,
    call_effect_prepare, call_effect_apply, sync_entity_data_back,
};

const EFFECT_HOST_API_JS: &str = r#"(function(){
    globalThis.hostApi={entity:{filter:{create:function(){
        return{byId:function(fn){return{fn:fn;}};}}}},
    string:{of:function(s){return s;}},number:{of:function(n){return n;}},
    maybe:{of:function(v){return{value:v};},none:function(){
        return{value:undefined};}},
    condition:{of:function(v){return{value:v,
        ifTrue:function(cb){if(v&&typeof cb==='function')cb();},
        ifFalse:function(cb){if(!v&&typeof cb==='function')cb();}};
    }}};
})()"#;

pub fn process_scheduled_effects(
    files: &std::collections::HashMap<String, String>,
    current_elapsed: i64,
) -> Result<()> {
    let due = crate::state::get_due_scheduled_effects(current_elapsed);
    if due.is_empty() { return Ok(()); }

    for scheduled in due.iter() {
        let (_rt, ctx) = prepare_runtime_and_ctx()?;
        install_host_api(&ctx)?;
        let source = select_entry_source(files);
        let _transformed = eval_entry_in_ctx(&ctx, &source)?;

        sync_entity_data_with_initial(&ctx);
        let _ = ctx.with(|c| c.eval::<(), _>(EFFECT_HOST_API_JS));

        if !lookup_effect(&ctx, &scheduled.name) { continue; }

        build_effect_context_scheduled(&ctx);

        // Pre-gate: check reoccurAfterMs before apply
        let ri_pre = eval_reoccur_interval(&ctx);
        if ri_pre <= 0.0 {
            crate::state::remove_scheduled_effect(&scheduled.name);
            continue;
        }

        call_effect_prepare(&ctx);
        call_effect_apply(&ctx);

        // Post-gate: re-evaluate reoccurAfterMs after apply
        let ri_post = eval_reoccur_interval(&ctx);
        if ri_post > 0.0 {
            let iv = ri_post as i64;
            let next = ((current_elapsed / iv) + 1) * iv;
            crate::state::add_scheduled_effect(
                scheduled.name.clone(),
                serde_json::Value::Object(serde_json::Map::new()),
                next, iv,
            );
        }
        sync_entity_data_back(&ctx);
        collect_logs(&ctx);
    }
    Ok(())
}
