use rquickjs::Context;

pub use super::js_strings_effect::{
    call_effect_prepare, call_effect_apply, eval_reoccur_interval,
};
pub use super::entity_sync::{
    lookup_effect, sync_entity_data_with_initial,
};
pub use super::entity_sync_back::{
    sync_entity_data_back, collect_logs,
};

pub fn build_effect_context_pending(ctx: &Context) {
    let js = super::js_strings_pending::get_pending_ctx_js();
    let _ = ctx.with(|c| c.eval::<(), _>(js));
}

pub fn build_effect_context_scheduled(ctx: &Context) {
    let js = super::js_strings_scheduled::get_scheduled_ctx_js();
    let _ = ctx.with(|c| c.eval::<(), _>(js));
}

pub fn sync_entity_data(ctx: &Context) {
    super::entity_sync::sync_entity_store(ctx);
}
