//! Shared hostApi shim used by the extraction invoke script and the effect
//! simulation entry (`sim_entry`).
//!
//! The host script parts (script_emit / script_rest) already install
//! `globalThis.host` with register*/set*/emitEvent/log/entity. The invoke
//! script reuses that object instead of rebuilding a shadowing hostApi, and
//! only adds the ui.* factories that the host script parts do not provide.
//!
//! `ui.panel` / `ui.window` / `ui.field` / `ui.text` / `ui.container` mirror
//! the C# HostApiSetup `__panelEmit` JSON shape and record each node into
//! `globalThis.__registeredPanels`, which the extraction declaration script
//! picks up. `getAnimation` reads `globalThis.__registeredAnimations` (the
//! object filled by the host script's registerAnimation) instead of a
//! shadowing local, so a background registered before a panel resolves.

pub const SHIM_JS: &str = r#"
var __hapi_getAnimation = function (name, animationDuration) {
  var resolvedName = typeof name === 'object' ? name.value : name;
  var store = globalThis.__registeredAnimations || {};
  if (store[resolvedName]) { return store[resolvedName]; }
  return null;
};
var __hapi_registerAnimation = function (name, args) {
  globalThis.__registeredAnimations =
    globalThis.__registeredAnimations || {};
  var resolvedName = typeof name === 'object' ? name.value : name;
  if (typeof resolvedName === 'string') {
    if (!args || typeof args !== 'object' ||
        typeof args.duration !== 'number') {
      throw new Error("registerAnimation '" + resolvedName +
        "': duration is required");
    }
    globalThis.__registeredAnimations[resolvedName] = args;
  }
};
var __hapi_isAnimation = function (v) {
  return !!v && typeof v === 'object'
    && Array.isArray(v.frames) && v.frames.length > 0;
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
      if (!__hapi_isAnimation(opts.background))
        throw new Error("panel '" + id
          + "': background must be an AnimationRegistrationArguments (use hostApi.ui.getAnimation)");
      json.background = opts.background;
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
          && !__hapi_isAnimation(opts.onHover.texture))
        throw new Error("panel '" + id
          + "': onHover.texture must be an AnimationRegistrationArguments (use hostApi.ui.getAnimation)");
      if (opts.onHover.background !== undefined
          && !__hapi_isAnimation(opts.onHover.background))
        throw new Error("panel '" + id
          + "': onHover.background must be an AnimationRegistrationArguments (use hostApi.ui.getAnimation)");
      json.hover = {
        texture: opts.onHover.texture !== undefined ? opts.onHover.texture : null,
        thickness: (opts.onHover.thickness !== undefined ? opts.onHover.thickness : 0),
        background: opts.onHover.background !== undefined ? opts.onHover.background : null,
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
        && !__hapi_isAnimation(opts.border.texture))
      throw new Error("panel '" + id
        + "': border.texture must be an AnimationRegistrationArguments (use hostApi.ui.getAnimation)");
    json.border = {
      width: opts.border.width !== undefined ? opts.border.width : 1,
      texture: opts.border.texture !== undefined ? opts.border.texture : null
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
if(!__hapi_ui.container){
  __hapi_ui.container=function(id,options,template){
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
if(!__hapi_runtime.registerEvent){__hapi_runtime.registerEvent=h.registerEvent;}
if(!__hapi_runtime.registerAction){__hapi_runtime.registerAction=h.registerAction;}
if(!__hapi_runtime.registerEffect){__hapi_runtime.registerEffect=h.registerEffect;}
if(!__hapi_runtime.registerContainer){__hapi_runtime.registerContainer=h.registerContainer;}
if(!__hapi_runtime.registerEntity){__hapi_runtime.registerEntity=h.registerEntity;}
if(!__hapi_runtime.setEntity){__hapi_runtime.setEntity=h.setEntity;}
if(!__hapi_runtime.setContainer){__hapi_runtime.setContainer=h.setContainer;}
if(!__hapi_runtime.registerBehavior){__hapi_runtime.registerBehavior=h.registerBehavior;}
if(!__hapi_runtime.registerAnimation){
  __hapi_runtime.registerAnimation=__hapi_registerAnimation;
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
globalThis.hostApi=hostApi;
var __mod=globalThis.__module_default||__module_default;
if(typeof __mod==='function'){__mod(hostApi);}
"#;

pub fn host_api_shim_js() -> &'static str {
    SHIM_JS
}

pub const SHIM_JS_LIT: &str = SHIM_JS;
