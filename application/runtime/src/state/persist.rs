use std::fs::File;
use std::io::Read;
use std::path::Path;
use std::sync::atomic::Ordering;
use rusqlite::Connection;

pub fn persist_state(path: &str, file_rows: &[Vec<String>], entity_rows: &[Vec<String>]) -> String {
    let mut conn = Connection::open_in_memory().expect("open db");
    conn.execute_batch("CREATE TABLE IF NOT EXISTS files (file_name TEXT, contents TEXT);")
        .expect("create files table");
    conn.execute_batch("CREATE TABLE IF NOT EXISTS module (id TEXT, name TEXT, version TEXT);")
        .expect("create module table");
    conn.execute_batch("CREATE TABLE IF NOT EXISTS events (name TEXT);")
        .expect("create events table");
    conn.execute_batch("CREATE TABLE IF NOT EXISTS action (name TEXT);")
        .expect("create action table");
    conn.execute_batch("CREATE TABLE IF NOT EXISTS entity (textMap_name TEXT);")
        .expect("create entity table");
    let module_rows = crate::export_helpers::insert_files_and_collect_modules(&mut conn, file_rows);
    crate::export_helpers::insert_entities(&mut conn, &entity_rows.to_vec());
    if !module_rows.is_empty() { super::set_last_module_rows(module_rows); }
    let dest = format!("{}-{}.db", path, std::process::id());
    if !file_rows.is_empty() || !entity_rows.is_empty() {
        super::persisted_flag().store(true, Ordering::SeqCst);
    }
    *super::last_file_rows().lock().unwrap() = file_rows.to_vec();
    *super::last_entity_rows().lock().unwrap() = entity_rows.to_vec();
    if Path::new(&dest).exists() { let _ = std::fs::remove_file(&dest); }
    let mut dest_conn = Connection::open(&dest).expect("open dest db");
    let backup = rusqlite::backup::Backup::new(&conn, &mut dest_conn).expect("backup");
    backup.step(-1).expect("backup step");
    dest
}

pub fn create_startup_sqlite_bytes() -> Vec<u8> {
    let path = format!("startup_state_{}.db", std::process::id());
    {
        let conn = Connection::open(&path).expect("create startup db");
        conn.execute_batch(
            "PRAGMA page_size = 512; \
             CREATE VIEW IF NOT EXISTS module AS \
               SELECT '' AS id, '' AS name, '' AS version WHERE 0; \
             CREATE VIEW IF NOT EXISTS events AS \
               SELECT '' AS name WHERE 0; \
             CREATE VIEW IF NOT EXISTS action AS \
               SELECT '' AS name WHERE 0; \
             CREATE VIEW IF NOT EXISTS entity AS \
               SELECT '' AS textMap_name WHERE 0; \
             CREATE VIEW IF NOT EXISTS panel AS \
               SELECT '' AS id WHERE 0; \
             VACUUM;",
        ).expect("init startup db");
    }
    let mut buf = Vec::new();
    {
        let mut f = File::open(&path).expect("open startup db");
        f.read_to_end(&mut buf).expect("read startup db");
    }
    let _ = std::fs::remove_file(&path);
    buf
}

#[allow(dead_code)]
fn try_copy_persisted_to(path: &str) -> bool {
    let persisted = format!("state.db-{}.db", std::process::id());
    if super::persisted_flag().load(Ordering::SeqCst)
        && Path::new(&persisted).exists()
    {
        let _ = std::fs::copy(&persisted, path).expect("copy persisted db");
        return true;
    }
    false
}
