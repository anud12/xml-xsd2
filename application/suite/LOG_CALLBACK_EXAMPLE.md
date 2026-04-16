Example: capturing runtime logs via callback

The runtime now supports registering a log callback so embedding code (tests, harnesses) can receive
log messages emitted by the runtime instead of relying on stdout. Two API forms are available:

- Rust API: runtime::native_stdio::set_log_callback(Some(fn(&str)))
- FFI C API: runtime_set_log_callback_c(callback: extern "C" fn(*const c_char))

Rust example (in-suite test):

```rust
// register a simple Rust callback to collect messages
let mut msgs: Vec<String> = Vec::new();
fn collect(s: &str) { msgs.push(s.to_string()); }
runtime::native_stdio::set_log_callback(Some(collect));

// run the runtime code that emits debug_println!(...) messages
// ...

// clear callback
runtime::native_stdio::set_log_callback(None);
```

C / JVM embedding example (pseudocode):

```c
void my_log_callback(const char* s) { /* forward into JVM or test harness */ }
// register
runtime_set_log_callback_c(my_log_callback);
// later clear
runtime_set_log_callback_c(NULL);
```

The suite's Java harness can call the FFI function to route logs into the test runner instead of
allowing the runtime to write to native stdout. This is helpful when tests expect raw sqlite bytes
on stdout while still asserting on log messages.