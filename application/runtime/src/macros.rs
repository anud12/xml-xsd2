macro_rules! debug_println {
    ($($arg:tt)*) => {
        if crate::native_stdio::is_native_stdout_enabled() {
            println!($($arg)*);
        }
    };
}
