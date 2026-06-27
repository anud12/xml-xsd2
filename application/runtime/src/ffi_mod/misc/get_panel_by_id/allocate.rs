use std::ffi::CString;
use std::os::raw::c_void;
use libc::c_char;
use std::ptr;

#[cfg(target_os = "windows")]
#[link(name = "ole32")]
extern "system" {
    fn CoTaskMemAlloc(cb: usize) -> *mut c_void;
}

pub(crate) fn allocate_cstr(s: &str) -> *mut c_char {
    let bytes = CString::new(s).unwrap_or_else(
        |_| CString::new("").unwrap()
    );
    let len = bytes.to_bytes_with_nul().len();
    let mem = unsafe { libc::malloc(len) as *mut i8 };
    if mem.is_null() { return ptr::null_mut(); }
    unsafe {
        ptr::copy_nonoverlapping(
            bytes.as_ptr() as *const i8, mem, len
        );
    }
    mem as *mut c_char
}

#[cfg(target_os = "windows")]
pub(crate) fn allocate_wstr(s: &str) -> *mut u16 {
    use std::os::windows::ffi::OsStrExt;
    use std::ffi::OsString;
    let mut wide: Vec<u16> =
        OsString::from(s).encode_wide().collect();
    wide.push(0);
    let byte_len = wide.len() * std::mem::size_of::<u16>();
    let mem = unsafe { CoTaskMemAlloc(byte_len) as *mut u16 };
    if mem.is_null() { return ptr::null_mut(); }
    unsafe { ptr::copy_nonoverlapping(
        wide.as_ptr(), mem, wide.len()
    ); }
    mem
}
