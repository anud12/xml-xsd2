use std::ffi::CString;
use std::os::raw::c_char;

#[no_mangle]
pub extern "C" fn runtime_drain_action_messages() -> *mut c_char {
    let mut queue = crate::state::action_messages().lock().unwrap();
    let messages = std::mem::take(&mut *queue);
    CString::new(serde_json::to_string(&messages).unwrap_or_else(|_| "[]".into())).unwrap().into_raw()
}
