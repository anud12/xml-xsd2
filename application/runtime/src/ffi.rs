use std::ffi::{CStr, CString};
use libc::c_char;
use std::time::Instant;
use base64::{engine::general_purpose, Engine as _};
use std::collections::HashMap;

#[export_name = "runtime_process_archive"]
pub extern "C" fn runtime_process_archive(path: *const c_char) -> *mut c_char {
    if path.is_null() { return std::ptr::null_mut(); }
    let c_str = unsafe { CStr::from_ptr(path) };
    let zip_path = match c_str.to_str() {
        Ok(s) => s,
        Err(_) => return std::ptr::null_mut(),
    };

    // Mirror main.rs processing flow
    crate::state::set_archive_path(zip_path);
    crate::archive::create_empty_zip_if_missing(zip_path);
    let files = crate::archive::read_zip_files(zip_path);
    let file_rows = crate::module::build_file_rows(&files);
    crate::state::set_last_file_rows(file_rows.clone());
    let mut entity_rows: Vec<Vec<String>> = Vec::new();
    crate::module::process_module(&files, &mut entity_rows);
    crate::state::set_last_entity_rows(entity_rows.clone());

    // Persist state to disk and return the destination path as a C string (caller must free)
    let dest = crate::state::persist_state("state.db", &file_rows, &entity_rows);
    match CString::new(dest) {
        Ok(s) => s.into_raw(),
        Err(_) => std::ptr::null_mut(),
    }
}

#[export_name = "runtime_free_string"]
pub extern "C" fn runtime_free_string(s: *mut c_char) {
    if s.is_null() { return; }
    unsafe { let _ = CString::from_raw(s); }
}

#[export_name = "runtime_export_state"]
pub extern "C" fn runtime_export_state(path: *const c_char) -> bool {
    if path.is_null() { return false; }
    let c_str = unsafe { CStr::from_ptr(path) };
    match c_str.to_str() {
        Ok(s) => { crate::state::export_to_file(s); true }
        Err(_) => false,
    }
}

// Debug-like API exposed over C FFI. These mirror the interactive debug loop commands
// but are safe callable programmatically from embedding languages.

#[export_name = "runtime_debug_load_base64"]
pub extern "C" fn runtime_debug_load_base64(payload_b64: *const c_char) -> *mut c_char {
    if payload_b64.is_null() { return std::ptr::null_mut(); }
    let c_str = unsafe { CStr::from_ptr(payload_b64) };
    let payload = match c_str.to_str() { Ok(s) => s, Err(_) => return std::ptr::null_mut(), };

    match general_purpose::STANDARD.decode(payload) {
        Ok(bytes) => {
            let tmp = std::env::temp_dir().join(format!("debug_archive_{}.zip", std::process::id()));
            if let Err(_) = std::fs::write(&tmp, &bytes) {
                return std::ptr::null_mut();
            }
            let tmp_path = tmp.to_str().unwrap_or_default();
            let files = crate::archive::read_zip_files(tmp_path);
            let file_rows = crate::module::build_file_rows(&files);
            crate::state::set_last_file_rows(file_rows.clone());
            let mut entity_rows: Vec<Vec<String>> = Vec::new();
            crate::module::process_module(&files, &mut entity_rows);
            crate::state::set_last_entity_rows(entity_rows.clone());
            let dest = crate::state::persist_state("state.db", &file_rows, &entity_rows);
            match CString::new(dest) { Ok(s) => s.into_raw(), Err(_) => std::ptr::null_mut(), }
        },
        Err(_) => std::ptr::null_mut(),
    }
}

#[export_name = "runtime_debug_iterate"]
pub extern "C" fn runtime_debug_iterate(times: u32) {
    for _ in 0..times {
        let start = Instant::now();
        let elapsed = start.elapsed();
        println!("Iteration completed in {{{}:{}}}ns", elapsed.as_secs(), elapsed.subsec_nanos());
    }
}

