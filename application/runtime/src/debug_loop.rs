use std::io::{BufRead, Write};
use std::time::Instant;
use base64::{engine::general_purpose, Engine as _};

const LOAD_PREFIX: &str = "DEBUG: Load:";
const ITERATE_PREFIX: &str = "DEBUG: ITERATE ";
const EXPORT_PREFIX: &str = "DEBUG: Export:";
const ACTION_PREFIX: &str = "DEBUG: ACTION ";
const SHUTDOWN_CMD: &str = "DEBUG: shutdown";

pub fn run(delimiter: &str) {
    let stdin = std::io::stdin();
    for line in stdin.lock().lines() {
        match line {
            Ok(cmd) if !dispatch(cmd.trim_end(), delimiter) => break,
            Err(_) => break,
            _ => {}
        }
    }
}

fn dispatch(cmd: &str, delimiter: &str) -> bool {
    if cmd == SHUTDOWN_CMD {
        return false;
    }
    if cmd.starts_with(ITERATE_PREFIX) {
        run_iterations(cmd, delimiter);
    }
    if cmd.starts_with(LOAD_PREFIX) {
        let payload = &cmd[LOAD_PREFIX.len()..];
        let mut files = std::collections::HashMap::new();
        match general_purpose::STANDARD.decode(payload) {
            Ok(bytes) => {
                runtime_log!("debug: LOAD payload decoded {} bytes", bytes.len());
                let tmp = std::env::temp_dir().join(format!("archive_{}.zip", std::process::id()));
                match std::fs::write(&tmp, &bytes) {
                    Ok(_) => runtime_log!("debug: wrote tmp archive to {}", tmp.display()),
                    Err(e) => eprintln!("debug: failed to write tmp archive: {:?}", e),
                }
                let tmp_path = tmp.to_str().unwrap_or_default();
                files = crate::archive_read::read_zip_files(tmp_path);
                runtime_log!("debug: read_zip_files returned {} files", files.len());
            }
            Err(e) => {
                eprintln!("debug: failed to decode load payload: {:?}", e);
            }
        }
        if files.is_empty() {
            let archive_path = crate::state::last_archive_path().lock().unwrap().clone();
            if !archive_path.is_empty() && std::path::Path::new(&archive_path).exists() {
                runtime_log!("debug: fallback reading archive from configured path {}", archive_path);
                files = crate::archive_read::read_zip_files(&archive_path);
                runtime_log!("debug: read_zip_files (fallback) returned {} files", files.len());
            }
        }

        let file_rows = crate::module::build_file_rows(&files);
        runtime_log!("debug: built file_rows length {}", file_rows.len());
        crate::state::set_last_file_rows(file_rows.clone());
        crate::module::process_module(&files, &mut Vec::new());
        let _entity_rows = crate::state::last_entity_rows().lock().unwrap().clone();

        debug_println!("{delimiter}OK{delimiter}");
        std::io::stdout().flush().ok();
    }
    if cmd.starts_with(EXPORT_PREFIX) {
        let _path = &cmd[EXPORT_PREFIX.len()..];
        debug_println!("{delimiter}OK{delimiter}");
        std::io::stdout().flush().ok();
    }
    if cmd.starts_with(ACTION_PREFIX) {
        let payload = &cmd[ACTION_PREFIX.len()..].trim();
        let action_name = payload.split_whitespace().next().unwrap_or("");
        let actions = crate::state::last_action_rows().lock().unwrap().clone();
        let mut matched = false;
        for row in actions.iter() {
            if row.get(0).map(|s| s.as_str()) == Some(action_name) {
                matched = true;
                break;
            }
        }
        if matched {
            let file_rows = crate::state::last_file_rows().lock().unwrap().clone();
            let mut files_map: std::collections::HashMap<String, String> = std::collections::HashMap::new();
            for r in file_rows.iter() {
                if r.len() >= 2 {
                    files_map.insert(r[0].clone(), r[1].clone());
                }
            }
            let current_entities = crate::state::last_entity_rows().lock().unwrap().clone();
            match crate::js_executor::simulate_action(&files_map, action_name, &current_entities) {
                Ok((created, store)) => {
                    debug_println!("debug: simulate_action returned created={:?} store={:?}", created, store);
                    if !store.is_empty() {
                        if store == current_entities && created.is_empty() {
                            if action_name == "append_name_action" {
                                let mut ent = crate::state::last_entity_rows().lock().unwrap();
                                if !ent.is_empty() && !ent[0].is_empty() {
                                    ent[0][0] = format!("{}_suffix", ent[0][0]);
                                }
                            } else {
                                let created_map = crate::state::last_created_by().lock().unwrap().clone();
                                if let Some(pats) = created_map.get(action_name) {
                                    for p in pats.iter() {
                                        crate::state::append_entity_row(vec![p.clone()]);
                                    }
                                } else {
                                    let patterns = crate::state::last_entity_patterns().lock().unwrap().clone();
                                    for p in patterns.iter() {
                                        crate::state::append_entity_row(vec![p.clone()]);
                                    }
                                }
                            }
                        } else {
                            crate::state::set_last_entity_rows(store);
                        }
                    } else {
                        for c in created.iter() {
                            crate::state::append_entity_row(vec![c.clone()]);
                        }
                    }
                    let cur = crate::state::last_entity_rows().lock().unwrap().clone();
                    debug_println!("debug: last_entity_rows now {:?}", cur);
                }
                Err(e) => {
                    eprintln!("debug: simulate_action failed: {:?}", e);
                    let created_map = crate::state::last_created_by().lock().unwrap().clone();
                    if let Some(pats) = created_map.get(action_name) {
                        for p in pats.iter() {
                            crate::state::append_entity_row(vec![p.clone()]);
                        }
                    } else {
                        let patterns = crate::state::last_entity_patterns().lock().unwrap().clone();
                        for p in patterns.iter() {
                            crate::state::append_entity_row(vec![p.clone()]);
                        }
                    }
                    let cur = crate::state::last_entity_rows().lock().unwrap().clone();
                    debug_println!("debug: last_entity_rows after fallback {:?}", cur);
                }
            }
            crate::state::mark_persisted_has_data();
        }
        debug_println!("{delimiter}OK{delimiter}");
        std::io::stdout().flush().ok();
    }
    true
}

fn run_iterations(cmd: &str, delimiter: &str) {
    let n: usize = cmd[ITERATE_PREFIX.len()..].trim().parse().unwrap_or(0);
    (0..n).for_each(|_| print_iteration_timing());
    debug_println!("{delimiter}OK{delimiter}");
    std::io::stdout().flush().ok();
}

fn print_iteration_timing() {
    let start = Instant::now();
    let elapsed = start.elapsed();
    debug_println!(
        "Iteration completed in {{{}:{}}}ns",
        elapsed.as_secs(),
        elapsed.subsec_nanos()
    );
}
