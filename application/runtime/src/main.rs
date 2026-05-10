mod native_stdio;
#[macro_use]
mod macros;

mod js_runtime;
mod js_host_api;
mod js_executor;
mod debug_loop;
mod archive_read;
mod state;
mod module;

use std::io::Write;

fn extract_debug_delimiter(args: &[String]) -> Option<String> {
    args.iter()
        .find(|a| a.starts_with("--stdioDebugWithDelimiterWrap="))
        .map(|a| a["--stdioDebugWithDelimiterWrap=".len()..].to_string())
}

fn find_zip_path(args: &[String]) -> Option<String> {
    args.iter().skip(1).find(|a| !a.starts_with("--")).cloned()
}

fn main() {
    let args: Vec<String> = std::env::args().collect();
    let delimiter = extract_debug_delimiter(&args);
    let zip_path = find_zip_path(&args).unwrap_or_default();
    crate::state::set_archive_path(&zip_path);

    runtime_log!("Runtime launched");
    std::io::stdout().flush().ok();

    archive_read::create_empty_zip_if_missing(&zip_path);
    let files = archive_read::read_zip_files(&zip_path);
    let file_rows = module::build_file_rows(&files);
    crate::state::set_last_file_rows(file_rows.clone());

    module::process_module(&files, &mut Vec::new());
    let _entity_rows = crate::state::last_entity_rows().lock().unwrap().clone();

    runtime_log!("main: initial file_rows count {}", file_rows.len());
    for r in file_rows.iter() {
        if !r.is_empty() { runtime_log!("main: file={}", r[0]); }
    }

    if let Some(ref delim) = delimiter {
        crate::native_stdio::set_native_stdout_enabled(true);
        debug_println!("{delim}OK{delim}");
        std::io::stdout().flush().ok();
        debug_loop::run(delim);
    }
}