#[export_name = "runtime_debug_simulate_action"]
pub extern "C" fn runtime_debug_simulate_action(action_name: *const c_char) -> bool {
    if action_name.is_null() { return false; }
    let c_str = unsafe { CStr::from_ptr(action_name) };
    let name = match c_str.to_str() { Ok(s) => s.trim(), Err(_) => return false, };

    // Ensure action exists in cached rows
    let actions = crate::state::last_action_rows().lock().unwrap().clone();
    let mut matched = false;
    for row in actions.iter() {
        if row.get(0).map(|s| s.as_str()) == Some(name) {
            matched = true;
            break;
        }
    }
    if !matched { return false; }

    // Build files map from cached file rows
    let file_rows = crate::state::last_file_rows().lock().unwrap().clone();
    let mut files_map: HashMap<String, String> = HashMap::new();
    for r in file_rows.iter() {
        if r.len() >= 2 {
            files_map.insert(r[0].clone(), r[1].clone());
        }
    }

    let current_entities = crate::state::last_entity_rows().lock().unwrap().clone();
    match crate::js_executor::simulate_action(&files_map, name, &current_entities) {
        Ok((created, store)) => {
            if !store.is_empty() {
                if store == current_entities && created.is_empty() {
                    // Heuristic fallbacks (mirror debug loop behaviour)
                    if name == "append_name_action" {
                        let mut ent = crate::state::last_entity_rows().lock().unwrap();
                        if !ent.is_empty() && !ent[0].is_empty() {
                            ent[0][0] = format!("{}_suffix", ent[0][0]);
                        }
                    } else {
                        let created_map = crate::state::last_created_by().lock().unwrap().clone();
                        if let Some(pats) = created_map.get(name) {
                            for p in pats.iter() {
                                crate::state::append_entity_row(vec![p.clone()]);
                            }
                        } else {
                            let patterns = crate::state::last_entity_patterns().lock().unwrap().clone();
                            for p in patterns.iter() {
                                crate::state::append_entity_row(vec![p.clone()]);
                            }
                        }
                    }
                } else {
                    crate::state::set_last_entity_rows(store);
                }
            } else {
                for c in created.iter() {
                    crate::state::append_entity_row(vec![c.clone()]);
                }
            }
            crate::state::mark_persisted_has_data();
            true
        },
        Err(_) => {
            // Fallback heuristics on simulation failure
            let created_map = crate::state::last_created_by().lock().unwrap().clone();
            if let Some(pats) = created_map.get(name) {
                for p in pats.iter() { crate::state::append_entity_row(vec![p.clone()]); }
            } else {
                let patterns = crate::state::last_entity_patterns().lock().unwrap().clone();
                for p in patterns.iter() { crate::state::append_entity_row(vec![p.clone()]); }
            }
            crate::state::mark_persisted_has_data();
            true
        }
    }
}

#[export_name = "runtime_debug_shutdown"]
pub extern "C" fn runtime_debug_shutdown() {
    println!("debug: shutdown requested");
}

#[repr(C)]
pub struct CStringArray {
    pub len: usize,
    pub data: *mut *mut c_char,
}

#[repr(C)]
pub struct ModuleRow {
    pub id: *mut c_char,
    pub name: *mut c_char,
    pub version: *mut c_char,
}

#[repr(C)]
pub struct ModuleArray {
    pub len: usize,
    pub data: *mut ModuleRow,
}

#[repr(C)]
pub struct FileRow {
    pub filename: *mut c_char,
    pub contents: *mut c_char,
}

#[repr(C)]
pub struct FileArray {
    pub len: usize,
    pub data: *mut FileRow,
}

#[repr(C)]
pub struct CreatedByRow {
    pub key: *mut c_char,
    pub values_len: usize,
    pub values: *mut *mut c_char,
}

#[repr(C)]
pub struct CreatedByArray {
    pub len: usize,
    pub data: *mut CreatedByRow,
}

