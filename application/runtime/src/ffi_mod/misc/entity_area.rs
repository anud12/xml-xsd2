use std::ffi::CString;
use libc::c_char;

/// The ids of the entities inside one *named* area of the entity, serialized
/// as a JSON array of strings (id ascending, self excluded, deduped across
/// containers). The area name is required: the named area is resolved first,
/// then the containment/overlap test runs against it. An entity with no such
/// area — or that is not a member of any container — yields `"[]"`. The caller
/// frees the returned string with [`runtime_free_string`].
#[no_mangle]
pub extern "C" fn runtime_get_entities_inside_area(
    id: *const c_char,
    area_id: *const c_char,
) -> *mut c_char {
    if id.is_null() || area_id.is_null() {
        return CString::new("[]").unwrap().into_raw();
    }
    let id_str = unsafe { std::ffi::CStr::from_ptr(id).to_string_lossy().to_string() };
    let area_str = unsafe { std::ffi::CStr::from_ptr(area_id).to_string_lossy().to_string() };
    let containers = crate::state::last_containers().lock().unwrap().clone();
    let ids = crate::state::entities_inside_area(&id_str, &area_str, &containers);
    let json = serde_json::to_string(&ids).unwrap_or_else(|_| "[]".into());
    CString::new(json).unwrap_or_else(|_| CString::new("[]").unwrap()).into_raw()
}

/// The ids of the containers the named entity is a member of, as a JSON array
/// of strings. Diagnostics only. Free with [`runtime_free_string`].
#[no_mangle]
pub extern "C" fn runtime_get_entity_containers(
    id: *const c_char,
) -> *mut c_char {
    if id.is_null() {
        return CString::new("[]").unwrap().into_raw();
    }
    let id_str = unsafe {
        std::ffi::CStr::from_ptr(id).to_string_lossy().to_string()
    };
    let containers = crate::state::last_containers().lock().unwrap().clone();
    let ids = crate::state::entity_containers(&id_str, &containers);
    let json = serde_json::to_string(&ids).unwrap_or_else(|_| "[]".into());
    CString::new(json).unwrap_or_else(|_| CString::new("[]").unwrap()).into_raw()
}

/// Free a string returned by any `runtime_get_*` string query (including
/// `runtime_get_entities_inside_area`).
#[no_mangle]
pub extern "C" fn runtime_free_inside_area(p: *mut c_char) {
    if !p.is_null() {
        unsafe {
            let _ = CString::from_raw(p);
        }
    }
}

/// Register (or clear) one *named* area of the entity's area entityMap from its
/// serialized vertices. The area name is required. The vertices arrive as a
/// JSON array of `[x, y]` pairs in entity-local units. An empty array clears
/// the area. Idempotent and order-independent: the geometry is recomputed from
/// the raw polygon.
#[no_mangle]
pub extern "C" fn runtime_set_entity_area(
    id: *const c_char,
    area_id: *const c_char,
    polygon_json: *const c_char,
) {
    if id.is_null() || area_id.is_null() || polygon_json.is_null() {
        return;
    }
    let id_str = unsafe { std::ffi::CStr::from_ptr(id).to_string_lossy().to_string() };
    let area_str = unsafe { std::ffi::CStr::from_ptr(area_id).to_string_lossy().to_string() };
    let poly = parse_poly_json(polygon_json);
    crate::state::set_entity_area(&id_str, &area_str, poly);
}

/// Parse a JSON array of `[x, y]` pairs into local points (empty on error).
fn parse_poly_json(polygon_json: *const c_char) -> Vec<(f64, f64)> {
    let poly_json = unsafe { std::ffi::CStr::from_ptr(polygon_json).to_string_lossy().to_string() };
    let mut poly: Vec<(f64, f64)> = Vec::new();
    if let Ok(arr) = serde_json::from_str::<Vec<serde_json::Value>>(&poly_json) {
        for v in arr.iter() {
            if let Some(pair) = v.as_array() {
                if pair.len() >= 2 {
                    let x = pair[0].as_f64().unwrap_or(0.0);
                    let y = pair[1].as_f64().unwrap_or(0.0);
                    poly.push((x, y));
                }
            }
        }
    }
    poly
}

#[cfg(test)]
mod tests {
    use super::*;
    use crate::state::test_lock;
    use std::ffi::{CStr, CString};

