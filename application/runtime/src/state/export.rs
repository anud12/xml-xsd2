use std::path::Path;
use rusqlite::Connection;

pub fn export_to_file(path: &str) {
    if Path::new(path).exists() { let _ = std::fs::remove_file(path); }
    let files = super::last_file_rows().lock().unwrap().clone();
    let entities = super::last_entity_rows().lock().unwrap().clone();
    let actions = super::last_action_rows().lock().unwrap().clone();
    let events = super::last_event_rows().lock().unwrap().clone();
    let modules = super::last_module_rows().lock().unwrap().clone();
    let mut panels = super::last_panels().lock().unwrap().clone();
    collect_panels_fallback(&mut panels);
    let has = !files.is_empty() || !actions.is_empty() || !events.is_empty()
        || !entities.is_empty() || !modules.is_empty() || !panels.is_empty();
    if has {
        let mut mem = crate::export_helpers::init_in_memory_export_db();
        crate::export_helpers::insert_module_rows_from_cache_or_files(&mut mem, &modules, &files);
        crate::export_helpers::insert_actions(&mut mem, &actions);
        crate::export_helpers::insert_events(&mut mem, &events);
        crate::export_helpers::insert_panels(&mut mem, &panels);
        crate::export_helpers::insert_entities(&mut mem, &entities);
        let mut dest = Connection::open(path).expect("open export db");
        let backup = rusqlite::backup::Backup::new(&mem, &mut dest).expect("backup");
        backup.step(-1).expect("backup step");
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
    ).expect("init export db");
}

fn collect_panels_fallback(panels: &mut Vec<String>) {
    if !panels.is_empty() { return; }
    for row in super::last_file_rows().lock().unwrap().iter() {
        if row.len() < 2 { continue; }
        let fname = row[0].to_lowercase();
        if !(fname.contains("panel") && fname.contains(".csv")) { continue; }
        for line in row[1].lines() {
            let t = line.trim();
            if t.is_empty() { continue; }
            let f = t.split(',').next().unwrap().trim_matches('"');
            if f.eq_ignore_ascii_case("id") || f.is_empty() { continue; }
            panels.push(f.to_string());
        }
    }
}
