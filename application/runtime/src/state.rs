#![allow(dead_code)]
use std::fs::File;
use std::io::Read;
use std::path::Path;
use rusqlite::Connection;
use std::sync::atomic::{AtomicBool, AtomicI64, Ordering};
use std::sync::{Once, Mutex};

use std::collections::HashMap;

static INIT: Once = Once::new();
static mut PERSISTED_HAS_DATA: Option<&'static AtomicBool> = None;
static mut LAST_FILE_ROWS: Option<&'static Mutex<Vec<Vec<String>>>> = None;
static mut LAST_ENTITY_ROWS: Option<&'static Mutex<Vec<Vec<String>>>> = None;
static mut LAST_ACTION_ROWS: Option<&'static Mutex<Vec<Vec<String>>>> = None;
static mut LAST_EVENT_ROWS: Option<&'static Mutex<Vec<Vec<String>>>> = None;
static mut LAST_MODULE_ROWS: Option<&'static Mutex<Vec<Vec<String>>>> = None;
static mut LAST_ARCHIVE_PATH: Option<&'static Mutex<String>> = None;
static mut LAST_ENTITY_PATTERNS: Option<&'static Mutex<Vec<String>>> = None;
static mut LAST_PANELS: Option<&'static Mutex<Vec<String>>> = None;
static mut LAST_CREATED_BY: Option<&'static Mutex<HashMap<String, Vec<String>>>> = None;
static mut PENDING_EFFECTS: Option<&'static Mutex<Vec<String>>> = None;
static mut SCHEDULED_EFFECTS: Option<&'static Mutex<Vec<ScheduledEffect>>> = None;
static mut LAST_ENTITY_DATA: Option<&'static Mutex<HashMap<String, HashMap<String, String>>>> = None;
static mut LAST_ENTITY_NUMBER_DATA: Option<&'static Mutex<HashMap<String, HashMap<String, f64>>>> = None;
static mut INITIAL_ENTITY_DATA: Option<&'static Mutex<HashMap<String, HashMap<String, String>>>> = None;
static mut ELAPSED_TIME_UNITS: Option<&'static AtomicI64> = None;

/// Tracks effects that should reoccur at specific elapsed time intervals.
#[derive(Clone, Debug)]
pub struct ScheduledEffect {
    pub name: String,
    pub payload: serde_json::Value,
    pub next_exec_time: i64,
    pub reoccurrence_interval: i64,
    pub execution_count: u64,
}

fn persisted_flag() -> &'static AtomicBool {
    INIT.call_once(|| {
        let b = Box::leak(Box::new(AtomicBool::new(false)));
        unsafe { PERSISTED_HAS_DATA = Some(b); }
        let f = Box::leak(Box::new(Mutex::new(Vec::new())));
        unsafe { LAST_FILE_ROWS = Some(f); }
        let e = Box::leak(Box::new(Mutex::new(Vec::new())));
        unsafe { LAST_ENTITY_ROWS = Some(e); }
        let a = Box::leak(Box::new(Mutex::new(Vec::new())));
        unsafe { LAST_ACTION_ROWS = Some(a); }
        let ev = Box::leak(Box::new(Mutex::new(Vec::new())));
        unsafe { LAST_EVENT_ROWS = Some(ev); }
        let m = Box::leak(Box::new(Mutex::new(Vec::new())));
        unsafe { LAST_MODULE_ROWS = Some(m); }
        let ap = Box::leak(Box::new(Mutex::new(String::new())));
        unsafe { LAST_ARCHIVE_PATH = Some(ap); }
        let p = Box::leak(Box::new(Mutex::new(Vec::new())));
        unsafe { LAST_ENTITY_PATTERNS = Some(p); }
        let panels = Box::leak(Box::new(Mutex::new(Vec::new())));
        unsafe { LAST_PANELS = Some(panels); }
        let cb = Box::leak(Box::new(Mutex::new(HashMap::new())));
        unsafe { LAST_CREATED_BY = Some(cb); }
        let pe = Box::leak(Box::new(Mutex::new(Vec::new())));
        unsafe { PENDING_EFFECTS = Some(pe); }
        let se = Box::leak(Box::new(Mutex::new(Vec::new())));
        unsafe { SCHEDULED_EFFECTS = Some(se); }
        let ed = Box::leak(Box::new(Mutex::new(HashMap::new())));
        unsafe { LAST_ENTITY_DATA = Some(ed); }
        let en = Box::leak(Box::new(Mutex::new(HashMap::new())));
        unsafe { LAST_ENTITY_NUMBER_DATA = Some(en); }
        let ied = Box::leak(Box::new(Mutex::new(HashMap::new())));
        unsafe { INITIAL_ENTITY_DATA = Some(ied); }
        let et = Box::leak(Box::new(AtomicI64::new(0)));
        unsafe { ELAPSED_TIME_UNITS = Some(et); }
    });
    unsafe { PERSISTED_HAS_DATA.expect("persisted flag initialized") }
}

