use rquickjs::Context;

pub fn sync_entity_data_back(ctx: &Context) {
    let ds = crate::js_executor::sim_ctx::sim_with(ctx, |c| c.eval::<String, _>(
        "JSON.stringify(globalThis.__entityData || {})"))
        .unwrap_or_else(|_| "{}".into());
    if let Ok(upd) = serde_json::from_str::<
        std::collections::HashMap<String, serde_json::Value>
    >(&ds) {
        apply_entity_data_update(&upd);
    }
}

fn apply_entity_data_update(
    upd: &std::collections::HashMap<String, serde_json::Value>,
) {
    let mut nd = crate::state::last_entity_number_data().lock().unwrap();
    let mut td = crate::state::last_entity_data().lock().unwrap();
    for (eid, ev) in upd.iter() {
        apply_number_and_text_maps(eid, ev, &mut nd, &mut td);
    }
}

fn apply_number_and_text_maps(
    eid: &str,
    ev: &serde_json::Value,
    nd: &mut std::collections::HashMap<String, std::collections::HashMap<String, f64>>,
    td: &mut std::collections::HashMap<String, std::collections::HashMap<String, String>>,
) {
    let em = nd.entry(eid.to_string())
        .or_insert_with(std::collections::HashMap::new);
    if let Some(nm) = ev.get("numberMap").and_then(|v| v.as_object()) {
        for (k, v) in nm.iter() {
            if let Some(n) = v.as_f64() { em.insert(k.clone(), n); }
        }
    }
    if let Some(tm) = ev.get("textMap").and_then(|v| v.as_object()) {
        let tem = td.entry(eid.to_string())
            .or_insert_with(std::collections::HashMap::new);
        for (k, v) in tm.iter() {
            if let Some(s) = v.as_str() {
                tem.insert(k.clone(), s.to_string());
            }
        }
    }
    // An entity's areas (like numberMap) are intrinsic: persist each declared
    // local polygon so the Rust-side inside-area query can test it. Areas are
    // always declared as a named `areaMap: { name: { polygon } }` entityMap.
    if let Some(areas_obj) = ev.get("areaMap").and_then(|v| v.as_object()) {
        for (name, area_obj) in areas_obj {
            crate::state::set_entity_area(eid, name, parse_polygon(area_obj));
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

pub fn collect_logs(ctx: &Context) {
    let lj = crate::js_executor::sim_ctx::sim_with(ctx, |c| c.eval::<String, _>(
        "JSON.stringify(globalThis.__logs || [])"))
        .unwrap_or_else(|_| "[]".into());
    if let Ok(lv) = serde_json::from_str::<Vec<String>>(&lj) {
        for l in lv.iter() { runtime_log!("{}", l); }
    }
    let _ = crate::js_executor::sim_ctx::sim_with(ctx, |c| c.eval::<(), _>("globalThis.__logs = []"));
}
