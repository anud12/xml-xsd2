use crate::ffi_mod::types::*;

mod state_collection;

#[export_name = "runtime_export_state_struct"]
pub extern "C" fn runtime_export_state_struct() -> *mut ExportedState {
    let files_cached = crate::state::last_file_rows()
        .lock().unwrap().clone();
    let entities_cached = crate::state::last_entity_rows()
        .lock().unwrap().clone();
    let actions_cached = crate::state::last_action_rows()
        .lock().unwrap().clone();
    let events_cached = crate::state::last_event_rows()
        .lock().unwrap().clone();
    let modules_cached = crate::state::last_module_rows()
        .lock().unwrap().clone();
    let patterns_cached = crate::state::last_entity_patterns()
        .lock().unwrap().clone();
    let panels_cached = Vec::new(); // Panels handled by C#
    let created_by_cached = crate::state::last_created_by()
        .lock().unwrap().clone();

    state_collection::build_exported_state(
        files_cached, entities_cached, actions_cached, events_cached,
        modules_cached, patterns_cached, panels_cached, created_by_cached,
    )
}
