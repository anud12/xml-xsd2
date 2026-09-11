use std::sync::atomic::Ordering;

/// Record the loaded file/entity state in memory. The runtime keeps its state
/// in-process (see the accessors); no SQLite snapshot file is written. Module
/// rows are already set by `module::process_module` (called before this) via
/// `set_module_rows`.
pub fn persist_state(file_rows: &[Vec<String>], entity_rows: &[Vec<String>]) {
    if !file_rows.is_empty() || !entity_rows.is_empty() {
        super::persisted_flag().store(true, Ordering::SeqCst);
    }
    *super::last_file_rows().lock().unwrap() = file_rows.to_vec();
    *super::last_entity_rows().lock().unwrap() = entity_rows.to_vec();
}
