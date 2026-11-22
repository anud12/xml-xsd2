use std::ffi::CStr;
use libc::c_char;

#[no_mangle]
pub extern "C" fn runtime_set_entity_text_map_value(
    entity_id: *const c_char,
    key: *const c_char,
    value: *const c_char,
) {
    if entity_id.is_null() || key.is_null() || value.is_null() {
        return;
    }
    let entity_id = unsafe { CStr::from_ptr(entity_id) }.to_string_lossy().to_string();
    let key = unsafe { CStr::from_ptr(key) }.to_string_lossy().to_string();
    let value = unsafe { CStr::from_ptr(value) }.to_string_lossy().to_string();

    let mut data = crate::state::entity_map().lock().unwrap();
    crate::state::ensure_entity(&mut data, &entity_id).string_map
        .insert(key, value);
}
