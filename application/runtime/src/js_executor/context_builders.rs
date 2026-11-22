use rquickjs::Context;

pub use super::js_strings_effect::{
    call_effect_prepare, call_effect_apply, eval_reoccur_interval,
};
pub use super::effect_helpers::{lookup_effect, collect_logs};

pub fn build_effect_context_pending(ctx: &Context) {
    let _ = build_effect_context_impl(ctx, "pending");
}

pub fn build_effect_context_scheduled(ctx: &Context) {
    let _ = build_effect_context_impl(ctx, "scheduled");
}

fn build_effect_context_impl(ctx: &Context, kind: &str) -> anyhow::Result<()> {
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
    let _ = crate::js_executor::sim_ctx::sim_with(ctx, |c| c.eval::<(), _>(full.as_str()));
    Ok(())
}

