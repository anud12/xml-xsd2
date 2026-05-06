use std::ffi::CString;
use crate::ffi_mod::types::*;

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
        // Free panels
        free_panel_array(boxed.panels.data, boxed.panels.len);
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