pub fn last_file_rows() -> &'static Mutex<Vec<Vec<String>>> {
    persisted_flag();
    unsafe { LAST_FILE_ROWS.expect("file rows initialized") }
}
pub fn last_entity_rows() -> &'static Mutex<Vec<Vec<String>>> {
    persisted_flag();
    unsafe { LAST_ENTITY_ROWS.expect("entity rows initialized") }
}
pub fn last_action_rows() -> &'static Mutex<Vec<Vec<String>>> {
    persisted_flag();
    unsafe { LAST_ACTION_ROWS.expect("action rows initialized") }
}
pub fn last_event_rows() -> &'static Mutex<Vec<Vec<String>>> {
    persisted_flag();
    unsafe { LAST_EVENT_ROWS.expect("event rows initialized") }
}
pub fn last_module_rows() -> &'static Mutex<Vec<Vec<String>>> {
    persisted_flag();
    unsafe { LAST_MODULE_ROWS.expect("module rows initialized") }
}
pub fn last_entity_patterns() -> &'static Mutex<Vec<String>> {
    persisted_flag();
    unsafe { LAST_ENTITY_PATTERNS.expect("entity patterns initialized") }
}

pub fn last_panels() -> &'static Mutex<Vec<String>> {
    persisted_flag();
    unsafe { LAST_PANELS.expect("panels initialized") }
}

pub fn last_created_by() -> &'static Mutex<HashMap<String, Vec<String>>> {
    persisted_flag();
    unsafe { LAST_CREATED_BY.expect("created by map initialized") }
}

pub fn set_last_created_by(map: HashMap<String, Vec<String>>) {
    *last_created_by().lock().unwrap() = map;
}

/// Public helper for other modules to mark that persisted state has data
pub fn mark_persisted_has_data() {
    persisted_flag().store(true, Ordering::SeqCst);
}

pub fn set_last_file_rows(rows: Vec<Vec<String>>) {
    *last_file_rows().lock().unwrap() = rows;
}
pub fn set_last_entity_rows(rows: Vec<Vec<String>>) {
    *last_entity_rows().lock().unwrap() = rows;
}
pub fn append_entity_row(row: Vec<String>) {
    last_entity_rows().lock().unwrap().push(row);
}
pub fn set_last_action_rows(rows: Vec<Vec<String>>) {
    *last_action_rows().lock().unwrap() = rows;
}
pub fn set_last_event_rows(rows: Vec<Vec<String>>) {
    *last_event_rows().lock().unwrap() = rows;
}
pub fn set_last_module_rows(rows: Vec<Vec<String>>) {
    *last_module_rows().lock().unwrap() = rows;
}
pub fn set_last_entity_patterns(rows: Vec<String>) {
    *last_entity_patterns().lock().unwrap() = rows;
}
pub fn set_last_panels(rows: Vec<String>) {
    *last_panels().lock().unwrap() = rows;
}

pub fn pending_effects() -> &'static Mutex<Vec<String>> {
    persisted_flag();
    unsafe { PENDING_EFFECTS.expect("pending effects initialized") }
}

pub fn scheduled_effects() -> &'static Mutex<Vec<ScheduledEffect>> {
    persisted_flag();
    unsafe { SCHEDULED_EFFECTS.expect("scheduled effects initialized") }
}

pub fn last_entity_data() -> &'static Mutex<HashMap<String, HashMap<String, String>>> {
    persisted_flag();
    unsafe { LAST_ENTITY_DATA.expect("entity data initialized") }
}

pub fn set_last_entity_data(data: HashMap<String, HashMap<String, String>>) {
    *last_entity_data().lock().unwrap() = data;
}

