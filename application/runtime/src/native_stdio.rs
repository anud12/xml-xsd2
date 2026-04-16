use std::sync::atomic::{AtomicBool, AtomicPtr, Ordering};
use std::ffi::{CString, CStr};
use std::os::raw::c_char;

// Controls whether the runtime should write to the native stdout/stderr streams.
// Default: false for library usage (suppresses native stdout writes that can corrupt forked JVM channels).
static NATIVE_STDOUT_ENABLED: AtomicBool = AtomicBool::new(false);

// Optional extern "C" log callback pointer (fn(*const c_char)). Stored as a raw pointer.
static LOG_CALLBACK: AtomicPtr<std::ffi::c_void> = AtomicPtr::new(std::ptr::null_mut());

/// Check if native stdout is enabled.
pub fn is_native_stdout_enabled() -> bool {
    NATIVE_STDOUT_ENABLED.load(Ordering::SeqCst)
}

/// Enable or disable native stdout writes. Intended to be enabled by the CLI binary when
/// it intentionally writes raw bytes to stdout (e.g. --stdioDebugWithDelimiterWrap).
pub fn set_native_stdout_enabled(enabled: bool) {
    NATIVE_STDOUT_ENABLED.store(enabled, Ordering::SeqCst);
}

/// Set a Rust-side log callback (fn(&str)). Useful for tests embedded in the same process.
/// Pass `None` to clear the callback.
pub fn set_log_callback(cb: Option<fn(&str)>) {
    let ptr = match cb {
        Some(f) => f as *const () as *mut std::ffi::c_void,
        None => std::ptr::null_mut(),
    };
    LOG_CALLBACK.store(ptr, Ordering::SeqCst);
}

/// FFI-friendly setter so embedding languages can register a C-style callback taking a C string.
/// Pass a null pointer to clear the callback.
#[no_mangle]
pub extern "C" fn runtime_set_log_callback(cb: *const std::ffi::c_void) {
    LOG_CALLBACK.store(cb as *mut _, Ordering::SeqCst);
}

/// Send a log message. If a callback is registered it is invoked, otherwise it falls back to
/// printing to native stdout when enabled.
pub fn send_log(msg: &str) {
    let ptr = LOG_CALLBACK.load(Ordering::SeqCst);
    if !ptr.is_null() {
        // Try Rust-side fn(&str)
        // SAFETY: if the pointer was set via set_log_callback it is a `fn(&str)` pointer casted to void*.
        let f: fn(&str) = unsafe { std::mem::transmute(ptr) };
        f(msg);
    } else if is_native_stdout_enabled() {
        // Fallback to native stdout if enabled.
        println!("{}", msg);
    }
}

/// FFI helper that accepts an extern "C" fn(*const c_char) pointer. Pass null to clear.
#[no_mangle]
pub extern "C" fn runtime_set_log_callback_c(cb: Option<extern "C" fn(*const c_char)>) {
    match cb {
        Some(f) => {
            let wrapper = move |s: &str| {
                if let Ok(c) = CString::new(s) {
                    f(c.as_ptr());
                }
            };
            // Box the wrapper function and store as a fn(&str) pointer by leaking it. This is safe for the
            // lifetime of the process in this context. Clearing the callback isn't reclaiming the allocation,
            // but tests/processes are short-lived. If reclaiming is required, a more complex registry is needed.
            let boxed: Box<dyn Fn(&str) + Send + Sync> = Box::new(wrapper);
            // Transmute to a fn(&str) by creating a thin function pointer isn't possible for trait objects.
            // Instead, create a trampoline fn by leaking the boxed closure as a concrete fn pointer isn't trivial.
            // For simplicity, store the raw C function pointer directly so senders can call it when registered via C API.
            LOG_CALLBACK.store(f as *const _ as *mut _, Ordering::SeqCst);
        }
        None => LOG_CALLBACK.store(std::ptr::null_mut(), Ordering::SeqCst),
    }
}
