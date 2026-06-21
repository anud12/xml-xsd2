use std::ffi::CString;
use libc::c_char;
use super::ModuleRow;
use super::FileRow;

/// Convert Vec<Vec<String>> into C ModuleRow array.
pub unsafe fn module_rows_to_c_array(
    rows: Vec<Vec<String>>,
) -> (*mut ModuleRow, usize) {
    if rows.is_empty() {
        return (std::ptr::null_mut(), 0);
    }
    let mut out: Vec<ModuleRow> = Vec::with_capacity(rows.len());
    for r in rows.into_iter() {
        let id = cstr_new(r.get(0).cloned().unwrap_or_default());
        let name = cstr_new(r.get(1).cloned().unwrap_or_default());
        let version = cstr_new(r.get(2).cloned().unwrap_or_default());
        out.push(ModuleRow { id, name, version });
    }
    let len = out.len();
    let ptr = Box::into_raw(out.into_boxed_slice()) as *mut ModuleRow;
    (ptr, len)
}

/// Free a ModuleRow array allocated by module_rows_to_c_array.
pub unsafe fn free_module_array(
    ptr: *mut ModuleRow,
    len: usize,
) {
    if ptr.is_null() || len == 0 {
        return;
    }
    let slice = std::ptr::slice_from_raw_parts_mut(ptr, len);
    let boxed: Box<[ModuleRow]> = Box::from_raw(slice);
    for m in boxed.iter() {
        if !m.id.is_null() { let _ = CString::from_raw(m.id); }
        if !m.name.is_null() { let _ = CString::from_raw(m.name); }
        if !m.version.is_null() { let _ = CString::from_raw(m.version); }
    }
}

/// Free a FileRow array.
pub unsafe fn free_file_array(
    ptr: *mut FileRow,
    len: usize,
) {
    if ptr.is_null() || len == 0 {
        return;
    }
    let slice = std::ptr::slice_from_raw_parts_mut(ptr, len);
    let boxed: Box<[FileRow]> = Box::from_raw(slice);
    for f in boxed.iter() {
        if !f.filename.is_null() {
            let _ = CString::from_raw(f.filename);
        }
        if !f.contents.is_null() {
            let _ = CString::from_raw(f.contents);
        }
    }
}

/// Helper: create CString::into_raw from a String.
fn cstr_new(s: String) -> *mut c_char {
    CString::new(s)
        .unwrap_or_else(|_| CString::new("").unwrap())
        .into_raw()
}