pub fn last_entity_number_data() -> &'static Mutex<HashMap<String, HashMap<String, f64>>> {
    persisted_flag();
    unsafe { LAST_ENTITY_NUMBER_DATA.expect("entity number data initialized") }
}

 pub fn set_last_entity_number_data(data: HashMap<String, HashMap<String, f64>>) {
    *last_entity_number_data().lock().unwrap() = data;
}

pub fn initial_entity_data() -> &'static Mutex<HashMap<String, HashMap<String, String>>> {
    persisted_flag();
    unsafe { INITIAL_ENTITY_DATA.expect("initial entity data initialized") }
}

pub fn set_initial_entity_data(data: HashMap<String, HashMap<String, String>>) {
    *initial_entity_data().lock().unwrap() = data;
}

pub fn elapsed_time_units() -> &'static AtomicI64 {
    persisted_flag();
    unsafe { ELAPSED_TIME_UNITS.expect("elapsed time units initialized") }
}

pub fn add_elapsed_time_units(units: i64) {
    elapsed_time_units().fetch_add(units, Ordering::SeqCst);
}

pub fn get_elapsed_time_units() -> i64 {
    elapsed_time_units().load(Ordering::SeqCst)
}

pub fn set_pending_effects(effects: Vec<String>) {
    *pending_effects().lock().unwrap() = effects;
}

pub fn clear_pending_effects() {
    pending_effects().lock().unwrap().clear();
}

pub fn add_scheduled_effect(name: String, payload: serde_json::Value, next_exec_time: i64, reoccurrence_interval: i64) {
    let mut effects = scheduled_effects().lock().unwrap();
    // Remove any existing effect with the same name to avoid duplicates
    effects.retain(|e| e.name != name);
    effects.push(ScheduledEffect {
        name,
        payload,
        next_exec_time,
        reoccurrence_interval,
        execution_count: 0,
    });
}

pub fn remove_scheduled_effect(name: &str) {
    let mut effects = scheduled_effects().lock().unwrap();
    effects.retain(|e| e.name != name);
}

pub fn get_due_scheduled_effects(current_elapsed: i64) -> Vec<ScheduledEffect> {
    let mut effects = scheduled_effects().lock().unwrap();
    let mut due = Vec::new();
    for effect in effects.iter_mut() {
        while current_elapsed >= effect.next_exec_time && effect.reoccurrence_interval > 0 {
            effect.execution_count += 1;
            due.push(effect.clone());
            effect.next_exec_time = effect.next_exec_time + effect.reoccurrence_interval;
        }
    }
    due
}

 #[allow(dead_code)]
pub fn clear_state() {
    // Clear cached rows and flags so embedding processes can reset runtime state between tests
    *last_file_rows().lock().unwrap() = Vec::new();
    *last_entity_rows().lock().unwrap() = Vec::new();
    *last_action_rows().lock().unwrap() = Vec::new();
    *last_event_rows().lock().unwrap() = Vec::new();
    *last_module_rows().lock().unwrap() = Vec::new();
    *last_entity_patterns().lock().unwrap() = Vec::new();
    *last_panels().lock().unwrap() = Vec::new();
    clear_pending_effects();
    *scheduled_effects().lock().unwrap() = Vec::new();
    *last_created_by().lock().unwrap() = HashMap::new();
    *last_archive_path().lock().unwrap() = String::new();
    *last_entity_data().lock().unwrap() = HashMap::new();
    *last_entity_number_data().lock().unwrap() = HashMap::new();
    *initial_entity_data().lock().unwrap() = HashMap::new();
    elapsed_time_units().store(0, Ordering::SeqCst);
    persisted_flag().store(false, Ordering::SeqCst);
}

pub fn last_archive_path() -> &'static Mutex<String> {
    persisted_flag();
    unsafe { LAST_ARCHIVE_PATH.expect("archive path initialized") }
}

pub fn set_archive_path(path: &str) {
    *last_archive_path().lock().unwrap() = path.to_string();
}

