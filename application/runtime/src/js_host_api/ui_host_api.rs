//! Shared hostApi shim used by the extraction invoke script and the effect
//! simulation entry (`sim_entry`).
//!
//! The host script parts (script_emit / script_rest) already install
//! `globalThis.host` with register*/set*/emitEvent/log/entity. The invoke
//! script reuses that object instead of rebuilding a shadowing hostApi, and
//! only adds the ui.* factories that the host script parts do not provide.
//!
//! `ui.panel` / `ui.window` / `ui.field` / `ui.text` / `ui.entityList` mirror
//! the C# HostApiSetup `__panelEmit` JSON shape and record each node into
//! `globalThis.__registeredPanels`, which the extraction declaration script
//! picks up. `getAnimation` returns the same handle `setAnimation` stored in
//! `globalThis.__animationHandles` (keyed by animation name), so a UI node
//! references its animation the way it references a bound entity — by a
//! name the engine resolves against the registration store every tick.

pub const SHIM_JS: &str = r#"
var __hapi_getAnimation = function (name) {
  var resolvedName = typeof name === 'object' ? name.value : name;
  var handles = globalThis.__animationHandles || {};
  if (handles[resolvedName]) { return handles[resolvedName]; }
  return null;
};
var __hapi_setAnimation = function (name, args) {
  globalThis.__registeredAnimations =
    globalThis.__registeredAnimations || {};
  globalThis.__animationHandles =
    globalThis.__animationHandles || {};
  var resolvedName = typeof name === 'object' ? name.value : name;
  if (typeof resolvedName === 'string') {
    if (!args || typeof args !== 'object' ||
        typeof args.duration !== 'number') {
      throw new Error("setAnimation '" + resolvedName +
        "': duration is required");
    }
    var handle = {
      name: resolvedName, value: resolvedName,
      toString: function() { return resolvedName; },
      toJSON: function() { return resolvedName; }
    };
    globalThis.__registeredAnimations[resolvedName] = args;
    globalThis.__animationHandles[resolvedName] = handle;
    return handle;
  }
};
// An animation reference: a static archive path string, the setAnimation
// handle, or a plain { name } ref. The legacy panel JSON inlines the full
// registration (frames/duration/loop) resolved through the store.
var __hapi_isAnimationRef = function (v) {
  return (typeof v === 'string' && v.length > 0)
    || (!!v && typeof v === 'object'
      && typeof v.name === 'string' && v.name.length > 0);
};
var __hapi_resolveAnimation = function (v) {
  var name = typeof v === 'string' ? v : v.name;
  var store = globalThis.__registeredAnimations || {};
  if (store[name]) { return store[name]; }
  throw new Error("animation '" + name + "' not registered");
};
var __hapi_firstFrameRef = function (v) {
  if (typeof v === 'string' && v.length > 0) return v;
  var frames = null;
  if (v && typeof v === 'object') {
    if (Array.isArray(v.frames)) {
      frames = v.frames;
    } else if (typeof v.name === 'string' && v.name.length > 0) {
      var reg = (globalThis.__registeredAnimations || {})[v.name];
      if (reg && Array.isArray(reg.frames)) frames = reg.frames;
    }
  }
  if (!frames) return null;
  for (var i = 0; i < frames.length; i++) {
    var f = frames[i];
    if (typeof f === 'string' && f.length > 0) return f;
    if (f && typeof f === 'object' && f.sprite != null) {
      var s = f.sprite;
      if (typeof s === 'string' && s.length > 0) return s;
      if (typeof s === 'object' && s.__spriteMap) {
        return JSON.stringify({ __spriteMap: true,
                                map: s.map, layers: s.layers || [] });
      }
      if (typeof s === 'object' && typeof s.name === 'string') return s.name;
    }
  }
  return null;
};
var __hapi_registerPanel = function (json) {
  globalThis.__registeredPanels =
    globalThis.__registeredPanels || [];
  globalThis.__registeredPanels.push(JSON.stringify(json));
};
var __hapi_anchorMap = {
  'top-left': [0, 0], 'top': [0.5, 0], 'top-right': [1, 0],
  'left': [0, 0.5], 'center': [0.5, 0.5], 'right': [1, 0.5],
  'bottom-left': [0, 1], 'bottom': [0.5, 1], 'bottom-right': [1, 1]
};
var __hapi_panelEmit = function (id, options, children, forceSurface) {
  var opts = options || {};
  var hasSurface = forceSurface
    || opts.x !== undefined || opts.y !== undefined
    || opts.width !== undefined || opts.height !== undefined
    || opts.background !== undefined || opts.onHover !== undefined
    || opts.onClick !== undefined || opts.anchor !== undefined;
  var json = { id: id };
  if (hasSurface) {
    var anchor = [0.5, 0.5];
    if (opts.anchor) {
      if (typeof opts.anchor === 'string'
          && __hapi_anchorMap[opts.anchor]) {
        anchor = __hapi_anchorMap[opts.anchor];
      } else if (typeof opts.anchor === 'object') {
        anchor = [opts.anchor.x !== undefined ? opts.anchor.x : 0.5,
                  opts.anchor.y !== undefined ? opts.anchor.y : 0.5];
      }
    }
    if (opts.background !== undefined) {
      if (!__hapi_isAnimationRef(opts.background))
        throw new Error("panel '" + id
          + "': background must be an archive path or animation name/handle");
      json.background = (typeof opts.background === 'string')
        ? opts.background : __hapi_resolveAnimation(opts.background);
    }
    json.surface = true;
    json.size = { width: opts.width || 0, height: opts.height || 0 };
    json.anchor = { x: anchor[0], y: anchor[1] };
    json.offset = {
      top: opts.y || 0, bottom: 0,
      left: opts.x || 0,
      right: opts.width ? (opts.width - (opts.x || 0)) : 0
    };
    if (opts.onHover) {
      if (opts.onHover.texture !== undefined
          && !__hapi_isAnimationRef(opts.onHover.texture))
        throw new Error("panel '" + id
          + "': onHover.texture must be an archive path or animation name/handle");
      if (opts.onHover.background !== undefined
          && !__hapi_isAnimationRef(opts.onHover.background))
        throw new Error("panel '" + id
          + "': onHover.background must be an archive path or animation name/handle");
      var __hoverTexture = opts.onHover.texture !== undefined
        ? __hapi_firstFrameRef(opts.onHover.texture) : null;
      var __hoverBackground = opts.onHover.background !== undefined
        ? __hapi_firstFrameRef(opts.onHover.background) : null;
      json.hover = {
        texture: __hoverTexture,
        thickness: (opts.onHover.thickness !== undefined ? opts.onHover.thickness : 0),
        background: __hoverBackground,
        emitAction: opts.onHover.emitAction || null,
        stopPropagation: opts.onHover.stopPropagation || false
      };
    }
    json.onClick = opts.onClick
      ? { type: "emitAction", actionName: opts.onClick } : null;
  }
  if (opts.layout !== undefined) {
    json.layout = typeof opts.layout === "object" ? opts.layout
      : (opts.layout === "row" ? { rowFirst: true } : { rowFirst: false });
  }
  if (opts.border !== undefined && typeof opts.border === "object") {
    if (opts.border.texture !== undefined
        && !__hapi_isAnimationRef(opts.border.texture))
      throw new Error("panel '" + id
        + "': border.texture must be an archive path or animation name/handle");
    var __borderTexture = opts.border.texture === undefined
      ? null
      : (typeof opts.border.texture === 'string'
        ? opts.border.texture : __hapi_resolveAnimation(opts.border.texture));
    json.border = {
      width: opts.border.width !== undefined ? opts.border.width : 1,
      texture: __borderTexture
    };
  }
  json.children = children || [];
  __hapi_registerPanel(json);
  return id;
};
var h=globalThis.host;
if(!h){throw new Error("host is undefined");}
var hostApi=h;
if(!hostApi.ui){hostApi.ui={};}
if(!hostApi.runtime){hostApi.runtime={};}
var __hapi_ui=hostApi.ui;
if(!__hapi_ui.texture){__hapi_ui.texture={of:function(p){return p;}};}
if(!__hapi_ui.getSpritePNG){
  __hapi_ui.getSpritePNG=function(p){return p;};
}
if(!__hapi_ui.getAnimation){
  __hapi_ui.getAnimation=__hapi_getAnimation;
}
if(!__hapi_ui.panel){
  __hapi_ui.panel=function(id,options,children){
    return __hapi_panelEmit(id,options,children,false);
  };
}
if(!__hapi_ui.window){
  __hapi_ui.window=function(id,options,children){
    return __hapi_panelEmit(id,options,children,true);
  };
}
if(!__hapi_ui.text){
  __hapi_ui.text=function(id,value){
    __hapi_registerPanel({
      id:id,
      content:{type:"constant",value:String(value)}
    });
    return id;
  };
}
if(!__hapi_ui.field){
  __hapi_ui.field=function(id,binding){
    var isNumber=binding&&binding.map==="number";
    var content={
      type:isNumber?"entityNumberValue":"entityTextValue",
      name:binding?binding.name:"",
      entityId:binding?binding.entity:"",
      fallback:binding&&typeof binding.fallback==="string"?binding.fallback:""
    };
    if(binding&&typeof binding.align==="string"){content.align=binding.align;}
    __hapi_registerPanel({id:id,content:content});
    return id;
  };
}
if(!__hapi_ui.div){
  __hapi_ui.div=function(id,options,children){
    var layout=options&&options.layout?options.layout:"column";
    return __hapi_panelEmit(id,{layout:layout},children,false);
  };
}
if(!__hapi_ui.entityList){
  __hapi_ui.entityList=function(id,options,template){
    var containerId=options&&options.container;
    var resolvedContainerId=(typeof containerId==="object")?containerId.value:containerId;
    var vertical=options&&options.vertical!==undefined?options.vertical:true;
    var containerData=globalThis.__containerData
      ?globalThis.__containerData[resolvedContainerId]:undefined;
    var entityIds=[];
    if(resolvedContainerId&&containerData&&containerData.entities){
      for(var ei=0;ei<containerData.entities.length;ei++){
        var ent=containerData.entities[ei];
        entityIds.push(typeof ent==="object"?ent.value:ent);
      }
    }
    var childIds=[];
    if(typeof template==="function"){
      for(var i=0;i<entityIds.length;i++){
        var result=template({id:entityIds[i],index:i});
        var arr=Array.isArray(result)?result:[result];
        for(var k=0;k<arr.length;k++){
          if(arr[k]!=null){childIds.push(arr[k]);}
        }
      }
    }
    __hapi_registerPanel({
      id:id,
      content:{type:"containerListView",containerId:containerId,vertical:vertical},
      children:childIds
    });
    return id;
  };
}
var __hapi_runtime=hostApi.runtime;
if(!__hapi_runtime.string){__hapi_runtime.string={of:function(s){return s;}};}
if(!__hapi_runtime.number){__hapi_runtime.number={of:function(n){return n;}};}
if(!__hapi_runtime.temporal){
  __hapi_runtime.temporal={
    ofTicks:function(n){return{type:'ticks',ticks:n};}
  };
}
if(!__hapi_runtime.emitEvent){__hapi_runtime.emitEvent=h.emitEvent;}
if(!__hapi_runtime.setEvent){__hapi_runtime.setEvent=h.setEvent;}
if(!__hapi_runtime.setAction){__hapi_runtime.setAction=h.setAction;}
if(!__hapi_runtime.setEffect){__hapi_runtime.setEffect=h.setEffect;}
if(!__hapi_runtime.setEntity){__hapi_runtime.setEntity=h.setEntity;}
if(!__hapi_runtime.setContainer){__hapi_runtime.setContainer=h.setContainer;}
if(!__hapi_runtime.setBehavior){__hapi_runtime.setBehavior=h.setBehavior;}
if(!__hapi_runtime.setAnimation){
  __hapi_runtime.setAnimation=__hapi_setAnimation;
}
if(!__hapi_runtime.getAnimation){
  __hapi_runtime.getAnimation=__hapi_getAnimation;
}
if(!__hapi_runtime.log){__hapi_runtime.log=h.log;}
if(!__hapi_runtime.entity){__hapi_runtime.entity=h.entity;}
if(!__hapi_runtime.maybe){
  __hapi_runtime.maybe={
    of:function(v){return{value:v};},
    none:function(){return{value:undefined};}
  };
}
if(!__hapi_runtime.condition){
  __hapi_runtime.condition={
    of:function(v){
      return{
        value:v,
        ifTrue:function(cb){
          if(v&&typeof cb==='function'){cb();}
        },
        ifFalse:function(cb){
          if(!v&&typeof cb==='function'){cb();}
        }
      };
    }
  };
}
// World/sector namespace: `world.sectorGrid(id)` declares a named grid and
// returns it. Sectors attach later via a container's optional `sector` field
// (captured in setContainer). The returned object only needs a stable id so
// module code can hold a reference; the Rust layer computes the actual grid.
if(!hostApi.world){
  hostApi.world={
    sectorGrid:function(id){
      var resolvedId=typeof id==='object'?id.value:id;
      return {
        id: resolvedId, value: resolvedId, sectorGrid: true,
        toString: function() { return resolvedId; },
        toJSON: function() { return resolvedId; }
      };
    }
  };
}
globalThis.hostApi=hostApi;
var __mod=globalThis.__module_default||__module_default;
if(typeof __mod==='function'){__mod(hostApi);}
"#;

pub fn host_api_shim_js() -> &'static str {
    SHIM_JS
}

pub const SHIM_JS_LIT: &str = SHIM_JS;
