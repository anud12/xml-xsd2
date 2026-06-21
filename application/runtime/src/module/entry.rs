use std::collections::HashMap;
use crate::js_executor::extract_from_source;
use crate::js_host_api::Declarations;

pub(super) fn handle_entry_point(
    manifest_name: &str,
    manifest_json: &serde_json::Value,
    files: &HashMap<String, String>,
) {
    let entry_name = super::manifest::get_entry_name(manifest_json);
    if let Some(module_src) = files.get(&entry_name).or_else(|| {
        if let Some(pos) = manifest_name.rfind('/') {
            let dir = &manifest_name[..pos];
            let candidate = format!("{}/{}", dir, entry_name);
            files.get(&candidate)
        } else {
            None
        }
    }) {
        runtime_log!("{} loaded", entry_name);
        super::manifest::mark_manifest_event(manifest_json);
        process_module_source(module_src);
    } else {
        runtime_log!("Error: entrypoint \"{}\" \
            not found in archive", entry_name);
    }
}

fn process_module_source(module_src: &str) {
    match extract_from_source(module_src) {
        Ok(dec) => super::declarations::apply_declarations(&dec),
        Err(_) => eprintln!(
            "js extraction failed; \
            no fallback heuristics are used"),
    }
}

#[allow(dead_code)]
fn collect_patterns(dec: &Declarations) -> Vec<String> {
    super::declarations::collect_patterns(dec)
}

#[allow(dead_code)]
fn build_action_to_created(
    dec: &Declarations,
) -> HashMap<String, Vec<String>> {
    super::declarations::build_action_to_created(dec)
}
