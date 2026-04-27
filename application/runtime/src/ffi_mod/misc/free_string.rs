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

// Free a PanelFfi instance previously allocated by the runtime
#[no_mangle]
pub extern "C" fn runtime_free_panel(p: *mut crate::ffi_mod::types::PanelFfi) {
    if p.is_null() { return; }
    unsafe {
        // Convert back to owned Box to drop it; strings were allocated with CString::into_raw (CoTaskMemAlloc not used for struct fields)
        let panel = Box::from_raw(p);
        if !panel.id.is_null() {
            let _ = CString::from_raw(panel.id);
        }
        if !panel.background.is_null() {
            let _ = CString::from_raw(panel.background);
        }
        if !panel.children_json.is_null() {
            let _ = CString::from_raw(panel.children_json);
        }
        if !panel.panel_json.is_null() {
            let _ = CString::from_raw(panel.panel_json);
        }
        // Box dropped here
    }
}
