use std::time::Instant;
use std::collections::HashMap;

mod helpers;

pub use helpers::runtime_emit_event;
pub use helpers::runtime_get_elapsed_time_units;

#[no_mangle]
pub extern "C" fn runtime_run_iteration(elapsed_units: i64) -> i64 {
    helpers::log(&format!("runtime_run_iteration ENTRY, elapsed_units={}",
        elapsed_units));
    let start = Instant::now();

    if elapsed_units > 0 {
        crate::state::add_elapsed_time_units(elapsed_units);
    }

    let total = crate::state::get_elapsed_time_units();
    helpers::log(&format!("runtime_run_iteration, total elapsed={}", total));

    let mut files_map: HashMap<String, String> = HashMap::new();
    for r in crate::state::last_file_rows().lock().unwrap().iter() {
        if r.len() >= 2 { files_map.insert(r[0].clone(), r[1].clone()); }
    }
    helpers::log(&format!(
        "runtime_run_iteration, files_map count={}", files_map.len()));

    {
        let pe = crate::state::pending_effects().lock().unwrap().clone();
        helpers::log(&format!("runtime_run_iteration, \
            pending_effects before: {:?}", pe));
    }
    {
        let nd = crate::state::last_entity_number_data()
            .lock().unwrap().clone();
        helpers::log(&format!("runtime_run_iteration, \
            entity_number_data before: {:?}", nd));
    }

    helpers::log("runtime_run_iteration, CALLING process_pending_effects");
    if let Err(e) = crate::js_executor::process_pending_effects(
        &files_map, total) {
        helpers::log(&format!("Failed to process pending effects: {:?}", e));
    }
    helpers::log("runtime_run_iteration, RETURNED from process_pending_effects");

    {
        let nd = crate::state::last_entity_number_data()
            .lock().unwrap().clone();
        helpers::log(&format!(
            "runtime_run_iteration, entity_number_data after \
            pending_effects: {:?}", nd));
    }

    helpers::log("runtime_run_iteration, CALLING process_scheduled_effects");
    if let Err(e) = crate::js_executor::process_scheduled_effects(
        &files_map, total) {
        helpers::log(&format!(
            "Failed to process scheduled effects: {:?}", e));
    }
    helpers::log(
        "runtime_run_iteration, RETURNED from process_scheduled_effects");

    helpers::log("runtime_run_iteration, CALLING process_autonomy_scripts");
    if let Err(e) = crate::js_executor::process_autonomy_scripts(
        &files_map, total) {
        helpers::log(&format!(
            "Failed to process autonomy scripts: {:?}", e));
    }
    helpers::log("runtime_run_iteration, RETURNED from process_autonomy_scripts");

    {
        let nd = crate::state::last_entity_number_data()
            .lock().unwrap().clone();
        helpers::log(&format!(
            "runtime_run_iteration, entity_number_data after \
             ALL processing: {:?}", nd));
    }

    let _elapsed = start.elapsed();
    total
}
