use std::sync::atomic::{AtomicBool, Ordering};

// Controls whether the runtime should write to the native stdout/stderr streams.
// Default: false for library usage (suppresses native stdout writes that can corrupt forked JVM channels).
static NATIVE_STDOUT_ENABLED: AtomicBool = AtomicBool::new(false);

/// Check if native stdout is enabled.
pub fn is_native_stdout_enabled() -> bool {
    NATIVE_STDOUT_ENABLED.load(Ordering::SeqCst)
}

/// Enable or disable native stdout writes. Intended to be enabled by the CLI binary when
/// it intentionally writes raw bytes to stdout (e.g. --stdioDebugWithDelimiterWrap).
pub fn set_native_stdout_enabled(enabled: bool) {
    NATIVE_STDOUT_ENABLED.store(enabled, Ordering::SeqCst);
}

/// FFI-friendly setter so embedding languages can toggle native stdout behaviour if needed.
#[no_mangle]
pub extern "C" fn runtime_set_native_stdout_enabled(enabled: bool) {
    set_native_stdout_enabled(enabled);
}
