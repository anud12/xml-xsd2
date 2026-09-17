use anyhow::{anyhow, Result};
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
    // Multi-file modules reference one another with relative `import` statements,
    // which QuickJS cannot parse in script mode. Bundle them the same way the
    // extraction path does: resolve each relative import against the archive,
    // inline its source, and strip the `export` keywords so the entry becomes a
    // single self-contained script. For single-file modules (no imports) this
    // is a no-op.
    let bundled = super::extract::bundle_imports(source);
    let transformed = transform_source(&bundled);
    ctx.with(|c| {
        c.eval::<(), _>(transformed.clone())
    })?;
    if transformed.contains("__module_default") {
        let js = r#"
try {
var h=globalThis.host;
if(!h){throw new Error("host is undefined");}
var __registeredAnimations={};
var hostApi={
  ui:{
    texture:{
      of:function(p){return p;}
    },
    getSpritePNG:function(p){return p;},
    getAnimation:function(name,animationDuration){
      var resolvedName=typeof name==='object'?name.value:name;
      if(__registeredAnimations[resolvedName]){
        return __registeredAnimations[resolvedName];
      }
      return null;
    },
    registerPanel:h.registerPanel
  },
  world:{
    sectorGrid:function(id){
      var resolvedId=typeof id==='object'?id.value:id;
      return { id: resolvedId, sectorGrid: true };
    }
  },
  runtime:{
    string:{of:function(s){return s;}},
    number:{of:function(n){return n;}},
    temporal:{ofTicks:function(n){return{type:'ticks',ticks:n};},
      ofFrames:function(n){return{type:'frames',frames:n};},
      ofSeconds:function(n){return{type:'seconds',seconds:n};}},
    emitEvent:h.emitEvent,
    registerEvent:h.registerEvent,
    registerAction:h.registerAction,
    registerEffect:h.registerEffect,
    registerContainer:h.registerContainer,
    registerEntity:h.registerEntity,
    setEntity:h.setEntity,
    setContainer:h.setContainer,
    registerBehavior:h.registerBehavior,
    registerAnimation:function(name,args){
      var resolvedName=typeof name==='object'?name.value:name;
      if(typeof resolvedName==='string'){
        __registeredAnimations[resolvedName]=args;
      }
    },
    getAnimation:function(name,animationDuration){
      var resolvedName=typeof name==='object'?name.value:name;
      if(__registeredAnimations[resolvedName]){
        return __registeredAnimations[resolvedName];
      }
      return null;
    },
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
// Wire the live .ui node factories from the persistent __uiHost layer so the
// module's hostApi.ui.panel/field/containerView calls register real nodes.
(function(){
  var u=hostApi.ui;
  var H=globalThis.__uiHost;
  if(H){
    if(H.div)u.div=H.div;
    if(H.text)u.text=H.text;
    if(H.window)u.window=H.window;
    if(H.field)u.field=H.field;
    if(H.image)u.image=H.image;
    if(H.canvas)u.canvas=H.canvas;
    if(H.entityList)u.entityList=H.entityList;
    if(H.containerView)u.containerView=H.containerView;
    if(H.sectorGrid)u.sectorGrid=H.sectorGrid;
    if(H.setActor)u.setActor=H.setActor;
  }
  // panel is the module-facing surface: positioned/sized/decorated panels are
  // window nodes, plain ones are divisions (mirrors the __uiHost prewire).
  if(H){
    u.panel=function(id,options,children){
      var o=options||{};
      var surface=o.x!==undefined||o.y!==undefined
        ||o.width!==undefined||o.height!==undefined
        ||o.background!==undefined||o.onHover!==undefined
        ||o.onClick!==undefined||o.anchor!==undefined;
      return surface?H.window(id,o,children):H.div(id,o,children);
    };
  }
  u.spriteMapTIFF=function(mapPath,layers){
    return {kind:'spriteMap',map:mapPath,
      layers:(layers||[]).map(function(l){return {layer:l.layer,texture:l.texture};})};
  };
  if(!u.getAnimation && hostApi.runtime && hostApi.runtime.getAnimation)
    u.getAnimation=hostApi.runtime.getAnimation;
})();
var __mod=globalThis.__module_default||__module_default;
if(typeof __mod==='function'){__mod(hostApi);}
} catch (e) {
  globalThis.__installErr = String(e && (e.message + ' || ' + e.stack) || e);
}
"#;
        ctx.with(|c| c.eval::<(), _>(js.to_string()))
            .map_err(|e| anyhow!("module entry invoke failed: {:?}", e))?;
        if let Ok(Some(err)) = ctx.with(|c| c.eval::<Option<String>, _>("globalThis.__installErr")) {
            return Err(anyhow!("module entry threw during install: {}", err));
        }
    }
    Ok(transformed)
}
