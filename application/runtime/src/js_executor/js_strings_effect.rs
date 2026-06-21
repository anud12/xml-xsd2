use rquickjs::Context;

const PREPARE_JS: &str = r#"(function(){
    if (globalThis.__foundEffect &&
        typeof globalThis.__foundEffect.prepare === 'function') {
        try { globalThis.__prepared =
            globalThis.__foundEffect.prepare(globalThis.__context);
        } catch(e) {}
    }
})()"#;

const APPLY_JS: &str = r#"(function(){
    if (globalThis.__foundEffect &&
        typeof globalThis.__foundEffect.apply === 'function') {
        try { globalThis.__foundEffect.apply(
            globalThis.__context, globalThis.__prepared);
        } catch(e) {}
    }
})()"#;

const REOCCUR_JS: &str = r#"(function() {
    if (globalThis.__foundEffect &&
        typeof globalThis.__foundEffect.reoccurAfterMs === 'function') {
        try {
            var r = globalThis.__foundEffect.reoccurAfterMs(
                globalThis.__context);
            if (r && typeof r === 'object') {
                if (typeof r.value === 'number') return r.value;
                if (r.value === undefined) return -1;
            }
            if (typeof r === 'number') return r;
        } catch(e) {}
    }
    return -1;
})()"#;

pub fn call_effect_prepare(ctx: &Context) {
    let _ = ctx.with(|c| c.eval::<(), _>(PREPARE_JS));
}

pub fn call_effect_apply(ctx: &Context) {
    let _ = ctx.with(|c| c.eval::<(), _>(APPLY_JS));
}

pub fn eval_reoccur_interval(ctx: &Context) -> f64 {
    ctx.with(|c| c.eval::<f64, _>(REOCCUR_JS)).unwrap_or(-1.0)
}
