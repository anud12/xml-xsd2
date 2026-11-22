use std::collections::HashMap;
use std::sync::atomic::{AtomicI64, AtomicU64, Ordering};
use std::sync::Mutex;

pub fn last_file_rows() -> &'static Mutex<Vec<Vec<String>>> {
    super::persisted_flag(); unsafe { super::LAST_FILE_ROWS.expect("file rows initialized") }
}
pub fn last_entity_rows() -> &'static Mutex<Vec<Vec<String>>> {
    super::persisted_flag(); unsafe { super::LAST_ENTITY_ROWS.expect("entity rows initialized") }
}
pub fn last_action_rows() -> &'static Mutex<Vec<Vec<String>>> {
    super::persisted_flag(); unsafe { super::LAST_ACTION_ROWS.expect("action rows initialized") }
}
pub fn last_event_rows() -> &'static Mutex<Vec<Vec<String>>> {
    super::persisted_flag(); unsafe { super::LAST_EVENT_ROWS.expect("event rows initialized") }
}
pub fn last_module_rows() -> &'static Mutex<Vec<Vec<String>>> {
    super::persisted_flag(); unsafe { super::LAST_MODULE_ROWS.expect("module rows initialized") }
}
pub fn last_entity_patterns() -> &'static Mutex<Vec<String>> {
    super::persisted_flag(); unsafe { super::LAST_ENTITY_PATTERNS.expect("entity patterns initialized") }
}
pub fn last_panels() -> &'static Mutex<Vec<String>> {
    super::persisted_flag(); unsafe { super::LAST_PANELS.expect("panels initialized") }
}
pub fn last_created_by() -> &'static Mutex<HashMap<String, Vec<String>>> {
    super::persisted_flag(); unsafe { super::LAST_CREATED_BY.expect("created by map initialized") }
}
pub fn pending_effects() -> &'static Mutex<Vec<String>> {
    super::persisted_flag(); unsafe { super::PENDING_EFFECTS.expect("pending effects initialized") }
}
pub fn scheduled_effects() -> &'static Mutex<Vec<super::ScheduledEffect>> {
    super::persisted_flag(); unsafe { super::SCHEDULED_EFFECTS.expect("scheduled effects initialized") }
}
pub fn elapsed_time_units() -> &'static AtomicI64 {
    super::persisted_flag(); unsafe { super::ELAPSED_TIME_UNITS.expect("elapsed time units initialized") }
}
pub fn set_last_file_rows(rows: Vec<Vec<String>>) { *last_file_rows().lock().unwrap() = rows; }
pub fn set_last_entity_rows(rows: Vec<Vec<String>>) { *last_entity_rows().lock().unwrap() = rows; }
pub fn append_entity_row(row: Vec<String>) { last_entity_rows().lock().unwrap().push(row); }
pub fn set_last_action_rows(rows: Vec<Vec<String>>) { *last_action_rows().lock().unwrap() = rows; }
pub fn set_last_event_rows(rows: Vec<Vec<String>>) { *last_event_rows().lock().unwrap() = rows; }
pub fn set_last_module_rows(rows: Vec<Vec<String>>) { *last_module_rows().lock().unwrap() = rows; }
pub fn set_last_entity_patterns(rows: Vec<String>) { *last_entity_patterns().lock().unwrap() = rows; }
pub fn set_last_panels(rows: Vec<String>) { *last_panels().lock().unwrap() = rows; }
pub fn set_last_created_by(map: HashMap<String, Vec<String>>) {
    *last_created_by().lock().unwrap() = map;
}
pub fn set_pending_effects(effects: Vec<String>) { *pending_effects().lock().unwrap() = effects; }
pub fn clear_pending_effects() { pending_effects().lock().unwrap().clear(); }
pub fn add_elapsed_time_units(units: i64) {
    elapsed_time_units().fetch_add(units, Ordering::SeqCst);
}
pub fn get_elapsed_time_units() -> i64 {
    elapsed_time_units().load(Ordering::SeqCst)
}
pub fn archive_files() -> &'static Mutex<HashMap<String, String>> {
    super::persisted_flag(); unsafe { super::ARCHIVE_FILES.expect("archive files initialized") }
}
pub fn set_archive_files(map: HashMap<String, String>) {
    *archive_files().lock().unwrap() = map;
}
pub fn lxm_seed_store() -> &'static AtomicU64 {
    &super::LXM_SEED
}
/// Records the 32-bit LXM PRNG seed to install on the next module (re)install.
/// It is not cleared by {@link super::clear_state}, so a hot reload re-installs
/// against the same seed the host set before the first install.
pub fn set_lxm_seed(seed: u32) {
    lxm_seed_store().store(seed as u64, Ordering::SeqCst);
}
/// The configured LXM seed, or `None` when the host never set one (the JS side
/// then uses its 0x7FFFFFFF default).
pub fn lxm_seed() -> Option<u32> {
    let v = lxm_seed_store().load(Ordering::SeqCst);
    if v == u64::MAX { None } else { Some(v as u32) }
}
