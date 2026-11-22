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
    if crate::js_executor::sim_ctx::sim_with(ctx, |c| c.eval::<(), _>(lookup)).is_err() { return false; }
    crate::js_executor::sim_ctx::sim_with(ctx, |c| c.eval::<bool, _>(
        "globalThis.__foundEffect !== undefined")).unwrap_or(false)
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
