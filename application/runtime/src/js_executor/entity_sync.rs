use rquickjs::Context;

pub fn lookup_effect(ctx: &Context, name: &str) -> bool {
    let lookup = format!(r#"(function(){{
        var evs = globalThis.__registeredEvents || [];
        for (var i = 0; i < evs.length; i++) {{
            if (evs[i] && evs[i].name === '{}') {{
                globalThis.__foundEffect = evs[i]; break;
            }}
        }}
    }})();"#, name);
    if ctx.with(|c| c.eval::<(), _>(lookup)).is_err() { return false; }
    ctx.with(|c| c.eval::<bool, _>(
        "globalThis.__foundEffect !== undefined")).unwrap_or(false)
}

pub fn sync_entity_store(ctx: &Context) {
    let nd = crate::state::last_entity_number_data().lock().unwrap().clone();
    let es: Vec<serde_json::Value> = nd.iter().map(|(id, props)| {
        let mut obj = serde_json::Map::new();
        obj.insert(id.clone(), serde_json::Value::String(id.clone()));
        if let Some(n) = props.get("key") {
            obj.insert("key".into(), serde_json::Value::Number(
                serde_json::Number::from_f64(*n)
                    .unwrap_or(serde_json::Number::from(0))));
        }
        serde_json::Value::Object(obj)
    }).collect();
    let ss = serde_json::to_string(&es).unwrap_or_else(|_| "[]".into());
    let _ = ctx.with(|c| c.eval::<(), _>(
        format!("globalThis.__entityStore = {}; ", ss)));
    super::entity_sync_map::__sync_entity_data_map(ctx);
}

pub fn sync_entity_data_with_initial(ctx: &Context) {
    let nd = crate::state::last_entity_number_data().lock().unwrap().clone();
    let init = crate::state::initial_entity_data().lock().unwrap().clone();
    *crate::state::last_entity_data().lock().unwrap() = init;
    let td = crate::state::last_entity_data().lock().unwrap().clone();
    super::entity_sync_map::__sync_entity_data_map(ctx);
    build_initial_entity_data_json(ctx, &nd, &td);
}

fn build_initial_entity_data_json(
    ctx: &Context,
    nd: &std::collections::HashMap<String, std::collections::HashMap<String, f64>>,
    td: &std::collections::HashMap<String, std::collections::HashMap<String, String>>,
) {
    let dj: std::collections::HashMap<String, serde_json::Value> = nd.iter()
        .map(|(id, _)| {
            let nm = serde_json::Map::new();
            let mut obj = serde_json::Map::new();
            obj.insert("numberMap".into(), serde_json::Value::Object(nm));
            if let Some(tp) = td.get(id) {
                let mut tm = serde_json::Map::new();
                for (k, v) in tp.iter() {
                    tm.insert(k.clone(), serde_json::Value::String(v.clone()));
                }
                obj.insert("textMap".into(), serde_json::Value::Object(tm));
            }
            (id.clone(), serde_json::Value::Object(obj))
        }).collect();
    let ds = serde_json::to_string(&dj).unwrap_or_else(|_| "{}".into());
    let _ = ctx.with(|c| c.eval::<(), _>(
        format!("globalThis.__entityData = {}; ", ds)));
}
