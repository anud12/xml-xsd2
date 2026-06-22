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
    build_effect_context_impl(ctx, "pending");
}

pub fn build_effect_context_scheduled(ctx: &Context) {
    build_effect_context_impl(ctx, "scheduled");
}

fn build_effect_context_impl(ctx: &Context, kind: &str) {
    let (p1, p2, p3) = if kind == "pending" {
        (super::pending_ctx_p1::get_part1(),
         super::pending_ctx_p2::get_part2(),
         super::pending_ctx_p3::get_part3())
    } else {
        (super::scheduled_ctx_p1::get_part1(),
         super::scheduled_ctx_p2::get_part2(),
         super::scheduled_ctx_p3::get_part3())
    };

    // Evaluate full JS directly
    let full = format!("{}{}{}", p1, p2, p3);
    let _ = ctx.with(|c| c.eval::<(), _>(full.as_str()));
}

pub fn sync_entity_data(ctx: &Context) {
    super::entity_sync::sync_entity_store(ctx);
}
