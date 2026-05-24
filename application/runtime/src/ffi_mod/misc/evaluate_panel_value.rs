use std::ffi::{CStr, CString};
use libc::c_char;

/// Evaluate a panel value expression against an entity.
/// Takes entity_id + ast_root_id (pointing to NumberEntityRef node) and returns the display string.
/// Returns fallback if the entity/key lookup fails.
#[no_mangle]
pub extern "C" fn ffi_evaluate_panel_value(
    entity_id: *const c_char,
    ast_root_id: u64,
) -> *mut c_char {
    if entity_id.is_null() {
        return CString::new("").unwrap().into_raw();
    }
    let entity_id_str = unsafe { CStr::from_ptr(entity_id) }.to_string_lossy().to_string();

    let registry = crate::state::compiled_ast_nodes().lock().unwrap();

    // Resolve key from NumberEntityRef node
    let key_str = if let Some(node) = registry.get(&ast_root_id) {
        if node.get("type").map(|v| v.as_str() == Some("NumberEntityRef")).unwrap_or(false) {
            let key_id = node.get("key").and_then(|v| v.as_u64()).unwrap_or(0);
            resolve_string_literal(key_id, &registry)
        } else {
            String::new()
        }
    } else {
        String::new()
    };

    // Resolve fallback from OrElseNumberRef node (search registry for expr=ast_root_id)
    let fallback_str = registry.iter()
        .find(|(_, v)| v.get("type").map(|t| t.as_str() == Some("OrElseNumberRef")).unwrap_or(false)
                     && v.get("expr").map(|e| e.as_u64() == Some(ast_root_id)).unwrap_or(false))
        .and_then(|(_, v)| v.get("fallback").and_then(|f| f.as_u64()))
        .map(|fid| resolve_string_literal(fid, &registry))
        .unwrap_or_default();

    // Evaluate: look up entity number map
    let data = crate::state::last_entity_number_data().lock().unwrap();
    let value = data
        .get(&entity_id_str)
        .and_then(|nm| nm.get(&key_str))
        .map(|n| n.to_string());

    let result = value.unwrap_or_else(|| fallback_str.clone());

    CString::new(result).unwrap_or_else(|_| CString::new("").unwrap()).into_raw()
}

fn resolve_string_literal(id: u64, registry: &std::collections::HashMap<u64, serde_json::Value>) -> String {
    if let Some(node) = registry.get(&id) {
        if node.get("type").map(|v| v.as_str() == Some("StringLiteral")).unwrap_or(false) {
            return node.get("value").and_then(|v| v.as_str()).unwrap_or("").to_string();
        }
    }
    String::new()
}
