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
var __animationHandles={};
var hostApi={
  ui:{
    texture:{
      of:function(p){return p;}
    },
    getSpritePNG:function(p){return p;},
    getAnimation:function(name){
      var resolvedName=typeof name==='object'?name.value:name;
      if(__animationHandles[resolvedName]){
        return __animationHandles[resolvedName];
      }
      return null;
    },
    registerPanel:h.registerPanel
  },
  world:{
    sectorGrid:function(id){
      var resolvedId=typeof id==='object'?id.value:id;
      return {
        id: resolvedId, value: resolvedId, sectorGrid: true,
        toString: function() { return resolvedId; },
        toJSON: function() { return resolvedId; }
      };
    }
  },
  runtime:{
    string:{of:function(s){return s;}},
    number:(function(){function numOf(o){return Number(typeof o==='object'&&o!==null?(o._raw!==undefined?o._raw:o.value):o);}function wrap(x){x=Number(x);return{_raw:x,value:x,of:function(y){return wrap(numOf(y));},sum:function(o){return wrap(x+numOf(o));},subtract:function(o){return wrap(x-numOf(o));},multiply:function(o){return wrap(x*numOf(o));},divide:function(o){return wrap(x/numOf(o));},modulo:function(o){return wrap(x%numOf(o));},toString:function(){return String(x);},toJSON:function(){return x;},isGreaterThan:function(o){return globalThis.hostApi.runtime.condition.of(x>numOf(o));},isLessThan:function(o){return globalThis.hostApi.runtime.condition.of(x<numOf(o));},isGreaterOrEqualTo:function(o){return globalThis.hostApi.runtime.condition.of(x>=numOf(o));},isLessOrEqualTo:function(o){return globalThis.hostApi.runtime.condition.of(x<=numOf(o));},isEqualTo:function(o){return globalThis.hostApi.runtime.condition.of(x===numOf(o));},isNotEqualTo:function(o){return globalThis.hostApi.runtime.condition.of(x!==numOf(o));},toStringExpression:function(){return globalThis.hostApi.runtime.string.of(String(x));}};}var _s=0x7FFFFFFF;function lxm(){_s=(Math.imul(_s,1664525)+1013904223)&0xFFFFFFFF;var x=_s;x^=x<<13;x&=0xFFFFFFFF;x^=x>>>17;x^=x<<5;x&=0xFFFFFFFF;x^=x>>>16;x=Math.imul(x,0x85ebca6b);x^=x>>>13;x=Math.imul(x,0xc2b2ae35);x^=x>>>16;return(x>>>0)/4294967296;}return{of:function(n){return n;},random:function(f,t){var a=Math.floor(numOf(f));var b=Math.floor(numOf(t));return wrap(a+Math.floor(lxm()*(b-a+1)));}};})(),
    temporal:{ofTicks:function(n){return{type:'ticks',ticks:n};},
      ofFrames:function(n){return{type:'frames',frames:n};},
      ofSeconds:function(n){return{type:'seconds',seconds:n};}},
    emitEvent:h.emitEvent,
    setEvent:h.setEvent,
    setAction:h.setAction,
    setEffect:h.setEffect,
    setEntity:h.setEntity,
    setContainer:h.setContainer,
    linkOpening:function(a,b){
      var g=globalThis.__portalLinks;
      if(!g){g=globalThis.__portalLinks=[];}
      function norm(o){
        if(!o||typeof o!=='object')return null;
        var cell=o.cell||[0,0];
        return {container:String(o.container),
          cell:[cell[0]|0,cell[1]|0],side:String(o.side)};
      }
      var na=norm(a),nb=norm(b);
      if(!na||!nb)return;
      function key(x,y){return x.container+'|'+x.cell[0]+','+x.cell[1]+':'+x.side+
        '>>'+y.container+'|'+y.cell[0]+','+y.cell[1]+':'+y.side;}
      var ka=key(na,nb),kb=key(nb,na);
      for(var i=0;i<g.length;i++){
        var e=g[i];
        if(key(e.a,e.b)===ka||key(e.a,e.b)===kb)return;
      }
      g.push({a:na,b:nb});
    },
    setBehavior:h.setBehavior,
    setAnimation:function(name,args){
      var resolvedName=typeof name==='object'?name.value:name;
      if(typeof resolvedName==='string'){
        var handle={ name:resolvedName, value:resolvedName, toString:function(){return resolvedName;}, toJSON:function(){return resolvedName;} };
        __registeredAnimations[resolvedName]=args;
        __animationHandles[resolvedName]=handle;
        return handle;
      }
    },
    getAnimation:function(name){
      var resolvedName=typeof name==='object'?name.value:name;
      if(__animationHandles[resolvedName]){
        return __animationHandles[resolvedName];
      }
      return null;
    },
    log:h.log,
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
if (globalThis.__stringGeneratorFactory) {
  hostApi.runtime.stringGenerator = globalThis.__stringGeneratorFactory.create();
}
// Wire the live .ui node factories from the persistent __uiHost layer so the
// module's hostApi.ui.panel/field/containerView calls register real nodes.
(function(){
  var u=hostApi.ui;
  var H=globalThis.__uiHost;
  if(H){
    if(H.div)u.div=H.div;
    if(H.text)u.text=H.text;
    if(H.window)u.window=H.window;
    if(H.setScreen)u.setScreen=H.setScreen;
    if(H.field)u.field=H.field;
    if(H.image)u.image=H.image;
    if(H.progress)u.progress=H.progress;
    if(H.canvas)u.canvas=H.canvas;
    if(H.entityList)u.entityList=H.entityList;
    if(H.containerView)u.containerView=H.containerView;
    if(H.sectorGrid)u.sectorGrid=H.sectorGrid;
    if(H.setActor)u.setActor=H.setActor;
    if(H.setFont)u.setFont=H.setFont;
  }
  // panel is a plain flow container (a division): only ui.window creates a
  // window and ui.setScreen the screen (the top-level surfaces). A standalone
  // (window-less, screen-less) node is a no-op — it registers but is never
  // added to the view.
  if(H){
    u.panel=H.div;
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
