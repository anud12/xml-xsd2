use std::collections::HashMap;
use crate::js_host_api::Declarations;

pub fn store_entity_data(dec: &Declarations) {
    if let serde_json::Value::Object(entities) = &dec.entity_data {
        let mut text_data:
            HashMap<String, HashMap<String, String>> =
            HashMap::new();
        let mut number_data:
            HashMap<String, HashMap<String, f64>> =
            HashMap::new();
        for (entity_id, entity_val) in entities {
            extract_text_map(entity_id, entity_val, &mut text_data);
            extract_number_map(entity_id, entity_val, &mut number_data);
            extract_area(entity_id, entity_val);
        }
        crate::state::set_last_entity_data(text_data.clone());
        crate::state::set_last_entity_number_data(number_data);
        crate::state::set_initial_entity_data(text_data);
    }
}

/// An entity's declared areas (like numberMap) are intrinsic: each is
/// registered in the entity's area entityMap so the Rust-side inside-area
/// query can test it. Areas are declared as a named
/// `areaMap: { name: { polygon }, ... }` entityMap — one entry per name.
/// There is no unnamed/default area. Vertices are entity-local units (y down),
/// implicitly closed. A polygon with fewer than 3 distinct points is ignored
/// (no presence).
fn extract_area(
    entity_id: &str,
    entity_val: &serde_json::Value,
) {
    if let Some(areas_obj) = entity_val.get("areaMap").and_then(|v| v.as_object()) {
        for (name, area_obj) in areas_obj {
            let poly = parse_polygon(area_obj);
            crate::state::set_entity_area(entity_id, name, poly);
        }
    }
}

/// Read `{ polygon: [[x, y], ...] }` into local points (empty when absent).
fn parse_polygon(area_obj: &serde_json::Value) -> Vec<(f64, f64)> {
    let mut poly: Vec<(f64, f64)> = Vec::new();
    if let Some(arr) = area_obj.get("polygon").and_then(|v| v.as_array()) {
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

fn extract_text_map(
    entity_id: &str,
    entity_val: &serde_json::Value,
    data: &mut HashMap<String, HashMap<String, String>>,
) {
    if let Some(text_map) =
        entity_val.get("textMap").and_then(|v| v.as_object())
    {
        let mut tm: HashMap<String, String> = HashMap::new();
        for (k, v) in text_map {
            if let Some(s) = v.as_str() {
                tm.insert(k.clone(), s.to_string());
            }
        }
        data.insert(entity_id.to_string(), tm);
    }
}

fn extract_number_map(
    entity_id: &str,
    entity_val: &serde_json::Value,
    data: &mut HashMap<String, HashMap<String, f64>>,
) {
    if let Some(number_map) =
        entity_val.get("numberMap").and_then(|v| v.as_object())
    {
        let mut nm: HashMap<String, f64> = HashMap::new();
        for (k, v) in number_map {
            if let Some(n) = v.as_f64() {
                nm.insert(k.clone(), n);
            }
        }
        data.insert(entity_id.to_string(), nm);
    }
}
