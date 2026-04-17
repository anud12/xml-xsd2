use std::ffi::CString;
use crate::ffi_mod::types::*;

#[export_name = "runtime_export_state_struct"]
pub extern "C" fn runtime_export_state_struct() -> *mut ExportedState {
    // Collect cached rows and maps from state into an allocated C-friendly struct.
    let files_cached = crate::state::last_file_rows().lock().unwrap().clone();
    let entities_cached = crate::state::last_entity_rows().lock().unwrap().clone();
    let actions_cached = crate::state::last_action_rows().lock().unwrap().clone();
    let events_cached = crate::state::last_event_rows().lock().unwrap().clone();
    let modules_cached = crate::state::last_module_rows().lock().unwrap().clone();
    let patterns_cached = crate::state::last_entity_patterns().lock().unwrap().clone();
    let panels_cached = crate::state::last_panels().lock().unwrap().clone();
    let created_by_cached = crate::state::last_created_by().lock().unwrap().clone();
    // Debug: print panels cache to stderr for troubleshooting
    eprintln!("export_state_struct: panels_cached = {:?}", panels_cached);

    unsafe {
        // Convert simple string-lists (take first column of row vectors)
        let (entities_ptr, entities_len) = string_vec_to_c_array(entities_cached.into_iter().map(|r| r.get(0).cloned().unwrap_or_default()).collect());
        let (actions_ptr, actions_len) = string_vec_to_c_array(actions_cached.into_iter().map(|r| r.get(0).cloned().unwrap_or_default()).collect());
        // Normalize any 'effect' substrings to 'event' to match export_to_file behavior
        let norm_events: Vec<String> = events_cached.into_iter().map(|r| r.get(0).cloned().unwrap_or_default().replace("effect", "event")).collect();
        let (events_ptr, events_len) = string_vec_to_c_array(norm_events);
        let (patterns_ptr, patterns_len) = string_vec_to_c_array(patterns_cached);
        let (panels_ptr, panels_len) = string_vec_to_c_array(panels_cached);

        // Modules (id,name,version) and files (filename,contents)
        let (modules_ptr, modules_len) = module_rows_to_c_array(modules_cached);

        let mut file_rows_vec: Vec<FileRow> = Vec::new();
        for r in files_cached.iter() {
            if r.len() >= 2 {
                let filename = CString::new(r[0].clone()).unwrap_or_else(|_| CString::new("").unwrap()).into_raw();
                let contents = CString::new(r[1].clone()).unwrap_or_else(|_| CString::new("").unwrap()).into_raw();
                file_rows_vec.push(FileRow { filename, contents });
            }
        }
        let files_len = file_rows_vec.len();
        let files_ptr = if files_len == 0 { std::ptr::null_mut() } else { Box::into_raw(file_rows_vec.into_boxed_slice()) as *mut FileRow };

        let (created_by_ptr, created_by_len) = created_by_to_c_array(created_by_cached);

        let es = ExportedState {
            entities: CStringArray { len: entities_len, data: entities_ptr },
            actions: CStringArray { len: actions_len, data: actions_ptr },
            events: CStringArray { len: events_len, data: events_ptr },
            panels: CStringArray { len: panels_len, data: panels_ptr },
            modules: ModuleArray { len: modules_len, data: modules_ptr },
            files: FileArray { len: files_len, data: files_ptr },
            entity_patterns: CStringArray { len: patterns_len, data: patterns_ptr },
            created_by: CreatedByArray { len: created_by_len, data: created_by_ptr },
            has_data: !files_cached.is_empty() || entities_len > 0 || actions_len > 0 || events_len > 0 || modules_len > 0,
        };
        Box::into_raw(Box::new(es))
    }
}
