use std::ffi::CString;
use libc::c_char;
use std::ptr;

/// All sector grid ids as a NULL-terminated `*mut *mut c_char`.
#[no_mangle]
pub extern "C" fn sector_grid_ids() -> *mut *mut c_char {
    let grids = crate::state::sector_grids().lock().unwrap();
    let mut vec: Vec<*mut c_char> = Vec::new();
    for id in grids.keys() {
        match CString::new(id.clone()) {
            Ok(c) => vec.push(c.into_raw()),
            Err(_) => vec.push(CString::new("").unwrap().into_raw()),
        }
    }
    vec.push(ptr::null_mut());
    let boxed = vec.into_boxed_slice();
    Box::into_raw(boxed) as *mut *mut c_char
}

/// The full `SectorGridState` JSON for one grid, or null if absent.
#[no_mangle]
pub extern "C" fn sector_grid_by_id(id: *const c_char) -> *mut c_char {
    if id.is_null() {
        return ptr::null_mut();
    }
    let id_str = unsafe {
        std::ffi::CStr::from_ptr(id).to_string_lossy().to_string()
    };
    let grids = crate::state::sector_grids().lock().unwrap();
    match grids.get(&id_str) {
        Some(state) => {
            let json = serde_json::to_string(state).unwrap_or_else(|_| "{}".into());
            CString::new(json).unwrap_or_else(|_| CString::new("{}").unwrap()).into_raw()
        }
        None => ptr::null_mut(),
    }
}

/// Free a string returned by `sector_grid_by_id`.
#[no_mangle]
pub extern "C" fn runtime_free_sector(p: *mut c_char) {
    if !p.is_null() {
        unsafe {
            let _ = CString::from_raw(p);
        }
    }
}
