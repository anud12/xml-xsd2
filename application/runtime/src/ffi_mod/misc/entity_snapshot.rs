use libc::c_char;
use std::ffi::{CStr, CString};
use crate::state::entity_map;

#[no_mangle]
pub extern "C" fn runtime_get_entity_snapshot(id: *const c_char) -> *mut c_char {
    if id.is_null() { return std::ptr::null_mut(); }
    let Ok(id) = (unsafe { CStr::from_ptr(id) }).to_str() else { return std::ptr::null_mut(); };
    let entities = entity_map().lock().unwrap();
    let Some(entity) = entities.get(id) else { return std::ptr::null_mut(); };
    json_ptr(entity)
}

#[no_mangle]
pub extern "C" fn runtime_get_entity_ids() -> *mut c_char {
    let entities = entity_map().lock().unwrap();
    let mut ids: Vec<_> = entities.keys().collect();
    ids.sort();
    json_ptr(&ids)
}

fn json_ptr(data: &impl serde::Serialize) -> *mut c_char {
    serde_json::to_string(data).ok().and_then(|s| CString::new(s).ok())
        .map(CString::into_raw).unwrap_or(std::ptr::null_mut())
}
