use rquickjs::Context;

fn sync_entity_data_map(
    ctx: &Context,
    nd: &std::collections::HashMap<String, std::collections::HashMap<String, f64>>,
) {
    let td = crate::state::last_entity_data().lock().unwrap().clone();
    let dj: std::collections::HashMap<String, serde_json::Value> = nd.iter()
        .map(|(id, props)| build_entity_entry(id, props, &td))
        .collect();
    let ds = serde_json::to_string(&dj).unwrap_or_else(|_| "{}".into());
    let _ = ctx.with(|c| c.eval::<(), _>(
        format!("globalThis.__entityData = {}; ", ds)));
}

fn build_entity_entry(
    id: &str,
    props: &std::collections::HashMap<String, f64>,
    td: &std::collections::HashMap<String, std::collections::HashMap<String, String>>,
) -> (String, serde_json::Value) {
    let mut nm = serde_json::Map::new();
    for (k, v) in props.iter() {
        if let Some(n) = serde_json::Number::from_f64(*v) {
            nm.insert(k.clone(), serde_json::Value::Number(n));
        }
    }
    let mut obj = serde_json::Map::new();
    obj.insert("numberMap".into(), serde_json::Value::Object(nm));
    if let Some(tp) = td.get(id) {
        let mut tm = serde_json::Map::new();
        for (k, v) in tp.iter() {
            tm.insert(k.clone(), serde_json::Value::String(v.clone()));
        }
        obj.insert("textMap".into(), serde_json::Value::Object(tm));
    }
    (id.to_string(), serde_json::Value::Object(obj))
}

pub(crate) fn __sync_entity_data_map(ctx: &Context) {
    let nd = crate::state::last_entity_number_data().lock().unwrap().clone();
    sync_entity_data_map(ctx, &nd);
}
