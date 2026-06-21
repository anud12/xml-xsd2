use rusqlite::Connection;
use rusqlite::Transaction;

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

pub fn process_manifest_row(
    tx: &Transaction, fname: &str, contents: &str,
    cache: &mut Vec<Vec<String>>,
) {
    if let Ok(v) = serde_json::from_str::<serde_json::Value>(contents) {
        let (id, name, ver) = mf(&v);
        ins_mod(tx, id, name, ver);
        cache.push(vec![id.into(), name.into(), ver.into()]);
    } else {
        runtime_log!("manifest {} parse failed", fname);
    }
}

pub fn insert_files_and_collect_modules(
    conn: &mut Connection,
    file_rows: &[Vec<String>],
) -> Vec<Vec<String>> {
    if file_rows.is_empty() { return Vec::new(); }
    conn.execute_batch(
        "CREATE TABLE IF NOT EXISTS files \
         (file_name TEXT, contents TEXT);").ok();
    let tx = conn.transaction().expect("tx_files");
    let mut cache: Vec<Vec<String>> = Vec::new();
    for r in file_rows.iter() {
        if r.len() < 2 { continue; }
        tx.execute(
            "INSERT INTO files (file_name, contents) \
             VALUES (?1, ?2)",
            &[&r[0].as_str(), &r[1].as_str()]).ok();
        if is_manifest(&r[0]) {
            process_manifest_row(&tx, &r[0], &r[1], &mut cache);
        }
    }
    tx.commit().ok();
    cache
}
