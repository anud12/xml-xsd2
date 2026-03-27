use std::env;
use std::fs::File;
use std::io::{Read, Write};
use std::path::Path;
use anyhow::{Context, Result};
use rusqlite::{params, Connection};
use zip::ZipArchive;

use std::io::Seek;

fn read_zip_files<R: Read + Seek>(reader: R) -> Result<Vec<(String, String)>> {
    let mut archive = ZipArchive::new(reader).context("Failed to read zip archive")?;
    let mut files = Vec::with_capacity(archive.len());
    for i in 0..archive.len() {
        let mut f = archive.by_index(i).context("Failed to access file in archive")?;
        let name = f.name().to_string();
        let mut buf = Vec::with_capacity(f.size() as usize);
        std::io::copy(&mut f, &mut buf).ok();
        // convert bytes to UTF-8 string (lossy) to store as TEXT
        let contents = String::from_utf8_lossy(&buf).into_owned();
        files.push((name, contents));
    }
    Ok(files)
}

fn persist_state(db_path: &Path, entries: &[(String, String)]) -> Result<()> {
    // keep primary state in-memory
    let mut mem = Connection::open_in_memory().context("Failed to open in-memory sqlite")?;
    mem.execute(
        "CREATE TABLE IF NOT EXISTS files (file_name TEXT PRIMARY KEY, contents TEXT NOT NULL)",
        [],
    )?;
    {
        let tx = mem.transaction()?;
        for (name, contents) in entries {
            tx.execute("REPLACE INTO files (file_name, contents) VALUES (?1, ?2)", params![name, contents])?;
        }
        tx.commit()?;
    }

    // persist in-memory DB to disk using VACUUM INTO for reliability
    let db_path_str = db_path.to_string_lossy().replace("'", "''");
    let vacuum_sql = format!("VACUUM INTO '{}';", db_path_str);
    mem.execute(&vacuum_sql, []).context("VACUUM INTO failed")?;
    // ensure connections closed so file is fully written
    mem.close().map_err(|(_conn, err)| err).context("Failed to close in-memory DB")?;
    Ok(())
}

fn main() -> Result<()> {
    let args: Vec<String> = env::args().collect();
    let zip_path = args.get(1).context("Missing path to zip file argument")?;
    let file = File::open(&zip_path).context("Failed to open zip file")?;

    let entries = read_zip_files(file).context("Reading zip files failed")?;

    // persist to temporary sqlite
    let tmp_db = env::temp_dir().join(format!("state_{}.db", std::process::id()));
    persist_state(&tmp_db, &entries).context("Persisting state failed")?;

    // ensure the sqlite file exists on disk (backup already created it)
    eprintln!("SQLite state written to: {}", tmp_db.display());

    // output delimiter then raw sqlite bytes to stdout
    println!("--SQLITE-START--");
    let mut db_file = File::open(tmp_db).context("Opening sqlite file failed")?;
    let mut buf = Vec::new();
    db_file.read_to_end(&mut buf)?;
    // write raw bytes to stdout
    let stdout = std::io::stdout();
    let mut handle = stdout.lock();
    handle.write_all(&buf)?;
    handle.flush()?;
    Ok(())
}
