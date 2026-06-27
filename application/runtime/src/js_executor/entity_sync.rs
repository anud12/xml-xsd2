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
    let init = crate::state::initial_entity_data().lock().unwrap().clone();
    *crate::state::last_entity_data().lock().unwrap() = init;
    super::entity_sync_map::__sync_entity_data_map(ctx);
}
