#![allow(dead_code)]
use std::collections::HashMap;
use crate::js_executor::extract_from_source;
use crate::js_host_api::Declarations;

pub mod manifest;
pub mod declarations;

/// Converts the zip file map into row tuples for SQLite insertion.
pub fn build_file_rows(files: &HashMap<String, String>) -> Vec<Vec<String>> {
    files.iter().map(|(n, c)| vec![n.clone(), c.clone()]).collect()
}

/// Processes the loaded archive: extracts entities/events from the JS module entry point.
/// Returns entity rows to be persisted.
pub fn process_module(files: &HashMap<String, String>, _entity_rows: &mut Vec<Vec<String>>) {
    if let Some((manifest_name, manifest_json)) = manifest::find_manifest(files) {
        runtime_log!("module process: found manifest {}", manifest_name);
        manifest::set_module_rows(&manifest_json);
        handle_entry_point(&manifest_json, files);
    } else {
        runtime_log!("manifest.json not found");
        for (k, _) in files.iter() { runtime_log!("file present: {}", k); }
        runtime_log!("module rejected");
    }
}

fn handle_entry_point(manifest_json: &serde_json::Value, files: &HashMap<String, String>) {
    let entry_name = manifest::get_entry_name(manifest_json);
    if let Some(module_src) = files.get(&entry_name) {
        runtime_log!("{} loaded", entry_name);
        manifest::mark_manifest_event(manifest_json);
        process_module_source(module_src);
    } else {
        runtime_log!("Error: entrypoint \"{}\" not found in archive", entry_name);
    }
}

fn process_module_source(module_src: &str) {
    match extract_from_source(module_src) {
        Ok(dec) => declarations::apply_declarations(&dec),
        Err(_) => eprintln!("js extraction failed; no fallback heuristics are used"),
    }
}

#[allow(dead_code)]
fn collect_patterns(dec: &Declarations) -> Vec<String> {
    declarations::collect_patterns(dec)
}

#[allow(dead_code)]
fn build_action_to_created(dec: &Declarations) -> HashMap<String, Vec<String>> {
    declarations::build_action_to_created(dec)
}