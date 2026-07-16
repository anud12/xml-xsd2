use anyhow::Result;
use rquickjs::Context;

pub fn select_entry_source(
    files: &std::collections::HashMap<String, String>,
) -> String {
    use serde_json::Value;
    for (name, content) in files.iter() {
        if name.ends_with("manifest.json") ||
            (name.to_lowercase().contains("manifest") && name.ends_with(".json")) {
            if let Ok(v) = serde_json::from_str::<Value>(content) {
                if let Some(entry) = v.get("entry").and_then(|v| v.as_str()) {
                    if let Some(src) = files.get(entry) { return src.clone(); }
                }
            }
        }
    }
    if let Some(src) = files.get("index.js") { return src.clone(); }
    if let Some((_k, v)) = files.iter().next() { return v.clone(); }
    "".to_string()
}

fn transform_source(source: &str) -> String {
    if source.contains("export default") {
        source.replace("export default", "var __module_default =")
    } else { source.to_string() }
}

pub fn eval_entry_in_ctx(ctx: &Context, source: &str) -> Result<String> {
    let transformed = transform_source(source);
    ctx.with(|c| {
        c.eval::<(), _>(transformed.clone())
    })?;
    if transformed.contains("__module_default") {
        let js = r#"
var h=globalThis.host;
if(!h){throw new Error("host is undefined");}
var hostApi={
  ui:{
    texture:{of:function(p){return p;}},
    registerPanel:h.registerPanel
  },
  runtime:{
    string:{of:function(s){return s;}},
    number:{of:function(n){return n;}},
    emitEvent:h.emitEvent,
    registerEvent:h.registerEvent,
    registerAction:h.registerAction,
    registerEffect:h.registerEffect,
    registerContainer:h.registerContainer,
    registerEntity:h.registerEntity,
    setEntity:h.setEntity,
    log:h.log,
    entity:h.entity,
    maybe:{
        of:function(v){return{value:v};},
        none:function(){return{value:undefined};}
      },
      condition:{
        of:function(v){
          return{
            value:v,
            ifTrue:function(cb){
              if(v&&typeof cb==='function')cb();
            },
            ifFalse:function(cb){
              if(!v&&typeof cb==='function')cb();
            }
          };
        }
      }
  }
};
globalThis.hostApi=hostApi;
var __mod=globalThis.__module_default||__module_default;
if(typeof __mod==='function'){__mod(hostApi);}
"#;
        let _ = ctx.with(|c| c.eval::<(), _>(js.to_string()));
    }
    Ok(transformed)
}
