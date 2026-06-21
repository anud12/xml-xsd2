use anyhow::{anyhow, Result};
use crate::js_runtime::{create_runtime, create_context};
use rquickjs::{Context, Runtime};
use crate::js_host_api::{install_host_api, extract_declarations, Declarations};

pub const HOST_API_JS: &str = r#"(function(){var h={
    string:{of:s=>s},number:{of:n=>n},
    entity:{create:function(){return{withTextMap:function(t){return t;}}},
        filter:{create:function(){return{byId:function(f){return{fn:f;}};}}}},
    textMap:{create:function(){return{put:function(k,v){
        const o={};o[k]=v;return o;}};}},
    texture:{of:function(p){return p;}},
    emitEvent:host.emitEvent,registerEvent:host.registerEvent,
    registerAction:host.registerAction,registerEffect:host.registerEffect,
    registerPanel:host.registerPanel,setEntity:host.setEntity,log:host.log,
    maybe:{of:function(v){return{value:v};},none:function(){
        return{value:undefined};}},
    condition:{of:function(v){return{value:v,ifTrue:function(cb){
        if(v&&typeof cb==='function')cb();},ifFalse:function(cb){
        if(!v&&typeof cb==='function')cb();}};}}};
    globalThis.hostApi=h;return h;})()"#;

fn create_rt_ctx_and_install() -> Result<(Runtime, Context)> {
    let rt = create_runtime()?;
    let ctx = create_context(&rt)?;
    install_host_api(&ctx)?;
    Ok((rt, ctx))
}

fn transform_source(source: &str) -> String {
    if source.contains("export default") {
        source.replace("export default", "var __module_default =")
    } else { source.to_string() }
}

fn eval_source_in_ctx(ctx: &Context, code: &str) -> Result<()> {
    match ctx.with(|ctx| ctx.eval::<(), _>(code.to_string())) {
        Err(e) => Err(anyhow!("QuickJS eval error: {}", e)),
        Ok(()) => Ok(()),
    }
}

fn call_module_default(ctx: &Context, transformed: &str) {
    if transformed.contains("__module_default") {
        let s = format!("try{{{};if(typeof __module_default==='function')\
            {{__module_default(hostApi);}}}}catch(e){{}}", HOST_API_JS);
        let _ = ctx.with(|ctx| ctx.eval::<(), _>(s));
    }
}

pub fn extract_from_source(source: &str) -> Result<Declarations> {
    let (_rt, ctx) = create_rt_ctx_and_install()?;
    let transformed = transform_source(source);
    eval_source_in_ctx(&ctx, &transformed)?;
    call_module_default(&ctx, &transformed);
    extract_declarations(&ctx)
}