    fn c(s: &str) -> CString {
        CString::new(s).unwrap()
    }

    /// A container row in the exact shape `setContainer` serializes: id,
    /// entities, and pre-baked getX/getY per member.
    fn container(id: &str, members: &[(&str, f64, f64)]) -> String {
        let mut c = serde_json::Map::new();
        c.insert("id".into(), serde_json::json!(id));
        c.insert(
            "entities".into(),
            serde_json::json!(members.iter().map(|(e, _, _)| *e).collect::<Vec<_>>()),
        );
        let mut gx = serde_json::Map::new();
        let mut gy = serde_json::Map::new();
        for (e, x, y) in members {
            gx.insert((**e).to_string(), serde_json::json!(*x));
            gy.insert((**e).to_string(), serde_json::json!(*y));
        }
        c.insert("getX".into(), serde_json::Value::Object(gx));
        c.insert("getY".into(), serde_json::Value::Object(gy));
        serde_json::to_string(&serde_json::Value::Object(c)).unwrap()
    }

    #[test]
    fn ffi_set_area_then_query_roundtrips() {
        let _g = test_lock();
        crate::state::clear_state();
        // room's "floor" area covers (10,10)..(110,110); the local rect is 0..100.
        let room_poly = c("[[0,0],[100,0],[100,100],[0,100]]");
        unsafe {
            runtime_set_entity_area(c("room").as_ptr(), c("floor").as_ptr(), room_poly.as_ptr())
        };
        crate::state::set_last_containers(vec![container(
            "c1",
            &[("room", 10.0, 10.0), ("npc-in", 30.0, 40.0), ("npc-out", 500.0, 500.0)],
        )]);

        // The area name is required and resolves the "floor" area.
        let out = unsafe {
            runtime_get_entities_inside_area(c("room").as_ptr(), c("floor").as_ptr())
        };
        assert!(!out.is_null());
        let ids = unsafe { CStr::from_ptr(out) }.to_string_lossy().to_string();
        unsafe { runtime_free_inside_area(out) };
        let parsed: Vec<String> = serde_json::from_str(&ids).unwrap();
        assert_eq!(parsed, vec!["npc-in".to_string()]);

        crate::state::clear_state();
    }

    #[test]
    fn ffi_unknown_area_name_yields_empty() {
        let _g = test_lock();
        crate::state::clear_state();
        // A small named "home" area: room at (10,10) covers (10,10)..(25,25).
        let home_poly = c("[[0,0],[15,0],[15,15],[0,15]]");
        unsafe {
            runtime_set_entity_area(c("room").as_ptr(), c("home").as_ptr(), home_poly.as_ptr())
        };
        crate::state::set_last_containers(vec![container(
            "c1",
            &[("room", 10.0, 10.0), ("npc-in", 15.0, 15.0), ("npc-out", 500.0, 500.0)],
        )]);

        // The named query resolves the "home" area first and finds npc-in.
        let out = unsafe {
            runtime_get_entities_inside_area(c("room").as_ptr(), c("home").as_ptr())
        };
        let ids = unsafe { CStr::from_ptr(out) }.to_string_lossy().to_string();
        unsafe { runtime_free_inside_area(out) };
        let parsed: Vec<String> = serde_json::from_str(&ids).unwrap();
        assert_eq!(parsed, vec!["npc-in".to_string()]);

        // An unknown named area yields empty.
        let out2 = unsafe {
            runtime_get_entities_inside_area(c("room").as_ptr(), c("ghost").as_ptr())
        };
        let ids2 = unsafe { CStr::from_ptr(out2) }.to_string_lossy().to_string();
        unsafe { runtime_free_inside_area(out2) };
        assert_eq!(ids2, "[]");

        crate::state::clear_state();
    }

    #[test]
    fn ffi_query_without_area_is_empty() {
        let _g = test_lock();
        crate::state::clear_state();
        crate::state::set_last_containers(vec![container(
            "c1",
            &[("room", 10.0, 10.0), ("npc", 30.0, 40.0)],
        )]);
        let out = unsafe {
            runtime_get_entities_inside_area(c("room").as_ptr(), c("floor").as_ptr())
        };
        let ids = unsafe { CStr::from_ptr(out) }.to_string_lossy().to_string();
        unsafe { runtime_free_inside_area(out) };
        assert_eq!(ids, "[]");
        crate::state::clear_state();
    }
}
