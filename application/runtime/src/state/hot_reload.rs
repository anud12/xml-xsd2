use std::collections::HashMap;

/// Merges preserved game state over freshly declared module state. For every
/// entity/key pair, the preserved (runtime-advanced) value wins over the
/// module's declared initial value; entities declared only in the new module
/// arrive with their declared values, and entities that exist only in the
/// preserved state (e.g. created at runtime) are kept.
pub fn merge_entity_maps<T: Clone>(
    preserved: &HashMap<String, HashMap<String, T>>,
    declared: &HashMap<String, HashMap<String, T>>,
) -> HashMap<String, HashMap<String, T>> {
    let mut out = declared.clone();
    for (id, props) in preserved.iter() {
        let entry = out.entry(id.clone()).or_insert_with(HashMap::new);
        for (k, v) in props.iter() {
            entry.insert(k.clone(), v.clone());
        }
    }
    out
}

/// Re-bakes the per-entity `getX`/`getY` positions of every serialized
/// container row from the merged entity number data. A reload re-declares the
/// containers, which re-bakes positions from the module's *initial* entity
/// values; this restores the preserved (current) positions so in-flight
/// moves and views continue from where the game left them. The position key
/// names come from the row's `xKey`/`yKey` (falling back to `x`/`y`).
pub fn rebake_container_positions(
    containers: &mut [String],
    number_data: &HashMap<String, HashMap<String, f64>>,
) {
    for json_str in containers.iter_mut() {
        let mut v = match serde_json::from_str::<serde_json::Value>(json_str) {
            Ok(v) => v,
            Err(_) => continue,
        };
        let x_key = v.get("xKey").and_then(|k| k.as_str())
            .filter(|s| !s.is_empty()).unwrap_or("x").to_string();
        let y_key = v.get("yKey").and_then(|k| k.as_str())
            .filter(|s| !s.is_empty()).unwrap_or("y").to_string();
        let entities: Vec<String> = v.get("entities").and_then(|e| e.as_array())
            .map(|arr| arr.iter().filter_map(|e| e.as_str().map(String::from)).collect())
            .unwrap_or_default();
        let mut changed = false;
        for eid in entities.iter() {
            let Some(props) = number_data.get(eid.as_str()) else { continue };
            if let (Some(gx), Some(x)) = (v.get_mut("getX").and_then(|g| g.as_object_mut()), props.get(&x_key)) {
                gx.insert(eid.clone(), num_json(*x));
                changed = true;
            }
            if let (Some(gy), Some(y)) = (v.get_mut("getY").and_then(|g| g.as_object_mut()), props.get(&y_key)) {
                gy.insert(eid.clone(), num_json(*y));
                changed = true;
            }
        }
        if changed {
            if let Ok(serialized) = serde_json::to_string(&v) {
                *json_str = serialized;
            }
        }
    }
}

fn num_json(n: f64) -> serde_json::Value {
    serde_json::Number::from_f64(n)
        .map(serde_json::Value::Number)
        .unwrap_or(serde_json::Value::Null)
}

/// Drops pending and scheduled effects that the reloaded module no longer
/// registers: their JS closures lived in the replaced sim context, so firing
/// them would be a silent no-op at best. Effects the module re-declares
/// resolve in the fresh context and keep their schedule.
pub fn drop_unresolvable_effects() {
    let Some(ctx) = crate::js_executor::sim_ctx::ctx() else { return; };
    let lookup = |name: &str| {
        crate::js_executor::lookup_effect(ctx, name)
    };
    crate::state::pending_effects().lock().unwrap().retain(|n| lookup(n));
    crate::state::scheduled_effects().lock().unwrap().retain(|e| lookup(&e.name));
}

#[cfg(test)]
mod tests {
    use super::*;

    fn text(pairs: &[(&str, &str)]) -> HashMap<String, String> {
        pairs.iter().map(|(k, v)| (k.to_string(), v.to_string())).collect()
    }

    fn num(pairs: &[(&str, f64)]) -> HashMap<String, f64> {
        pairs.iter().map(|(k, v)| (k.to_string(), *v)).collect()
    }

