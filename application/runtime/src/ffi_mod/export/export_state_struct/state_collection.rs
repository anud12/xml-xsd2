use std::collections::HashMap;
use std::ffi::CString;
use crate::ffi_mod::types::*;

pub fn build_exported_state(
    files_cached: Vec<Vec<String>>,
    entities_cached: Vec<Vec<String>>,
    actions_cached: Vec<Vec<String>>,
    events_cached: Vec<Vec<String>>,
    modules_cached: Vec<Vec<String>>,
    patterns_cached: Vec<String>,
    panels_cached: Vec<String>,
    created_by_cached: HashMap<String, Vec<String>>,
) -> *mut ExportedState {
    unsafe {
        let (entities_ptr, entities_len) = string_vec_to_c_array(
            entities_cached.into_iter()
                .map(|r| r.get(0).cloned().unwrap_or_default()).collect(),
        );
        let (actions_ptr, actions_len) = string_vec_to_c_array(
            actions_cached.into_iter()
                .map(|r| r.get(0).cloned().unwrap_or_default()).collect(),
        );
        let norm_events: Vec<String> = events_cached.into_iter()
            .map(|r| r.get(0).cloned().unwrap_or_default()
                .replace("effect", "event"))
            .collect();
        let (events_ptr, events_len) = string_vec_to_c_array(norm_events);
        let (patterns_ptr, patterns_len) = string_vec_to_c_array(patterns_cached);
        let (panels_ptr, panels_len) = panels_to_c_array(panels_cached);
        let (modules_ptr, modules_len) = module_rows_to_c_array(modules_cached);

        let mut file_rows_vec: Vec<FileRow> = Vec::new();
        for r in files_cached.iter() {
            if r.len() >= 2 {
                let f = CString::new(r[0].clone())
                    .unwrap_or_else(|_| CString::new("").unwrap())
                    .into_raw();
                let c = CString::new(r[1].clone())
                    .unwrap_or_else(|_| CString::new("").unwrap())
                    .into_raw();
                file_rows_vec.push(FileRow { filename: f, contents: c });
            }
        }
        let files_len = file_rows_vec.len();
        let files_ptr = if files_len == 0 {
            std::ptr::null_mut()
        } else {
            Box::into_raw(file_rows_vec.into_boxed_slice()) as *mut FileRow
        };

        let (cb_ptr, cb_len) = created_by_to_c_array(created_by_cached);

        let es = ExportedState {
            entities: CStringArray { len: entities_len, data: entities_ptr },
            actions: CStringArray { len: actions_len, data: actions_ptr },
            events: CStringArray { len: events_len, data: events_ptr },
            panels: PanelArray { len: panels_len, data: panels_ptr },
            modules: ModuleArray { len: modules_len, data: modules_ptr },
            files: FileArray { len: files_len, data: files_ptr },
            entity_patterns: CStringArray { len: patterns_len, data: patterns_ptr },
            created_by: CreatedByArray { len: cb_len, data: cb_ptr },
            has_data: !files_cached.is_empty()
                || entities_len > 0 || actions_len > 0
                || events_len > 0 || modules_len > 0,
        };
        Box::into_raw(Box::new(es))
    }
}
