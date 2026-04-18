use rusqlite::{Connection, Transaction};


// Initialize an in-memory export DB with the minimal tables used by export
pub fn init_in_memory_export_db() -> Connection {
    let mem_conn = Connection::open_in_memory().expect("open in-memory export db");
    mem_conn
        .execute_batch(
            "PRAGMA page_size = 512; \
             CREATE TABLE IF NOT EXISTS module (id TEXT, name TEXT, version TEXT); \
             CREATE TABLE IF NOT EXISTS events (name TEXT); \
             CREATE TABLE IF NOT EXISTS action (name TEXT); \
             CREATE TABLE IF NOT EXISTS entity (textMap_name TEXT); \
             CREATE TABLE IF NOT EXISTS panel (id TEXT); \
             VACUUM;",
        )
        .expect("init in-memory export db");
    mem_conn
}

pub fn insert_module_rows_from_cache_or_files(mem_conn: &mut Connection, modules_cached: &Vec<Vec<String>>, files: &Vec<Vec<String>>) {
    if !modules_cached.is_empty() {
        runtime_log!("export: inserting module rows from module cache ({})", modules_cached.len());
        let tx_m = mem_conn.transaction().expect("tx_mod");
        for row in modules_cached.iter() {
            let id = row.get(0).map(|s| s.as_str()).unwrap_or("");
            let name = row.get(1).map(|s| s.as_str()).unwrap_or("");
            let version = row.get(2).map(|s| s.as_str()).unwrap_or("");
            tx_m
                .execute(
                    "INSERT INTO module (id, name, version) VALUES (?1, ?2, ?3)",
                    &[&id, &name, &version],
                )
                .ok();
        }
        tx_m.commit().ok();
    } else if !files.is_empty() {
        runtime_log!("export: inserting module rows from {} file rows", files.len());
        let tx = mem_conn.transaction().expect("tx");
        for row in files.iter() {
            if row.len() >= 2 {
                let fname = row.get(0).map(|s| s.as_str()).unwrap_or("");
                let contents = row.get(1).map(|s| s.as_str()).unwrap_or("");
                if fname.ends_with("manifest.json") || fname.contains("manifest") {
                    if let Ok(v) = serde_json::from_str::<serde_json::Value>(contents) {
                        let id = v.get("id").and_then(|x| x.as_str()).unwrap_or("");
                        let name = v.get("name").and_then(|x| x.as_str()).unwrap_or("");
                        let version = v.get("version").and_then(|x| x.as_str()).unwrap_or("");
                        runtime_log!("export: found manifest {} {} {}", id, name, version);
                        tx.execute(
                            "INSERT INTO module (id, name, version) VALUES (?1, ?2, ?3)",
                            &[&id, &name, &version],
                        )
                        .ok();
                    } else {
                        runtime_log!("export: manifest {} failed to parse as json", fname);
                    }
                }
            }
        }
        tx.commit().ok();
    } else {
        // fallback handled by caller
    }
}

pub fn insert_actions(mem_conn: &mut Connection, actions: &Vec<Vec<String>>) {
    if actions.is_empty() { return; }
    let tx = mem_conn.transaction().expect("tx_actions");
    for row in actions.iter() {
        tx.execute("INSERT INTO action (name) VALUES (?1)", &[&row[0]]).ok();
    }
    tx.commit().ok();
}

pub fn insert_events(mem_conn: &mut Connection, events: &Vec<Vec<String>>) {
    if events.is_empty() { return; }
    let tx = mem_conn.transaction().expect("tx_events");
    for row in events.iter() {
        let val = row.get(0).map(|s| s.as_str()).unwrap_or("");
        let norm = val.replace("effect", "event");
        tx.execute("INSERT INTO events (name) VALUES (?1)", &[&norm]).ok();
    }
    tx.commit().ok();
}

pub fn insert_entities(mem_conn: &mut Connection, entities: &Vec<Vec<String>>) {
    if entities.is_empty() { return; }
    let tx = mem_conn.transaction().expect("tx2");
    for row in entities.iter() {
        tx.execute("INSERT INTO entity (textMap_name) VALUES (?1)", &[&row[0]]).ok();
    }
    tx.commit().ok();
}

pub fn insert_panels(mem_conn: &mut Connection, panels: &Vec<String>) {
    if panels.is_empty() { return; }
    let txp = mem_conn.transaction().expect("tx_panels");
    for p in panels.iter() {
        txp.execute("INSERT INTO panel (id) VALUES (?1)", &[&p]).ok();
    }
    txp.commit().ok();
}

pub fn process_manifest_row(tx: &Transaction, fname: &str, contents: &str, module_rows_cache: &mut Vec<Vec<String>>) {
    if let Ok(v) = serde_json::from_str::<serde_json::Value>(contents) {
        let id = v.get("id").and_then(|x| x.as_str()).unwrap_or("");
        let name = v.get("name").and_then(|x| x.as_str()).unwrap_or("");
        let version = v.get("version").and_then(|x| x.as_str()).unwrap_or("");
        tx.execute("INSERT INTO module (id, name, version) VALUES (?1, ?2, ?3)", &[&id, &name, &version]).ok();
        module_rows_cache.push(vec![id.to_string(), name.to_string(), version.to_string()]);
    } else {
        runtime_log!("export: manifest {} failed to parse as json", fname);
    }
}

pub fn insert_files_and_collect_modules(mem_conn: &mut Connection, file_rows: &[Vec<String>]) -> Vec<Vec<String>> {
    if file_rows.is_empty() { return Vec::new(); }
    mem_conn.execute_batch("CREATE TABLE IF NOT EXISTS files (file_name TEXT, contents TEXT);").ok();
    let tx = mem_conn.transaction().expect("tx_files");
    let mut module_rows_cache: Vec<Vec<String>> = Vec::new();
    for row in file_rows.iter() {
        if row.len() < 2 { continue; }
        let fname = row.get(0).map(|s| s.as_str()).unwrap_or("");
        let contents = row.get(1).map(|s| s.as_str()).unwrap_or("");
        tx.execute("INSERT INTO files (file_name, contents) VALUES (?1, ?2)", &[&fname, &contents]).ok();
        if fname.ends_with("manifest.json") || fname.contains("manifest") {
            process_manifest_row(&tx, fname, contents, &mut module_rows_cache);
        }
    }
    tx.commit().ok();
    module_rows_cache
}
