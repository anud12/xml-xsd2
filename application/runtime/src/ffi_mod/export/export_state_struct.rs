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
    let mut panels_cached = crate::state::last_panels().lock().unwrap().clone();
    let created_by_cached = crate::state::last_created_by().lock().unwrap().clone();
    // Debug: print panels cache to stderr for troubleshooting
    // debug: panels_cached (disabled)
    // Fallback: if panels not registered by JS, try extracting from any panel.csv file in last_file_rows
    if panels_cached.is_empty() {
        let files_cached = crate::state::last_file_rows().lock().unwrap().clone();
        for row in files_cached.iter() {
            if row.len() >= 2 {
                let fname = row.get(0).unwrap().to_lowercase();
                if fname.contains("panel") && fname.contains(".csv") {
                    let contents = row.get(1).unwrap();
                    for line in contents.lines() {
                        let trimmed = line.trim();
                        if trimmed.is_empty() { continue; }
                        // skip common CSV header lines that include 'id' or non-alpha chars only
                        let first_col = trimmed.split(',').next().unwrap_or("").trim().trim_matches('"');
                        if first_col.eq_ignore_ascii_case("id") || first_col.is_empty() { continue; }
                        panels_cached.push(first_col.to_string());
                    }
                }
            }
        }
        // If still empty, try deriving a panel id from module cache (module id or name)
        if panels_cached.is_empty() {
            let modules_cached = crate::state::last_module_rows().lock().unwrap().clone();
            if !modules_cached.is_empty() {
                if let Some(row) = modules_cached.get(0) {
                    let id = row.get(0).cloned().unwrap_or_default();
                    let name = row.get(1).cloned().unwrap_or_default();
                    let chosen = if !id.is_empty() { id } else { name };
                    if !chosen.is_empty() { panels_cached.push(chosen); }
                }
            }
        }
        // If still empty, scan index.js source for registerPanel calls and any quoted 'panel' strings
        if panels_cached.is_empty() {
            let files_cached = crate::state::last_file_rows().lock().unwrap().clone();
            for row in files_cached.iter() {
                if row.len() >= 2 {
                    let fname = row.get(0).unwrap().to_lowercase();
                    if fname.ends_with("index.js") || fname.ends_with(".js") {
                        let src = row.get(1).unwrap();
                        // index.js diagnostic output disabled
                        // crude patterns: registerPanel('id') or registerPanel({ id: 'id' })
                        for cap in src.match_indices("registerPanel(") {
                            let start = cap.0 + cap.1.len();
                            if let Some(rest) = src.get(start..) {
                                if let Some(end_idx) = rest.find(')') {
                                    let arg = &rest[..end_idx];
                                    // look for quoted string (double or single)
                                    if let Some(qstart) = arg.find('"') {
                                        if let Some(qend) = arg[qstart+1..].find('"') {
                                            let val = &arg[qstart+1..qstart+1+qend];
                                            if !val.is_empty() { panels_cached.push(val.to_string()); }
                                        }
                                    }
                                    if let Some(qstart) = arg.find('\'') {
                                        if let Some(qend) = arg[qstart+1..].find('\'') {
                                            let val = &arg[qstart+1..qstart+1+qend];
                                            if !val.is_empty() { panels_cached.push(val.to_string()); }
                                        }
                                    }
                                    // try object id: look for id:
                                    if let Some(id_pos) = arg.find("id") {
                                        if let Some(colon) = arg[id_pos..].find(':') {
                                            let after = &arg[id_pos+colon+1..];
                                            let s = after.trim();
                                            let s = s.trim_matches(|c| c==' '||c=='"'||c=='\''||c=='}');
                                            if !s.is_empty() { panels_cached.push(s.to_string()); }
                                        }
                                    }
                                }
                            }
                        }
                        // additional fallback: collect any quoted substrings equal to 'panel' or containing 'panel'
                        for (i, _ch) in src.match_indices('"') {
                            // find closing
                            if let Some(rest) = src.get(i+1..) {
                                if let Some(end) = rest.find('"') {
                                    let val = &rest[..end];
                                    if val.to_lowercase().contains("panel") { panels_cached.push(val.to_string()); }
                                }
                            }
                        }
                        for (i, _ch) in src.match_indices('\'') {
                            if let Some(rest) = src.get(i+1..) {
                                if let Some(end) = rest.find('\'') {
                                    let val = &rest[..end];
                                    if val.to_lowercase().contains("panel") { panels_cached.push(val.to_string()); }
                                }
                            }
                        }
                    }
                }
            }
        }
        // panels_cached fallback diagnostic disabled
        // Debug list filenames available in files_cached to understand why fallback failed
        let files_cached = crate::state::last_file_rows().lock().unwrap().clone();
        let names: Vec<String> = files_cached.iter().filter_map(|r| r.get(0).cloned()).collect();
        // files_cached names diagnostic disabled
    }

    unsafe {
        // Convert simple string-lists (take first column of row vectors)
        let (entities_ptr, entities_len) = string_vec_to_c_array(entities_cached.into_iter().map(|r| r.get(0).cloned().unwrap_or_default()).collect());
        let (actions_ptr, actions_len) = string_vec_to_c_array(actions_cached.into_iter().map(|r| r.get(0).cloned().unwrap_or_default()).collect());
        // Normalize any 'effect' substrings to 'event' to match export_to_file behavior
        let norm_events: Vec<String> = events_cached.into_iter().map(|r| r.get(0).cloned().unwrap_or_default().replace("effect", "event")).collect();
        let (events_ptr, events_len) = string_vec_to_c_array(norm_events);
        let (patterns_ptr, patterns_len) = string_vec_to_c_array(patterns_cached);
        let (panels_ptr, panels_len) = panels_to_c_array(panels_cached);

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
            panels: PanelArray { len: panels_len, data: panels_ptr },
            modules: ModuleArray { len: modules_len, data: modules_ptr },
            files: FileArray { len: files_len, data: files_ptr },
            entity_patterns: CStringArray { len: patterns_len, data: patterns_ptr },
            created_by: CreatedByArray { len: created_by_len, data: created_by_ptr },
            has_data: !files_cached.is_empty() || entities_len > 0 || actions_len > 0 || events_len > 0 || modules_len > 0,
        };
        Box::into_raw(Box::new(es))
    }
}
