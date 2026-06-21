use std::io::Write;
use base64::{engine::general_purpose, Engine as _};

pub fn handle_load(payload: &str, delimiter: &str) {
    let mut files = std::collections::HashMap::new();
    match general_purpose::STANDARD.decode(payload) {
        Ok(bytes) => {
            runtime_log!("debug: LOAD payload decoded {} bytes", bytes.len());
            let tmp = std::env::temp_dir()
                .join(format!("archive_{}.zip", std::process::id()));
            match std::fs::write(&tmp, &bytes) {
                Ok(_) => {
                    runtime_log!("debug: wrote tmp archive to {}", tmp.display())
                }
                Err(e) => {
                    eprintln!("debug: failed to write tmp archive: {:?}", e)
                }
            }
            let tmp_path = tmp.to_str().unwrap_or_default();
            files = crate::archive::read_zip_files(tmp_path);
            runtime_log!("debug: read_zip_files returned {} files", files.len());
        }
        Err(e) => {
            eprintln!("debug: failed to decode load payload: {:?}", e);
        }
    }
    if files.is_empty() {
        let archive_path =
            crate::state::last_archive_path().lock().unwrap().clone();
        if !archive_path.is_empty()
            && std::path::Path::new(&archive_path).exists()
        {
            runtime_log!(
                "debug: fallback reading archive from configured path {}",
                archive_path
            );
            files = crate::archive::read_zip_files(&archive_path);
            runtime_log!(
                "debug: read_zip_files (fallback) returned {} files",
                files.len()
            );
        }
    }
    let file_rows = crate::module::build_file_rows(&files);
    runtime_log!("debug: built file_rows length {}", file_rows.len());
    crate::state::set_last_file_rows(file_rows.clone());
    let mut entity_rows: Vec<Vec<String>> = Vec::new();
    crate::module::process_module(&files, &mut entity_rows);
    crate::state::set_last_entity_rows(entity_rows.clone());
    let dest = crate::state::persist_state(
        "state.db", &file_rows, &entity_rows,
    );
    runtime_log!("debug: persist_state wrote {}", dest);
    debug_println!("{delimiter}OK{delimiter}");
    std::io::stdout().flush().ok();
}
