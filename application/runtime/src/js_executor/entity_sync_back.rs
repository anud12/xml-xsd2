use rquickjs::Context;

pub fn sync_entity_data_back(ctx: &Context) {
    let ds = ctx.with(|c| c.eval::<String, _>(
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
}

pub fn collect_logs(ctx: &Context) {
    let lj = ctx.with(|c| c.eval::<String, _>(
        "JSON.stringify(globalThis.__logs || [])"))
        .unwrap_or_else(|_| "[]".into());
    if let Ok(lv) = serde_json::from_str::<Vec<String>>(&lj) {
        for l in lv.iter() { runtime_log!("{}", l); }
    }
}
