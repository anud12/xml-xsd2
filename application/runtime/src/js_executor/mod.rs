use anyhow::Result;
use crate::js_host_api::Declarations;
use std::collections::HashMap;

mod clean;

/// Patch user JS source to remove `string`, `number` from destructuring params only.
fn patch_user_source(source: &str) -> String {
    let result = source
        .replace("({string, number, ...hostApi})", "({...hostApi})")
        .replace("({ string, number, ...hostApi })", "({...hostApi})");

    let result2 = result.replace("({string, ...hostApi})", "({...hostApi})")
                        .replace("({number, ...hostApi})", "({...hostApi})");

    result2
}

/// Extract declarations from user source by running it in a unified QuickJS context.
pub fn extract_from_source(source: &str) -> Result<Declarations> {
    let patched = patch_user_source(source);
    clean::extract_from_source(&patched)
}

/// Simulate a single action within the archive context.
pub fn simulate_action(
    files: &HashMap<String, String>,
    action_name: &str,
    initial_store: &[Vec<String>],
) -> Result<(Vec<String>, Vec<Vec<String>>)> {
    clean::simulate_action(files, action_name, initial_store)
}

/// Process any pending effects queued from previous action simulations.
pub fn process_pending_effects(files: &HashMap<String, String>) -> Result<()> {
    clean::process_pending_effects(files)
}
