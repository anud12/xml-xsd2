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

    match crate::js_executor::simulate_action(&files, name, &current) {
        Ok((created, store)) => {
            fallback::handle_success(name, created, store, &current)
        }
        Err(_) => {
            runtime_log!("DEBUG: simulate_action failed, using fallback");
            fallback::handle_failure(name)
        }
    }
}
