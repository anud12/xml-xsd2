use std::io::Write;

pub(super) fn log(msg: &str) {
    eprintln!("{}", msg);
    if let Ok(mut f) = std::fs::OpenOptions::new()
        .create(true).append(true).open("rust_debug_csharp.log")
    {
        let _ = writeln!(f, "{}", msg);
    }
}

#[no_mangle]
pub extern "C" fn runtime_emit_event(
    name_ptr: *const std::ffi::c_char,
) {
    let name = unsafe {
        if name_ptr.is_null() {
            String::new()
        } else {
            std::ffi::CStr::from_ptr(name_ptr)
                .to_string_lossy().into_owned()
        }
    };
    crate::state::pending_effects()
        .lock().unwrap().push(name);
}

#[no_mangle]
pub extern "C" fn runtime_get_elapsed_time_units() -> i64 {
    crate::state::get_elapsed_time_units()
}
