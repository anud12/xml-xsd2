use std::collections::HashMap;
use std::ffi::CStr;
use libc::c_char;

#[no_mangle]
pub extern "C" fn runtime_set_entity_number_map_value(
    entity_id: *const c_char,
    key: *const c_char,
    value: *const c_char,
) {
    if entity_id.is_null() || key.is_null() || value.is_null() {
        return;
    }
    let entity_id = unsafe { CStr::from_ptr(entity_id) }.to_string_lossy().to_string();
    let key = unsafe { CStr::from_ptr(key) }.to_string_lossy().to_string();
    let value: f64 = unsafe { CStr::from_ptr(value) }.to_string_lossy().parse().unwrap_or(0.0);

    let mut data = crate::state::last_entity_number_data().lock().unwrap();
    data.entry(entity_id)
        .or_insert_with(HashMap::new)
        .insert(key, value);
}
