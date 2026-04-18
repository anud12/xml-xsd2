#![allow(dead_code)]
use std::sync::atomic::{AtomicBool, AtomicPtr, Ordering};
use std::ffi::CString;
use std::os::raw::c_char;

// Controls whether the runtime should write to the native stdout/stderr streams.
// Default: false for library usage (suppresses native stdout writes that can corrupt forked JVM channels).
static NATIVE_STDOUT_ENABLED: AtomicBool = AtomicBool::new(false);

// Optional Rust-side log callback pointer (fn(&str)). Stored as a raw pointer.
static LOG_CALLBACK: AtomicPtr<std::ffi::c_void> = AtomicPtr::new(std::ptr::null_mut());
// Optional C-compatible callback pointer (extern "C" fn(*const c_char)).
static C_LOG_CALLBACK: AtomicPtr<std::ffi::c_void> = AtomicPtr::new(std::ptr::null_mut());

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
#[allow(dead_code)]
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
    C_LOG_CALLBACK.store(cb as *mut _, Ordering::SeqCst);
}

/// Backwards-compatible exported name for Java tests: register_logger
#[no_mangle]
pub extern "C" fn register_logger(cb: *const std::ffi::c_void) {
    C_LOG_CALLBACK.store(cb as *mut _, Ordering::SeqCst);
}

/// Send a log message. If a callback is registered it is invoked, otherwise it falls back to
/// printing to native stdout when enabled.
pub fn send_log(msg: &str) {
    // First try C callback
    let cptr = C_LOG_CALLBACK.load(Ordering::SeqCst);
    if !cptr.is_null() {
        // SAFETY: assume the pointer is an extern "C" fn(*const c_char).
        let c_fn: extern "C" fn(*const c_char) = unsafe { std::mem::transmute(cptr) };
        let cstr = CString::new(msg).unwrap_or_else(|_| CString::new("").unwrap());
        c_fn(cstr.as_ptr());
        return;
    }

    // Then try Rust fn(&str) callback
    let rptr = LOG_CALLBACK.load(Ordering::SeqCst);
    if !rptr.is_null() {
        // SAFETY: if the pointer was set via set_log_callback it is a `fn(&str)` pointer casted to void*.
        let f: fn(&str) = unsafe { std::mem::transmute(rptr) };
        f(msg);
    } else if is_native_stdout_enabled() {
        // Fallback to native stdout if enabled.
        println!("{}", msg);
    }
}

