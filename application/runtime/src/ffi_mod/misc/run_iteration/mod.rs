mod helpers;

pub use helpers::runtime_emit_event;
pub use helpers::runtime_get_elapsed_time_units;

#[no_mangle]
pub extern "C" fn runtime_run_iteration(elapsed_units: i64) -> i64 {
    if elapsed_units > 0 {
        crate::state::add_elapsed_time_units(elapsed_units);
    }

    let total = crate::state::get_elapsed_time_units();

    crate::js_executor::process_active_plans(total);

    let _ = crate::js_executor::process_pending_effects(total);
    let _ = crate::js_executor::process_scheduled_effects(total);

    let _ = crate::js_executor::process_behavior_machine(total);

    total
}
