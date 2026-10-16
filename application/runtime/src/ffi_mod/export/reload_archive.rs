use std::ffi::{CStr, CString};
use libc::c_char;

/// Hot reload: re-reads the module archive at `path` and re-processes it
/// against the *live* runtime, keeping the previous game state. Entity values
/// the game already advanced win over the module's re-declared initial values,
/// the elapsed clock and parked action plans are untouched, and scheduled
/// effects that the reloaded module still registers keep their schedule.
/// Returns an empty string on success (caller must free) or null when the
/// reload is aborted (missing/invalid archive or module install failure), in
/// which case the previous module and state stay live.
#[export_name = "runtime_reload_archive"]
pub extern "C" fn runtime_reload_archive(path: *const c_char) -> *mut c_char {
    if path.is_null() { return std::ptr::null_mut(); }
    let zip_path = match unsafe { CStr::from_ptr(path) }.to_str() {
        Ok(s) if !s.is_empty() => s,
        _ => return std::ptr::null_mut(),
    };

    let files = crate::archive::read_zip_files(zip_path);
    if files.is_empty() {
        runtime_log!(
            "reload_archive: no files read from '{}', reload aborted", zip_path);
        return std::ptr::null_mut();
    }

    // Snapshot the live game state a reload must carry over: the current
    // entity values (text + number). The elapsed clock and parked action
    // plans are kept in place (this path never calls clear_state).
    let preserved_text =
        crate::state::last_entity_data().lock().unwrap().clone();
    let preserved_num =
        crate::state::last_entity_number_data().lock().unwrap().clone();

    // Install the new module before swapping any state markers, so an
    // install failure leaves the previous module and state fully live.
    if let Err(e) = crate::js_executor::sim_ctx::install_locked(&files) {
        runtime_log!("reload_archive: module install failed: {:?}", e);
        return std::ptr::null_mut();
    }

    crate::state::set_archive_path(zip_path);
    let file_rows = crate::module::build_file_rows(&files);
    crate::state::set_last_file_rows(file_rows.clone());
    crate::state::set_archive_files(files.clone());

    let mut entity_rows: Vec<Vec<String>> = Vec::new();
    crate::module::process_module(&files, &mut entity_rows);
    crate::state::set_last_entity_rows(entity_rows.clone());

    // Re-apply the preserved game state on top of the fresh declarations:
    // a value the game already advanced wins over the re-declared initial.
    let declared_text =
        crate::state::last_entity_data().lock().unwrap().clone();
    let declared_num =
        crate::state::last_entity_number_data().lock().unwrap().clone();
    let merged_text =
        crate::state::merge_entity_maps(&preserved_text, &declared_text);
    let merged_num =
        crate::state::merge_entity_maps(&preserved_num, &declared_num);
    // Containers were re-declared (positions re-baked from initial values);
    // restore the preserved positions so in-flight moves continue.
    crate::state::rebake_container_positions(
        &mut *crate::state::last_containers().lock().unwrap(), &merged_num);
    *crate::state::last_entity_data().lock().unwrap() = merged_text;
    *crate::state::last_entity_number_data().lock().unwrap() = merged_num;

    crate::state::drop_unresolvable_effects();

    crate::state::persist_state(&file_rows, &entity_rows);
    runtime_log!(
        "reload_archive: hot reload of '{}' complete, state preserved",
        zip_path);

    match CString::new(String::new()) {
        Ok(s) => s.into_raw(),
        Err(_) => std::ptr::null_mut(),
    }
}
