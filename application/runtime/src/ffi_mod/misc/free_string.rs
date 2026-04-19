use std::ffi::CString;
use libc::c_char;
use std::os::raw::c_void;

#[link(name = "ole32")]
extern "system" {
    fn CoTaskMemFree(pv: *mut c_void);
}

#[export_name = "runtime_free_string"]
pub extern "C" fn runtime_free_string(s: *mut c_char) {
    if s.is_null() { return; }
    unsafe {
        CoTaskMemFree(s as *mut c_void);
    }
}
