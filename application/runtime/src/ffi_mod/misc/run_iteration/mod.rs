use std::collections::HashMap;

mod helpers;

pub use helpers::runtime_emit_event;
pub use helpers::runtime_get_elapsed_time_units;

fn build_files_map() -> HashMap<String, String> {
    let file_rows = crate::state::last_file_rows().lock().unwrap().clone();
    let mut map = HashMap::new();
    for r in file_rows.iter() {
        if r.len() >= 2 {
            map.insert(r[0].clone(), r[1].clone());
        }
    }
    map
}

#[no_mangle]
pub extern "C" fn runtime_run_iteration(elapsed_units: i64) -> i64 {
    if elapsed_units > 0 {
        crate::state::add_elapsed_time_units(elapsed_units);
    }

    let total = crate::state::get_elapsed_time_units();

    crate::js_executor::process_active_plans(total);

    let files = build_files_map();
    let _ = crate::js_executor::process_pending_effects(&files, total);
    let _ = crate::js_executor::process_scheduled_effects(&files, total);

    total
}
