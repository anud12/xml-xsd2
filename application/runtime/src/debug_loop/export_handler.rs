use std::io::Write;

pub fn handle_export(path: &str, delimiter: &str) {
    crate::state::export_to_file(path);
    debug_println!("{delimiter}OK{delimiter}");
    std::io::stdout().flush().ok();
}
