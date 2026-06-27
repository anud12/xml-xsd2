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
        }
        crate::state::set_last_entity_data(text_data.clone());
        crate::state::set_last_entity_number_data(number_data);
        crate::state::set_initial_entity_data(text_data);
    }
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
