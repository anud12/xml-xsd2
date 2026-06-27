use std::ffi::{CStr, CString};
use std::ptr;
use super::allocate::allocate_wstr;
use super::narrow::get_panel_by_id_c;

// Wide (UTF-16) export for .NET DllImport(CharSet=Unicode)
#[export_name = "get_panel_by_id_wide"]
pub extern "system" fn get_panel_by_id_wide(
    id: *const u16
) -> *mut u16 {
    use std::slice;
    use std::os::windows::ffi::OsStringExt;
    use std::ffi::OsString;

    if id.is_null() { return ptr::null_mut(); }
    unsafe {
        let mut len = 0usize;
        while *id.add(len) != 0 { len += 1; }
        let slice = slice::from_raw_parts(id, len);
        let os = OsString::from_wide(slice);
        let id_str = os.to_string_lossy().into_owned();

        let c_input = CString::new(id_str.clone())
            .unwrap_or_else(|_| CString::new("").unwrap());
        let res_ptr = get_panel_by_id_c(c_input.as_ptr());
        if res_ptr.is_null() { return ptr::null_mut(); }
        let res_cstr = CStr::from_ptr(res_ptr);
        let res_str =
            res_cstr.to_string_lossy().into_owned();
        crate::ffi_mod::runtime_free_string(res_ptr);

        allocate_wstr(&res_str)
    }
}
