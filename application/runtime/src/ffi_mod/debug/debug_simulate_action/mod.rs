use std::ffi::CStr;
use std::io::Write;
use libc::c_char;

mod files_map;
mod fallback;

#[export_name = "runtime_debug_simulate_action"]
pub extern "C" fn runtime_debug_simulate_action(
    action_name: *const c_char,
) -> bool {
    runtime_debug_simulate_action_for(action_name, std::ptr::null())
}

/// Dispatch an action, optionally bound to an actor (entity id). While the
/// actor has a parked action plan, every further action for it is rejected:
/// the plan is neither interrupted nor queued behind the new action.
#[export_name = "runtime_debug_simulate_action_for"]
pub extern "C" fn runtime_debug_simulate_action_for(
    action_name: *const c_char,
    actor: *const c_char,
) -> bool {
    runtime_log!("DEBUG: runtime_debug_simulate_action_for invoked");
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
    let actor = if actor.is_null() {
        String::new()
    } else {
        unsafe { CStr::from_ptr(actor) }.to_str().unwrap_or("").trim().to_string()
    };
    runtime_log!("DEBUG: simulating action: {} (actor: {:?})", name, actor);

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

    if !actor.is_empty() {
        // A busy actor is rejected only while its plan is non-interruptible.
        // An interruptible plan is dropped and replaced by this action.
        if crate::state::actor_is_busy(&actor)
            && !crate::state::actor_plan_interruptible(&actor)
        {
            runtime_log!(
                "DEBUG: action '{}' dropped: actor '{}' has a non-interruptible plan (busy)",
                name, actor);
            return false;
        }
    } else if crate::state::has_active_plan(name) {
        runtime_log!(
            "DEBUG: action '{}' rejected: plan already active (actor busy)", name);
        return false;
    }

    let frc = crate::state::last_file_rows().lock().unwrap().len();
    if let Ok(mut f) = std::fs::OpenOptions::new()
        .create(true).append(true).open("C:\\temp\\rust_debug.log")
    {
        let _ = writeln!(f, "[{}] simulate_action: action={}, actor={:?}, file_rows={}",
            std::process::id(), name, actor, frc);
    }

    let files = files_map::build_files_map();
    let current = crate::state::last_entity_rows()
        .lock().unwrap().clone();

    match crate::js_executor::simulate_action(&files, name, &actor, &current) {
        Ok((created, store)) => {
            fallback::handle_success(name, created, store, &current)
        }
        Err(_) => {
            runtime_log!("DEBUG: simulate_action failed, using fallback");
            fallback::handle_failure(name)
        }
    }
}