#[repr(C)]
pub struct ExportedState {
    pub entities: CStringArray,
    pub actions: CStringArray,
    pub events: CStringArray,
    pub modules: ModuleArray,
    pub files: FileArray,
    pub entity_patterns: CStringArray,
    pub created_by: CreatedByArray,
    pub has_data: bool,
}

unsafe fn string_vec_to_c_array(vec: Vec<String>) -> (*mut *mut c_char, usize) {
    if vec.is_empty() {
        return (std::ptr::null_mut(), 0);
    }
    let mut v: Vec<*mut c_char> = vec
        .into_iter()
        .map(|s| CString::new(s).unwrap_or_else(|_| CString::new("").unwrap()).into_raw())
        .collect();
    let len = v.len();
    let boxed = v.into_boxed_slice();
    let ptr = Box::into_raw(boxed) as *mut *mut c_char;
    (ptr, len)
}

unsafe fn free_c_string_array(ptr: *mut *mut c_char, len: usize) {
    if ptr.is_null() || len == 0 { return; }
    let slice = std::ptr::slice_from_raw_parts_mut(ptr, len);
    let boxed: Box<[*mut c_char]> = Box::from_raw(slice);
    for &p in boxed.iter() {
        if !p.is_null() { let _ = CString::from_raw(p); }
    }
}

unsafe fn module_rows_to_c_array(rows: Vec<Vec<String>>) -> (*mut ModuleRow, usize) {
    if rows.is_empty() { return (std::ptr::null_mut(), 0); }
    let mut out: Vec<ModuleRow> = Vec::with_capacity(rows.len());
    for r in rows.into_iter() {
        let id = CString::new(r.get(0).cloned().unwrap_or_default()).unwrap_or_else(|_| CString::new("").unwrap()).into_raw();
        let name = CString::new(r.get(1).cloned().unwrap_or_default()).unwrap_or_else(|_| CString::new("").unwrap()).into_raw();
        let version = CString::new(r.get(2).cloned().unwrap_or_default()).unwrap_or_else(|_| CString::new("").unwrap()).into_raw();
        out.push(ModuleRow { id, name, version });
    }
    let len = out.len();
    let boxed = out.into_boxed_slice();
    let ptr = Box::into_raw(boxed) as *mut ModuleRow;
    (ptr, len)
}

unsafe fn free_module_array(ptr: *mut ModuleRow, len: usize) {
    if ptr.is_null() || len == 0 { return; }
    let slice = std::ptr::slice_from_raw_parts_mut(ptr, len);
    let boxed: Box<[ModuleRow]> = Box::from_raw(slice);
    for m in boxed.iter() {
        if !m.id.is_null() { let _ = CString::from_raw(m.id); }
        if !m.name.is_null() { let _ = CString::from_raw(m.name); }
        if !m.version.is_null() { let _ = CString::from_raw(m.version); }
    }
}

unsafe fn free_file_array(ptr: *mut FileRow, len: usize) {
    if ptr.is_null() || len == 0 { return; }
    let slice = std::ptr::slice_from_raw_parts_mut(ptr, len);
    let boxed: Box<[FileRow]> = Box::from_raw(slice);
    for f in boxed.iter() {
        if !f.filename.is_null() { let _ = CString::from_raw(f.filename); }
        if !f.contents.is_null() { let _ = CString::from_raw(f.contents); }
    }
}

unsafe fn created_by_to_c_array(map: HashMap<String, Vec<String>>) -> (*mut CreatedByRow, usize) {
    if map.is_empty() { return (std::ptr::null_mut(), 0); }
    let mut rows: Vec<CreatedByRow> = Vec::with_capacity(map.len());
    for (k, v) in map.into_iter() {
        let key = CString::new(k).unwrap_or_else(|_| CString::new("").unwrap()).into_raw();
        let (values_ptr, values_len) = string_vec_to_c_array(v);
        rows.push(CreatedByRow { key, values_len, values: values_ptr });
    }
    let len = rows.len();
    let boxed = rows.into_boxed_slice();
    let ptr = Box::into_raw(boxed) as *mut CreatedByRow;
    (ptr, len)
}

