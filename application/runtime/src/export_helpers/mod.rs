use rusqlite::Connection;
use rusqlite::Transaction;

mod inserters;
mod module_rows;

pub use inserters::*;
pub use module_rows::*;

pub fn init_in_memory_export_db() -> Connection {
    let c = Connection::open_in_memory()
        .expect("open in-memory export db");
    c.execute_batch(
        "PRAGMA page_size = 512; \
         CREATE TABLE IF NOT EXISTS module \
         (id TEXT, name TEXT, version TEXT); \
         CREATE TABLE IF NOT EXISTS events (name TEXT); \
         CREATE TABLE IF NOT EXISTS action (name TEXT); \
         CREATE TABLE IF NOT EXISTS entity (textMap_name TEXT); \
         CREATE TABLE IF NOT EXISTS panel (id); VACUUM;",
    )
    .expect("init in-memory export db");
    c
}

fn is_manifest(f: &str) -> bool {
    f.ends_with("manifest.json") || f.contains("manifest")
}

fn mf(v: &serde_json::Value) -> (&str, &str, &str) {
    (v.get("id").and_then(|x| x.as_str()).unwrap_or(""),
     v.get("name").and_then(|x| x.as_str()).unwrap_or(""),
     v.get("version").and_then(|x| x.as_str()).unwrap_or(""))
}

fn ins_mod(tx: &Transaction, i: &str, n: &str, v: &str) {
    tx.execute(
        "INSERT INTO module (id, name, version) \
         VALUES (?1, ?2, ?3)", &[&i, &n, &v]).ok();
}

pub fn insert_module_rows_from_cache_or_files(
    conn: &mut Connection,
    mc: &Vec<Vec<String>>,
    files: &Vec<Vec<String>>,
) {
    if !mc.is_empty() {
        let tx = conn.transaction().expect("tx_mod");
        for r in mc.iter() {
            ins_mod(&tx,
                r.get(0).map(|s| s.as_str()).unwrap_or(""),
                r.get(1).map(|s| s.as_str()).unwrap_or(""),
                r.get(2).map(|s| s.as_str()).unwrap_or(""),
            );
        }
        tx.commit().ok();
    } else if !files.is_empty() {
        let tx = conn.transaction().expect("tx");
        for r in files.iter() {
            if r.len() >= 2 && is_manifest(&r[0]) {
                if let Ok(v) =
                    serde_json::from_str::<serde_json::Value>(&r[1])
                {
                    ins_mod(&tx, mf(&v).0, mf(&v).1, mf(&v).2);
                }
            }
        }
        tx.commit().ok();
    }
}
