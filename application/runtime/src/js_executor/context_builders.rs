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

    // Precompute the inside-area map so entity wrappers can read it without a
    // per-call FFI round trip. Areas and container membership are static at
    // this point (synced before context build).
    let containers = crate::state::last_containers().lock().unwrap().clone();
    let inside_json = crate::state::inside_area_map_json(&containers);
    let _ = crate::js_executor::sim_ctx::sim_with(ctx, |c| c.eval::<(), _>(
        format!("globalThis.__insideArea = {}; ", inside_json).as_str()
    ));

    // Evaluate full JS directly
    let full = format!("{}{}{}", p1, p2, p3);
    let _ = crate::js_executor::sim_ctx::sim_with(ctx, |c| c.eval::<(), _>(full.as_str()));
    Ok(())
}

pub fn sync_entity_data(ctx: &Context) {
    super::entity_sync::sync_entity_store(ctx);
}


