mod native_stdio;
#[macro_use]
mod macros;

mod js_runtime;
mod js_host_api;
mod js_executor;
mod debug_loop;
mod archive;
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

    debug_println!("Runtime launched");
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
    debug_println!("main: initial file_rows count {}", file_rows.len());
    for r in file_rows.iter() {
        if !r.is_empty() { debug_println!("main: file={}", r[0]); }
    }

    if let Some(ref delim) = delimiter {
        crate::native_stdio::set_native_stdout_enabled(true);
        // 8 invalid UTF-8 bytes shift byteStart (Java's re-encoded byte count) forward by 16,
        // landing it exactly after "--SQLITE-START--" (16 chars) and onto the SQLite magic bytes.
        std::io::stdout().write_all(&[0x80u8; 8]).expect("write alignment bytes");
        print!("--SQLITE-START--"); // no trailing newline: SQLite bytes follow immediately
        let sqlite_bytes = if !file_rows.is_empty() {
            let dest = state::persist_state("state.db", &file_rows, &entity_rows);
            state::read_sqlite_bytes(&dest)
        } else {
            state::create_startup_sqlite_bytes()
        };
        std::io::stdout().write_all(&sqlite_bytes).expect("write sqlite bytes");
        print!("{}", delim);
        // Extra padding so byteEnd (Java's re-encoded offset) never exceeds lastOutput.length
        for _ in 0..50 {
            print!("\n");
        }
        std::io::stdout().flush().ok();
        debug_loop::run(delim);
    }
}
