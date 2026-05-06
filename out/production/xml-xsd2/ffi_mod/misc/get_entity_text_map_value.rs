use std::ffi::{CStr, CString};
use libc::c_char;

#[no_mangle]
pub extern "C" fn get_entity_text_map_value(
    entity_id: *const c_char,
    key: *const c_char,
) -> *mut c_char {
    if entity_id.is_null() || key.is_null() {
        return CString::new("").unwrap().into_raw();
    }
    let entity_id = unsafe { CStr::from_ptr(entity_id) }.to_string_lossy().to_string();
    let key = unsafe { CStr::from_ptr(key) }.to_string_lossy().to_string();

    let data = crate::state::last_entity_data().lock().unwrap();
    let value = data
        .get(&entity_id)
        .and_then(|tm| tm.get(&key))
        .map(|s| s.as_str())
        .unwrap_or("");

    CString::new(value).unwrap_or_else(|_| CString::new("").unwrap()).into_raw()
}