    #[test]
    fn preserved_value_wins_over_declared() {
        let declared: HashMap<String, HashMap<String, f64>> =
            HashMap::from([("e1".to_string(), num(&[("column", 0.0), ("row", 0.0)]))]);
        let preserved: HashMap<String, HashMap<String, f64>> =
            HashMap::from([("e1".to_string(), num(&[("column", 3.0)]))]);
        let merged = merge_entity_maps(&preserved, &declared);
        let e1 = &merged["e1"];
        assert_eq!(e1.get("column"), Some(&3.0));
        assert_eq!(e1.get("row"), Some(&0.0));
    }

    #[test]
    fn runtime_created_entity_survives_reload() {
        let declared: HashMap<String, HashMap<String, f64>> =
            HashMap::from([("e1".to_string(), num(&[("column", 0.0)]))]);
        let preserved: HashMap<String, HashMap<String, f64>> =
            HashMap::from([("e2".to_string(), num(&[("health", 7.0)]))]);
        let merged = merge_entity_maps(&preserved, &declared);
        assert_eq!(merged.len(), 2);
        assert_eq!(merged["e2"].get("health"), Some(&7.0));
        assert_eq!(merged["e1"].get("column"), Some(&0.0));
    }

    #[test]
    fn text_maps_merge_the_same_way() {
        let declared = HashMap::from([("e1".to_string(), text(&[("state", "idle")]))]);
        let preserved = HashMap::from([("e1".to_string(), text(&[("state", "busy"), ("note", "x")]))]);
        let merged = merge_entity_maps(&preserved, &declared);
        let e1 = &merged["e1"];
        assert_eq!(e1.get("state"), Some(&"busy".to_string()));
        assert_eq!(e1.get("note"), Some(&"x".to_string()));
    }

    fn container_json(id: &str, entity: &str, x_key: Option<&str>) -> String {
        let mut c = serde_json::Map::new();
        c.insert("id".into(), serde_json::json!(id));
        c.insert("entities".into(), serde_json::json!([entity]));
        c.insert("getX".into(), serde_json::json!({ entity: 0.0 }));
        c.insert("getY".into(), serde_json::json!({ entity: 0.0 }));
        if let Some(k) = x_key {
            c.insert("xKey".into(), serde_json::json!(k));
            c.insert("yKey".into(), serde_json::json!("row"));
        }
        serde_json::to_string(&serde_json::Value::Object(c)).unwrap()
    }

    fn pos(row: &str, entity: &str) -> (f64, f64) {
        let v = serde_json::from_str::<serde_json::Value>(row).unwrap();
        let x = v.get("getX").and_then(|g| g.get(entity)).and_then(|n| n.as_f64()).unwrap_or(-1.0);
        let y = v.get("getY").and_then(|g| g.get(entity)).and_then(|n| n.as_f64()).unwrap_or(-1.0);
        (x, y)
    }

    #[test]
    fn rebake_uses_position_key_names() {
        let mut rows = vec![container_json("grid", "e1", Some("column"))];
        let nd: HashMap<String, HashMap<String, f64>> =
            HashMap::from([("e1".to_string(), num(&[("column", 4.0), ("row", 2.0)]))]);
        rebake_container_positions(&mut rows, &nd);
        assert_eq!(pos(&rows[0], "e1"), (4.0, 2.0));
    }

    #[test]
    fn rebake_falls_back_to_x_y_keys() {
        let mut rows = vec![container_json("grid", "e1", None)];
        let nd: HashMap<String, HashMap<String, f64>> =
            HashMap::from([("e1".to_string(), num(&[("x", 1.0), ("y", 9.0)]))]);
        rebake_container_positions(&mut rows, &nd);
        assert_eq!(pos(&rows[0], "e1"), (1.0, 9.0));
    }

    #[test]
    fn rebake_leaves_unknown_entities_alone() {
        let mut rows = vec![container_json("grid", "e1", Some("column"))];
        rebake_container_positions(&mut rows, &HashMap::new());
        assert_eq!(pos(&rows[0], "e1"), (0.0, 0.0));
    }
}
