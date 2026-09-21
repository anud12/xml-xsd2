use rquickjs::Context;

const PREPARE_JS: &str = r#"(function(){
    globalThis.__dbg = { step: 'start' };
    if (globalThis.__foundEffect &&
        typeof globalThis.__foundEffect.prepare === 'function') {
        globalThis.__dbg.step = 'found-prepare';
        try { globalThis.__prepared =
            globalThis.__foundEffect.prepare(globalThis.__context);
        globalThis.__dbg.step = 'prepare-ok';
        } catch(e) { globalThis.__dbg.err = String(e); }
    } else {
        globalThis.__dbg.step = 'no-prepare';
    }
})()"#;

const APPLY_JS: &str = r#"(function(){
    globalThis.__dbg = globalThis.__dbg || { step: 'start' };
    globalThis.__dbg.applyStep = 'start';
    if (globalThis.__foundEffect &&
        typeof globalThis.__foundEffect.apply === 'function') {
        globalThis.__dbg.applyStep = 'found-apply';
        try {
            globalThis.__dbg.applyStep = 'calling';
            globalThis.__foundEffect.apply(
                globalThis.__context, globalThis.__prepared);
            globalThis.__dbg.applyStep = 'apply-ok';
        } catch(e) { globalThis.__dbg.applyErr = String(e); }
    } else {
        globalThis.__dbg.applyStep = 'no-apply';
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
    let _ = crate::js_executor::sim_ctx::sim_with(ctx, |c| c.eval::<(), _>(PREPARE_JS));
}

pub fn call_effect_apply(ctx: &Context) {
    let _ = crate::js_executor::sim_ctx::sim_with(ctx, |c| c.eval::<(), _>(APPLY_JS));
}

pub fn eval_reoccur_interval(ctx: &Context) -> f64 {
    crate::js_executor::sim_ctx::sim_with(ctx, |c| c.eval::<f64, _>(REOCCUR_JS)).unwrap_or(-1.0)
}