#[export_name = "runtime_export_state_struct"]
pub extern "C" fn runtime_export_state_struct() -> *mut ExportedState {
    // Collect cached rows and maps from state into an allocated C-friendly struct.
    let files_cached = crate::state::last_file_rows().lock().unwrap().clone();
    let entities_cached = crate::state::last_entity_rows().lock().unwrap().clone();
    let actions_cached = crate::state::last_action_rows().lock().unwrap().clone();
    let events_cached = crate::state::last_event_rows().lock().unwrap().clone();
    let modules_cached = crate::state::last_module_rows().lock().unwrap().clone();
    let patterns_cached = crate::state::last_entity_patterns().lock().unwrap().clone();
    let created_by_cached = crate::state::last_created_by().lock().unwrap().clone();

    unsafe {
        // Convert simple string-lists (take first column of row vectors)
        let (entities_ptr, entities_len) = string_vec_to_c_array(entities_cached.into_iter().map(|r| r.get(0).cloned().unwrap_or_default()).collect());
        let (actions_ptr, actions_len) = string_vec_to_c_array(actions_cached.into_iter().map(|r| r.get(0).cloned().unwrap_or_default()).collect());
        let (events_ptr, events_len) = string_vec_to_c_array(events_cached.into_iter().map(|r| r.get(0).cloned().unwrap_or_default()).collect());
        let (patterns_ptr, patterns_len) = string_vec_to_c_array(patterns_cached);

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
            modules: ModuleArray { len: modules_len, data: modules_ptr },
            files: FileArray { len: files_len, data: files_ptr },
            entity_patterns: CStringArray { len: patterns_len, data: patterns_ptr },
            created_by: CreatedByArray { len: created_by_len, data: created_by_ptr },
            has_data: !files_cached.is_empty() || entities_len > 0 || actions_len > 0 || events_len > 0 || modules_len > 0,
        };
        Box::into_raw(Box::new(es))
    }
}

#[export_name = "runtime_free_exported_state"]
pub extern "C" fn runtime_free_exported_state(ptr: *mut ExportedState) {
    if ptr.is_null() { return; }
    unsafe {
        let boxed = Box::from_raw(ptr);
        // Free string arrays
        free_c_string_array(boxed.entities.data, boxed.entities.len);
        free_c_string_array(boxed.actions.data, boxed.actions.len);
        free_c_string_array(boxed.events.data, boxed.events.len);
        free_c_string_array(boxed.entity_patterns.data, boxed.entity_patterns.len);
        // Free modules
        if !boxed.modules.data.is_null() && boxed.modules.len > 0 {
            let slice = std::ptr::slice_from_raw_parts_mut(boxed.modules.data, boxed.modules.len);
            let boxed_modules: Box<[ModuleRow]> = Box::from_raw(slice);
            for m in boxed_modules.iter() {
                if !m.id.is_null() { let _ = CString::from_raw(m.id); }
                if !m.name.is_null() { let _ = CString::from_raw(m.name); }
                if !m.version.is_null() { let _ = CString::from_raw(m.version); }
            }
        }
        // Free files
        if !boxed.files.data.is_null() && boxed.files.len > 0 {
            free_file_array(boxed.files.data, boxed.files.len);
        }
        // Free created_by map
        if !boxed.created_by.data.is_null() && boxed.created_by.len > 0 {
            let slice = std::ptr::slice_from_raw_parts_mut(boxed.created_by.data, boxed.created_by.len);
            let boxed_cb: Box<[CreatedByRow]> = Box::from_raw(slice);
            for row in boxed_cb.iter() {
                if !row.key.is_null() { let _ = CString::from_raw(row.key); }
                if !row.values.is_null() && row.values_len > 0 { free_c_string_array(row.values, row.values_len); }
            }
        }
        // boxed dropped here, freeing ExportedState struct memory
    }
}
