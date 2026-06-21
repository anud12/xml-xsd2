use std::collections::HashMap;
use std::io::Write;

pub fn build_files_map() -> HashMap<String, String> {
    let file_rows = crate::state::last_file_rows()
        .lock().unwrap().clone();
    runtime_log!("DEBUG: Building files map from {} cached file rows",
        file_rows.len());

    let mut map: HashMap<String, String> = HashMap::new();
    for r in file_rows.iter() {
        if r.len() >= 2 {
            runtime_log!("DEBUG: File row: {} -> {} chars", r[0], r[1].len());
            log_file_row(r);
            map.insert(r[0].clone(), r[1].clone());
        }
    }
    map
}

fn log_file_row(row: &[String]) {
    if let Ok(mut f) = std::fs::OpenOptions::new()
        .create(true).append(true).open("C:\\temp\\rust_debug.log")
    {
        let _ = writeln!(f, "[{}]   cached file '{}' = {} chars",
            std::process::id(), row[0], row[1].len());
    }
}
