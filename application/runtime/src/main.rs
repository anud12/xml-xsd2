mod native_stdio;
#[macro_use]
mod macros;

mod js_runtime;
mod js_host_api;
mod js_executor;
mod debug_loop;
mod archive;
mod state;
mod export_helpers;
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

    archive::create_empty_zip_if_missing(&zip_path);
    let files = archive::read_zip_files(&zip_path);
    let file_rows = module::build_file_rows(&files);
    // store file_rows for export
    crate::state::set_last_file_rows(file_rows.clone());
    let mut entity_rows: Vec<Vec<String>> = Vec::new();

    module::process_module(&files, &mut entity_rows);
    // store entity rows after processing
    crate::state::set_last_entity_rows(entity_rows.clone());

    // Debug: print collected file rows
    runtime_log!("main: initial file_rows count {}", file_rows.len());
    for r in file_rows.iter() {
        if !r.is_empty() { runtime_log!("main: file={}", r[0]); }
    }

    state::persist_state(&file_rows, &entity_rows);

    if let Some(ref delim) = delimiter {
        crate::native_stdio::set_native_stdout_enabled(true);
        print!("{}", delim);
        std::io::stdout().flush().ok();
        debug_loop::run(delim);
    }
}
