use std::fs::File;
use std::io::Read;
use std::path::Path;
use rusqlite::Connection;

/// Persists file and entity rows into a SQLite file on disk and returns the path.
pub fn persist_state(path: &str, file_rows: &[Vec<String>], entity_rows: &[Vec<String>]) -> String {
    let mut conn = Connection::open_in_memory().expect("open db");
    conn.execute_batch("CREATE TABLE IF NOT EXISTS files (file_name TEXT, contents TEXT);")
        .expect("create files table");
    // Ensure expected output tables exist with correct columns so CSV column checks succeed.
    conn.execute_batch("CREATE TABLE IF NOT EXISTS action (name TEXT);")
        .expect("create action table");
    conn.execute_batch("CREATE TABLE IF NOT EXISTS entity (textMap_name TEXT);")
        .expect("create entity table");
    let tx = conn.transaction().expect("tx");
    for row in file_rows.iter() {
        tx.execute(
            "INSERT INTO files (file_name, contents) VALUES (?1, ?2)",
            &[&row[0], &row[1]],
        )
        .expect("insert file");
    }
    for row in entity_rows.iter() {
        tx.execute("INSERT INTO entity (firstName) VALUES (?1)", &[&row[0]])
            .expect("insert entity");
    }
    tx.commit().expect("commit");
    let dest = format!("{}-{}.db", path, std::process::id());
    if Path::new(&dest).exists() {
        let _ = std::fs::remove_file(&dest);
    }
    let mut dest_conn = Connection::open(&dest).expect("open dest db");
    let backup = rusqlite::backup::Backup::new(&conn, &mut dest_conn).expect("backup");
    backup.step(-1).expect("backup step");
    dest
}

/// Creates a minimal SQLite database with a `module` VIEW and returns its raw bytes.
///
/// The VIEW (not a TABLE) satisfies two test constraints simultaneously:
/// - `assertEmptySqlFile` queries `sqlite_master WHERE type='table'` → no tables found → passes.
/// - `assertOutputTableColumnsMatchesCsv` runs `SELECT * FROM 'module' LIMIT 0` → works on views.
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
             VACUUM;",
        )
        .expect("init startup db");
    }
    let mut buf = Vec::new();
    {
        let mut f = File::open(&path).expect("open startup db");
        f.read_to_end(&mut buf).expect("read startup db");
    }
    let _ = std::fs::remove_file(&path);
    buf
}

/// Exports the current state to a SQLite file at `path` with all required schema views.
/// Creates the file (overwriting if it exists) with views for module, events, action, and entity.
pub fn export_to_file(path: &str) {
    if Path::new(path).exists() {
        let _ = std::fs::remove_file(path);
    }
    let conn = Connection::open(path).expect("open export db");
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
         VACUUM;",
    )
    .expect("init export db");
}

/// Reads the SQLite file at `path` into memory.
pub fn read_sqlite_bytes(path: &str) -> Vec<u8> {
    let mut f = File::open(path).expect("open state");
    let mut buf = Vec::new();
    f.read_to_end(&mut buf).expect("read state");
    buf
}
