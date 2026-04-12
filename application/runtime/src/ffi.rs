use std::ffi::{CStr, CString};
use libc::c_char;

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
    unsafe { CString::from_raw(s); }
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
