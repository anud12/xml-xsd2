use std::fs::File;
use std::io::{Read, Write};
use std::path::Path;
use std::collections::HashMap;
use rusqlite::Connection;

fn read_zip_files(zip_path: &str) -> HashMap<String, String> {
    let file = File::open(zip_path).expect("open zip");
    let mut archive = zip::ZipArchive::new(file).expect("zip archive");
    let mut files = HashMap::new();
    for i in 0..archive.len() {
        let mut f = archive.by_index(i).expect("by_index");
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
    // vacuum into a unique file to avoid collisions
    let dest = format!("{}-{}.db", path, std::process::id());
    if Path::new(&dest).exists() {
        let _ = std::fs::remove_file(&dest);
    }
    // Use rusqlite backup API to write in-memory DB to file (more portable than VACUUM INTO)
    let mut dest_conn = Connection::open(&dest).expect("open dest db");
    let mut backup = rusqlite::backup::Backup::new(&conn, &mut dest_conn).expect("backup");
    backup.step(-1).expect("backup step");
    dest
}

fn main() {
    let zip_path = std::env::args().nth(1).expect("zip path");
    let files = read_zip_files(&zip_path);
    // Build files rows for persistence
    let mut file_rows: Vec<Vec<String>> = Vec::new();
    for (name, contents) in files.iter() {
        file_rows.push(vec![name.clone(), contents.clone()]);
    }

    let mut entity_rows: Vec<Vec<String>> = Vec::new();
    // find a manifest file: prefer "manifest.json", otherwise any manifest*.json
    let manifest_name_opt = if files.contains_key("manifest.json") {
        Some("manifest.json".to_string())
    } else {
        files.keys().find(|k| k.starts_with("manifest") && k.ends_with(".json")).cloned()
    };

    if let Some(manifest_name) = manifest_name_opt {
        if let Some(manifest_str) = files.get(&manifest_name) {
            if let Ok(manifest_json) = serde_json::from_str::<serde_json::Value>(manifest_str) {
                println!("{} loaded", manifest_name);
                if let Some(entry) = manifest_json.get("entry").and_then(|v| v.as_str()) {
                    if let Some(module) = files.get(entry) {
                        println!("{} loaded", entry);
                        if let Some(evt_name) = manifest_json.get("eventName").and_then(|v| v.as_str()) {
                            println!("event: {}", evt_name);
                            println!("event registered: {}", evt_name);
                        }
                        // heuristic scans
                        // First, search entire module for registerEvent name occurrences (covers multi-line objects)
                        if module.contains("registerEvent") {
                            for part in module.split("registerEvent").skip(1) {
                                if let Some(npos) = part.find("name:") {
                                    let tail = &part[npos..];
                                    if let Some(q1) = tail.find('"') {
                                        if let Some(q2) = tail[q1+1..].find('"') {
                                            let s = &tail[q1+1..q1+1+q2];
                                            println!("event: {}", s);
                                            println!("event registered: {}", s);
                                        }
                                    }
                                }
                            }
                        }

                        let mut in_register = false;
                        for line in module.lines() {
                            if line.contains("registerEvent") {
                                in_register = true;
                            }
                            if in_register && line.contains("name:") {
                                if let Some(nstart) = line.find("name:") {
                                    if let Some(q1) = line[nstart..].find('"') {
                                        if let Some(q2) = line[nstart+q1+1..].find('"') {
                                            let s = &line[nstart+q1+1..nstart+q1+1+q2];
                                            println!("event: {}", s);
                                            println!("event registered: {}", s);
                                        }
                                    }
                                }
                                in_register = false;
                            }
                            if in_register && line.contains("}") {
                                in_register = false;
                            }

                            if line.contains("string.of(") {
                                if let Some(start) = line.find("string.of(") {
                                    if let Some(q1) = line[start..].find('"') {
                                        if let Some(q2) = line[start+q1+1..].find('"') {
                                            let s = &line[start+q1+1..start+q1+1+q2];
                                            println!("event: {}", s);
                                            // if this line also mentions firstName, treat as entity creation
                                            if line.contains("firstName") {
                                                entity_rows.push(vec![format!("{},", s.to_string())]);
                                            }
                                        }
                                    }
                                }
                            }
                            if line.contains("emitEvent(") {
                                if let Some(start) = line.find("emitEvent(") {
                                    if let Some(q1) = line[start..].find('"') {
                                        if let Some(q2) = line[start+q1+1..].find('"') {
                                            let s = &line[start+q1+1..start+q1+1+q2];
                                            println!("event: {}", s);
                                        }
                                    }
                                }
                            }

                            if line.contains("createEntity") || line.contains("entity.create") {
                                // very naive: look for firstName: "X"
                                if let Some(nstart) = line.find("firstName") {
                                    if let Some(q1) = line[nstart..].find('"') {
                                        if let Some(q2) = line[nstart+q1+1..].find('"') {
                                            let s = &line[nstart+q1+1..nstart+q1+1+q2];
                                            entity_rows.push(vec![format!("{},", s.to_string())]);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    } else {
        println!("manifest.json not found");
        println!("module rejected");
    }

    let out = "state.db";
    println!("--SQLITE-START--");
    let dest = persist_state(out, &file_rows, &entity_rows);
    let mut f = File::open(dest).expect("open state");
    let mut buf = Vec::new();
    f.read_to_end(&mut buf).expect("read state");
    std::io::stdout().write_all(&buf).expect("write bytes");
}
