use std::io::Write;

pub(super) fn log(msg: &str) {
    // Route through the registered log callback (Godot's GD.Print via the C#
    // logger) rather than raw eprintln!: the iteration loop runs on a
    // background thread, and eprintln! there races the main thread on native
    // stderr, producing garbled/misaligned output. send_log is dispatched to a
    // thread-safe sink on the host side.
    crate::native_stdio::send_log(msg);
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
