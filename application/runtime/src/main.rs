mod js_runtime;
mod js_host_api;
mod js_executor;
mod debug_loop;

use std::fs::File;
use std::io::{Read, Write};
use std::path::Path;
use std::collections::{HashMap, HashSet};
use rusqlite::Connection;
use crate::js_executor::extract_from_source;
use crate::js_host_api::Declarations;

fn read_zip_files(zip_path: &str) -> HashMap<String, String> {
    let file = match File::open(zip_path) {
        Ok(f) => f,
        Err(_) => return HashMap::new(),
    };
    let mut archive = match zip::ZipArchive::new(file) {
        Ok(a) => a,
        Err(_) => return HashMap::new(),
    };
    let mut files = HashMap::new();
    for i in 0..archive.len() {
        let mut f = match archive.by_index(i) {
            Ok(f) => f,
            Err(_) => continue,
        };
        let name = f.name().to_string();
        println!("loaded {}", name);
        let mut contents = String::new();
        f.read_to_string(&mut contents).unwrap_or_default();
        println!("{} loaded", name);
        files.insert(name, contents);
    }
    files
}

fn persist_state(path: &str, file_rows: &Vec<Vec<String>>, entity_rows: &Vec<Vec<String>>) -> String {
    let mut conn = Connection::open_in_memory().expect("open db");
    conn.execute_batch("CREATE TABLE IF NOT EXISTS files (file_name TEXT, contents TEXT);")
        .expect("create files table");
    conn.execute_batch("CREATE TABLE IF NOT EXISTS entity (firstName TEXT);")
        .expect("create entity table");
    let tx = conn.transaction().expect("tx");
    for row in file_rows.iter() {
        tx.execute("INSERT INTO files (file_name, contents) VALUES (?1, ?2)", &[&row[0], &row[1]]).expect("insert file");
    }
    for row in entity_rows.iter() {
        tx.execute("INSERT INTO entity (firstName) VALUES (?1)", &[&row[0]]).expect("insert entity");
    }
    tx.commit().expect("commit");
    // write to a unique file to avoid collisions
    let dest = format!("{}-{}.db", path, std::process::id());
    if Path::new(&dest).exists() {
        let _ = std::fs::remove_file(&dest);
    }
    let mut dest_conn = Connection::open(&dest).expect("open dest db");
    let backup = rusqlite::backup::Backup::new(&conn, &mut dest_conn).expect("backup");
    backup.step(-1).expect("backup step");
    dest
}

fn build_file_rows(files: &HashMap<String, String>) -> Vec<Vec<String>> {
    files.iter().map(|(n, c)| vec![n.clone(), c.clone()]).collect()
}

fn find_manifest(files: &HashMap<String, String>) -> Option<(String, serde_json::Value)> {
    if files.contains_key("manifest.json") {
        let name = "manifest.json".to_string();
        files.get(&name).and_then(|s| serde_json::from_str::<serde_json::Value>(s).ok()).map(|v| (name, v))
    } else {
        files.iter()
            .find(|(k, _)| k.starts_with("manifest") && k.ends_with(".json"))
            .and_then(|(k, s)| serde_json::from_str::<serde_json::Value>(s).ok().map(|v| (k.clone(), v)))
    }
}

fn print_events_from_declarations(dec: &Declarations) -> HashSet<String> {
    let mut seen = HashSet::new();
    for ev in dec.events.iter() {
        println!("event: {}", ev);
        println!("event registered: {}", ev);
        seen.insert(ev.clone());
    }
    seen
}

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
    let zip_path = find_zip_path(&args);

    println!("Runtime launched");
    std::io::stdout().flush().ok();

    let zip_path = zip_path.unwrap_or_default();
    let files = read_zip_files(&zip_path);
    let file_rows = build_file_rows(&files);
    let mut entity_rows: Vec<Vec<String>> = Vec::new();

    match find_manifest(&files) {
        Some((manifest_name, manifest_json)) => {
            println!("{} loaded", manifest_name);
            if let Some(entry) = manifest_json.get("entry").and_then(|v| v.as_str()) {
                if let Some(module) = files.get(entry) {
                    println!("{} loaded", entry);
                    if let Some(evt_name) = manifest_json.get("eventName").and_then(|v| v.as_str()) {
                        println!("event: {}", evt_name);
                        println!("event registered: {}", evt_name);
                    }

                    if let Ok(dec) = extract_from_source(module) {
                        print_events_from_declarations(&dec);
                        for l in dec.logs.iter() { println!("{}", l); }
                        for en in dec.entities.iter() { entity_rows.push(vec![format!("{},", en)]); }
                    } else {
                        eprintln!("js extraction failed; no fallback heuristics are used");
                    }
                } else {
                    eprintln!("entry {} not found in files", entry);
                }
            }
        }
        None => {
            println!("manifest.json not found");
            println!("module rejected");
        }
    }

    if !file_rows.is_empty() {
        let out = "state.db";
        println!("--SQLITE-START--");
        let dest = persist_state(out, &file_rows, &entity_rows);
        let mut f = File::open(dest).expect("open state");
        let mut buf = Vec::new();
        f.read_to_end(&mut buf).expect("read state");
        std::io::stdout().write_all(&buf).expect("write bytes");
    }

    std::io::stdout().flush().ok();

    if let Some(delim) = delimiter {
        debug_loop::run(&delim);
    }
}
