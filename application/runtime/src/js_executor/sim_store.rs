use anyhow::Result;
use serde_json::Value;

pub fn build_initial_store_json(store: &[Vec<String>]) -> Result<String> {
    let mut arr: Vec<Value> = Vec::new();
    for row in store.iter() {
        if !row.is_empty() {
            let key = row[0].clone();
            let mut map = serde_json::Map::new();
            map.insert("textMap_name".into(), Value::String(key.clone()));
            map.insert(key.clone(), Value::String(key.clone()));
            arr.push(Value::Object(map));
        }
    }
    Ok(serde_json::to_string(&arr)?)
}

pub fn convert_store_values(values: &[Value]) -> Vec<Vec<String>> {
    let mut rows: Vec<Vec<String>> = Vec::new();
    for obj in values.iter() {
        if let Some(map) = obj.as_object() {
            if !map.is_empty() {
                let (_k, v) = map.iter().next().unwrap();
                if let Some(s) = v.as_str() { rows.push(vec![s.to_string()]); }
                else { rows.push(vec![v.to_string()]); }
            } else { rows.push(vec!["".to_string()]); }
        } else { rows.push(vec![obj.to_string()]); }
    }
    rows
}
