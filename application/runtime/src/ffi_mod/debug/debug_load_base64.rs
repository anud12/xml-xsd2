use std::ffi::{CStr, CString};
use libc::c_char;
use base64::{engine::general_purpose, Engine as _};

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
            crate::state::persist_state(&file_rows, &entity_rows);
            match CString::new("ok") { Ok(s) => s.into_raw(), Err(_) => std::ptr::null_mut(), }
        },
        Err(_) => std::ptr::null_mut(),
    }
}
