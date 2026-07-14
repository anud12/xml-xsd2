use crate::ffi_mod::types::*;

mod state_collection;

fn collect_panels_fallback() -> Vec<String> {
    let mut panels = Vec::new();
    for row in crate::state::last_file_rows().lock().unwrap().iter() {
        if row.len() < 2 { continue; }
        let fname = row[0].to_lowercase();
        if !(fname.contains("panel") && fname.contains(".csv")) { continue; }
        for line in row[1].lines() {
            let t = line.trim();
            if t.is_empty() { continue; }
            let f = t.split(',').next().unwrap().trim_matches('"');
            if f.eq_ignore_ascii_case("id") || f.is_empty() { continue; }
            panels.push(f.to_string());
        }
    }
    panels
}

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
    let panels_cached = collect_panels_fallback();
    let created_by_cached = crate::state::last_created_by()
        .lock().unwrap().clone();

    state_collection::build_exported_state(
        files_cached, entities_cached, actions_cached, events_cached,
        modules_cached, patterns_cached, panels_cached, created_by_cached,
    )
}
