use std::ffi::{CStr, CString};
use std::ptr;
use libc::c_char;

type PanelFfi = crate::ffi_mod::types::PanelFfi;

use super::panel_helpers::{
    debug_append, build_fallback_panel, build_empty_panel,
};
use super::json_extract::lookup_panel;
use super::parse_fields::parse_fields;
use super::serde_build::build_panel_from_parsed;

// Return pointer to PanelFfi struct.
// Caller must free with runtime_free_panel.
#[no_mangle]
pub extern "C" fn get_panel_by_id_struct(
    id: *const c_char
) -> *mut PanelFfi {
    let _ = std::fs::write(
        "rust_function_called.txt",
        "get_panel_by_id_struct called\n",
    );
    if id.is_null() { return ptr::null_mut(); }
    let id_str = unsafe {
        CStr::from_ptr(id).to_string_lossy().to_string()
    };

    let panels = crate::state::last_panels()
        .lock().unwrap().clone();
    debug_append(&format!(
        "Looking for id='{}' in {} panels\n",
        id_str, panels.len()
    ));
    for (i, p) in panels.iter().enumerate() {
        debug_append(&format!(
            "  Panel {}: {} bytes\n", i, p.len()
        ));
    }

    if let Some((panel_json, bg_opt)) =
        lookup_panel(&panels, &id_str)
    {
        let bg_ptr = if let Some(ref bg) = bg_opt {
            CString::new(bg.clone())
                .unwrap_or_else(|_| CString::new("").unwrap())
                .into_raw()
        } else {
            ptr::null_mut()
        };
        let id_c = CString::new(id_str.clone())
            .unwrap_or_else(|_| CString::new("").unwrap());
        let id_ptr = id_c.into_raw();

        let result = serde_json::from_str::<
            super::JsPanel
        >(&panel_json);
        if let Ok(parsed) = result {
            let fields = parse_fields(&parsed);
            eprintln!("MARKER_SERDE_OK returned ptr");
            return build_panel_from_parsed(
                id_ptr, bg_ptr, &panel_json, fields
            );
        }

        let panel_str = panel_json.as_str();
        eprintln!("MARKER_SERDE_FAIL returned ptr");
        return build_fallback_panel(
            id_ptr, bg_ptr, panel_str
        );
    }

    eprintln!("MARKER_NOT_FOUND returned ptr");
    build_empty_panel(&id_str)
}
