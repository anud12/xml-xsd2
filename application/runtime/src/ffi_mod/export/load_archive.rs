use libc::{c_uchar, c_int};

#[export_name = "runtime_load_archive"]
pub extern "C" fn runtime_load_archive(data_ptr: *const c_uchar, len: c_int) -> bool {
    // Log entry so Java-side test can observe the call
    runtime_log!("native: runtime_load_archive called with len={}", len);

    if data_ptr.is_null() {
        return false;
    }

    if len <= 0 {
        return false;
    }

    let len_usize = len as usize;

    // Safety: data_ptr points to a buffer of length `len_usize` provided by the caller.
    let bytes: &[u8] = unsafe { std::slice::from_raw_parts(data_ptr as *const u8, len_usize) };

    let tmp = std::env::temp_dir().join(format!("debug_archive_{}.zip", std::process::id()));
    if let Err(e) = std::fs::write(&tmp, bytes) {
        runtime_log!("native: failed to write tmp archive: {}", e);
        return false;
    }
    let tmp_path = tmp.to_str().unwrap_or_default();

    let files = crate::archive::read_zip_files(tmp_path);
    let file_rows = crate::module::build_file_rows(&files);
    crate::state::set_last_file_rows(file_rows.clone());
    let mut entity_rows: Vec<Vec<String>> = Vec::new();
    crate::module::process_module(&files, &mut entity_rows);
    crate::state::set_last_entity_rows(entity_rows.clone());
    crate::state::persist_state(&file_rows, &entity_rows);

    true
}
