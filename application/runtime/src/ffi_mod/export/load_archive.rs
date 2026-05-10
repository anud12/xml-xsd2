use libc::{c_uchar, c_int};

#[export_name = "runtime_load_archive"]
pub extern "C" fn runtime_load_archive(data_ptr: *const c_uchar, len: c_int) -> bool {
    runtime_log!("native: runtime_load_archive called with len={}", len);

    if data_ptr.is_null() {
        return false;
    }

    if len <= 0 {
        return false;
    }

    let len_usize = len as usize;

    let bytes: &[u8] = unsafe { std::slice::from_raw_parts(data_ptr as *const u8, len_usize) };

    let tmp = std::env::temp_dir().join(format!("debug_archive_{}.zip", std::process::id()));
    if let Err(e) = std::fs::write(&tmp, bytes) {
        runtime_log!("native: failed to write tmp archive: {}", e);
        return false;
    }
    let tmp_path = tmp.to_str().unwrap_or_default();

    let files = crate::archive_read::read_zip_files(tmp_path);
    let file_rows = crate::module::build_file_rows(&files.clone());
    crate::state::set_last_file_rows(file_rows.clone());
    crate::module::process_module(&files, &mut Vec::new());

    crate::state::mark_persisted_has_data();

    true
}
