use std::collections::HashMap;

/// Locates the manifest JSON in the archive files and parses it.
pub fn find_manifest(files: &HashMap<String, String>) -> Option<(String, serde_json::Value)> {
    files
        .iter()
        .find(|(k, _)| {
            let k_lower = k.to_lowercase();
            k_lower.contains("manifest") && k_lower.contains(".json")
        })
        .and_then(|(k, s)| serde_json::from_str::<serde_json::Value>(s).ok().map(|v| (k.clone(), v)))
}

pub fn set_module_rows(manifest_json: &serde_json::Value) {
    if let Some(id_v) = manifest_json.get("id").and_then(|v| v.as_str()) {
        let name_v = manifest_json.get("name").and_then(|v| v.as_str()).unwrap_or("");
        let version_v = manifest_json.get("version").and_then(|v| v.as_str()).unwrap_or("");
        crate::state::set_last_module_rows(vec![vec![id_v.to_string(), name_v.to_string(), version_v.to_string()]]);
    }
}

pub fn get_entry_name(manifest_json: &serde_json::Value) -> String {
    manifest_json
        .get("entry")
        .and_then(|v| v.as_str())
        .unwrap_or("index.js")
        .to_string()
}

pub fn mark_manifest_event(manifest_json: &serde_json::Value) {
    if let Some(evt_name) = manifest_json.get("eventName").and_then(|v| v.as_str()) {
        runtime_log!("event: {}", evt_name);
        runtime_log!("event registered: {}", evt_name);
        runtime_log!("module process: manifest has eventName, marking persisted_has_data");
        crate::state::mark_persisted_has_data();
    }
}