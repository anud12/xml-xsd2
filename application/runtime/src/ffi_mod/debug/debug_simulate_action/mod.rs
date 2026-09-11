use std::ffi::CStr;
use std::io::Write;
use libc::c_char;

mod files_map;
mod fallback;

#[export_name = "runtime_debug_simulate_action"]
pub extern "C" fn runtime_debug_simulate_action(
    action_name: *const c_char,
) -> bool {
    runtime_log!("DEBUG: runtime_debug_simulate_action invoked");
    if action_name.is_null() {
        runtime_log!("DEBUG: action_name is null");
        return false;
    }
    let c_str = unsafe { CStr::from_ptr(action_name) };
    let name = match c_str.to_str() {
        Ok(s) => s.trim(),
        Err(_) => {
            runtime_log!("DEBUG: failed to convert action_name");
            return false;
        }
    };
    runtime_log!("DEBUG: simulating action: {}", name);

    let actions = crate::state::last_action_rows()
        .lock().unwrap().clone();
    runtime_log!("DEBUG: checking {} cached action rows", actions.len());
    let matched = actions.iter().any(|row| {
        row.get(0).map(|s| s.as_str()) == Some(name)
    });
    if matched {
        runtime_log!("DEBUG: action '{}' found in cached rows", name);
    } else {
        runtime_log!("DEBUG: action '{}' NOT found in cached rows", name);
        return false;
    }

    let frc = crate::state::last_file_rows().lock().unwrap().len();
    if let Ok(mut f) = std::fs::OpenOptions::new()
        .create(true).append(true).open("C:\\temp\\rust_debug.log")
    {
        let _ = writeln!(f, "[{}] simulate_action: action={}, file_rows={}",
            std::process::id(), name, frc);
    }

    let files = files_map::build_files_map();
    let current = crate::state::last_entity_rows()
        .lock().unwrap().clone();
    dispatch_in_sim_or_fallback(name, "", &files, &current)
}

/// Shared tail of the action dispatch: the sim-context path (actor-bound, so
/// the resulting plan keys on the actor for busy/interruptible state) or the
/// fresh-context `simulate_action` fallback. `files`/`current` are only read on
/// the fallback path.
fn dispatch_in_sim_or_fallback(
    name: &str,
    actor: &str,
    files: &std::collections::HashMap<String, String>,
    current: &Vec<Vec<String>>,
) -> bool {
    if crate::js_executor::sim_ctx::ctx().is_some() {
        if let Err(e) = crate::js_executor::dispatch_action_in_sim_for(name, actor) {
            runtime_log!("DEBUG: dispatch_action_in_sim failed: {:?}", e);
        }
        return true;
    }

    match crate::js_executor::simulate_action(files, name, current) {
        Ok((created, store)) => {
            fallback::handle_success(name, created, store, current)
        }
        Err(_) => {
            runtime_log!("DEBUG: simulate_action failed, using fallback");
            fallback::handle_failure(name)
        }
    }
}

/// Dispatch an action by name from the persistent sim context (the `.ui`
/// click path emits actions with a key/value args payload). Mirrors
/// `runtime_debug_simulate_action` but is reachable from the FFI misc layer;
/// the args are carried for handler `ctx.args` lookups.
pub fn runtime_debug_simulate_action_args(
    action_name: *const c_char,
    args: &[(String, f64)],
) {
    publish_action_args(args);
    let _ = runtime_debug_simulate_action(action_name);
}

/// Dispatch an action with key/value args AND a bound actor (the C#
/// `emitActionFor` path). The actor is threaded to the sim dispatch so the
/// resulting plan keys on it for busy/interruptible state.
pub fn runtime_debug_simulate_action_args_for(
    action_name: *const c_char,
    args: &[(String, f64)],
    actor: &str,
) {
    publish_action_args(args);
    if action_name.is_null() { return; }
    let c_str = unsafe { CStr::from_ptr(action_name) };
    let name = match c_str.to_str() {
        Ok(s) => s.trim(),
        Err(_) => return,
    };
    let actions = crate::state::last_action_rows()
        .lock().unwrap().clone();
    let matched = actions.iter().any(|row| {
        row.get(0).map(|s| s.as_str()) == Some(name)
    });
    if !matched { return; }
    let files = files_map::build_files_map();
    let current = crate::state::last_entity_rows()
        .lock().unwrap().clone();
    dispatch_in_sim_or_fallback(name, actor, &files, &current);
}

fn publish_action_args(args: &[(String, f64)]) {
    // Publish the click/cursor args into the persistent sim context so the
    // action's `ctx.args` sees them (the dispatch reads `__actionArgs`).
    if let Some(ctx) = crate::js_executor::sim_ctx::ctx() {
        let mut obj = String::from("{");
        for (i, (k, v)) in args.iter().enumerate() {
            if i > 0 { obj.push(','); }
            let key = serde_json::to_string(k).unwrap_or_else(|_| "\"\"".to_string());
            obj.push_str(&format!("{}:{}", key, v));
        }
        obj.push('}');
        let _ = crate::js_executor::sim_ctx::sim_with(ctx, |c| c.eval::<(), _>(format!(
            "globalThis.__actionArgs = {};", obj
        )));
    }
}