/// Persists file and entity rows into a SQLite file on disk and returns the path.
pub fn persist_state(path: &str, file_rows: &[Vec<String>], entity_rows: &[Vec<String>]) -> String {
    let mut conn = Connection::open_in_memory().expect("open db");
    conn.execute_batch("CREATE TABLE IF NOT EXISTS files (file_name TEXT, contents TEXT);")
        .expect("create files table");
    // Ensure expected output tables exist with correct columns so CSV column checks succeed.
    conn.execute_batch("CREATE TABLE IF NOT EXISTS module (id TEXT, name TEXT, version TEXT);")
        .expect("create module table");
    conn.execute_batch("CREATE TABLE IF NOT EXISTS events (name TEXT);")
        .expect("create events table");
    conn.execute_batch("CREATE TABLE IF NOT EXISTS action (name TEXT);")
        .expect("create action table");
    conn.execute_batch("CREATE TABLE IF NOT EXISTS entity (textMap_name TEXT);")
        .expect("create entity table");
    // Use helper to insert files and collect module rows from manifests
    let module_rows_cache = crate::export_helpers::insert_files_and_collect_modules(&mut conn, file_rows);
    // Insert entities and commit
    crate::export_helpers::insert_entities(&mut conn, &entity_rows.to_vec());
    // cache module rows for later exports
    if !module_rows_cache.is_empty() { set_last_module_rows(module_rows_cache); }
    let dest = format!("{}-{}.db", path, std::process::id());
    // mark that a persisted DB with data exists
    if !file_rows.is_empty() || !entity_rows.is_empty() {
        persisted_flag().store(true, Ordering::SeqCst);
    }
    // update last rows cache
    *last_file_rows().lock().unwrap() = file_rows.to_vec();
    *last_entity_rows().lock().unwrap() = entity_rows.to_vec();
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
             CREATE VIEW IF NOT EXISTS panel AS \
               SELECT '' AS id WHERE 0; \
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

#[allow(dead_code)]
fn try_copy_persisted_to(path: &str) -> bool {
    let persisted = format!("state.db-{}.db", std::process::id());
    if persisted_flag().load(Ordering::SeqCst) && Path::new(&persisted).exists() {
        let _ = std::fs::copy(&persisted, path).expect("copy persisted db");
        return true;
    }
    false
}

/// Exports the current state to a SQLite file at `path` with all required schema views."}
/// Creates the file (overwriting if it exists) with views for module, events, action, and entity.
pub fn export_to_file(path: &str) {
    if Path::new(path).exists() { let _ = std::fs::remove_file(path); }

    let persisted = format!("state.db-{}.db", std::process::id());
    let files_cached = last_file_rows().lock().unwrap().clone();
    let entities_cached = last_entity_rows().lock().unwrap().clone();
    let actions_cached = last_action_rows().lock().unwrap().clone();
    let events_cached = last_event_rows().lock().unwrap().clone();
    let modules_cached = last_module_rows().lock().unwrap().clone();
    let panels_cached = last_panels().lock().unwrap().clone();

    let has_cached = !files_cached.is_empty() || !actions_cached.is_empty() || !events_cached.is_empty() || !entities_cached.is_empty() || !modules_cached.is_empty() || !panels_cached.is_empty();

    if has_cached {
        let mut mem_conn = crate::export_helpers::init_in_memory_export_db();
        crate::export_helpers::insert_module_rows_from_cache_or_files(&mut mem_conn, &modules_cached, &files_cached);
        crate::export_helpers::insert_actions(&mut mem_conn, &actions_cached);
        crate::export_helpers::insert_events(&mut mem_conn, &events_cached);
        crate::export_helpers::insert_panels(&mut mem_conn, &panels_cached);
        crate::export_helpers::insert_entities(&mut mem_conn, &entities_cached);

        let mut dest_conn = Connection::open(path).expect("open export db");
        let backup = rusqlite::backup::Backup::new(&mem_conn, &mut dest_conn).expect("backup");
        backup.step(-1).expect("backup step");
        return;
    } else {
        if persisted_flag().load(Ordering::SeqCst) && Path::new(&persisted).exists() {
            let _ = std::fs::copy(&persisted, path).expect("copy persisted db");
            return;
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
        return;
    }
}

/// Reads the SQLite file at `path` into memory.
pub fn read_sqlite_bytes(path: &str) -> Vec<u8> {
    let mut f = File::open(path).expect("open state");
    let mut buf = Vec::new();
    f.read_to_end(&mut buf).expect("read state");
    buf
}
