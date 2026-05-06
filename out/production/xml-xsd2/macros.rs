macro_rules! runtime_log {
    ($($arg:tt)*) => {
        crate::native_stdio::send_log(&format!($($arg)*))
    };
}

// Backwards-compatible alias: some modules historically used debug_println!.
macro_rules! debug_println {
    ($($arg:tt)*) => {
        runtime_log!($($arg)*);
    };
}