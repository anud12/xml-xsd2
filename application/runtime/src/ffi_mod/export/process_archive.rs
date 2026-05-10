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

    crate::state::clear_state();
    crate::state::set_archive_path(zip_path);
    
    runtime_log!("process_archive: zip_path='{}' exists={} ", zip_path, std::path::Path::new(zip_path).exists());
    crate::archive_read::create_empty_zip_if_missing(zip_path);
    let files = crate::archive_read::read_zip_files(zip_path);
    runtime_log!("process_archive: read {} files", files.len());
    
    let file_rows = crate::module::build_file_rows(&files);
    crate::state::set_last_file_rows(file_rows.clone());
    crate::module::process_module(&files, &mut Vec::new());
    eprintln!("process_archive: panels after process_module = {:?}", crate::state::last_panels().lock().unwrap());
    let _entity_rows = crate::state::last_entity_rows().lock().unwrap().clone();

    crate::state::mark_persisted_has_data();
    
    match CString::new(zip_path) {
        Ok(s) => s.into_raw(),
        Err(_) => std::ptr::null_mut(),
    }
}
