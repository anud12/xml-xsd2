use std::collections::HashMap;
use std::sync::atomic::Ordering;

#[allow(dead_code)]
pub fn clear_state() {
    *super::last_file_rows().lock().unwrap() = Vec::new();
    *super::last_entity_rows().lock().unwrap() = Vec::new();
    *super::last_action_rows().lock().unwrap() = Vec::new();
    *super::last_event_rows().lock().unwrap() = Vec::new();
    *super::last_module_rows().lock().unwrap() = Vec::new();
    *super::last_entity_patterns().lock().unwrap() = Vec::new();
    *super::last_panels().lock().unwrap() = Vec::new();
    super::clear_pending_effects();
    *super::scheduled_effects().lock().unwrap() = Vec::new();
    *super::active_plans().lock().unwrap() = Vec::new();
    *super::last_created_by().lock().unwrap() = HashMap::new();
    *super::last_archive_path().lock().unwrap() = String::new();
    *super::last_entity_data().lock().unwrap() = HashMap::new();
    *super::last_entity_number_data().lock().unwrap() = HashMap::new();
    *super::initial_entity_data().lock().unwrap() = HashMap::new();
    *super::last_containers().lock().unwrap() = Vec::new();
    super::elapsed_time_units().store(0, Ordering::SeqCst);
    super::persisted_flag().store(false, Ordering::SeqCst);
}
