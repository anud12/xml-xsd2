pub fn get_invoke_js() -> &'static str {
    r#"
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
  // World/sector namespace. `sectorGrid(id)` declares a named grid; sectors
  // attach later via a container's optional `sector` field (captured in
  // setContainer). The returned object only needs a stable id reference.
  world:{
    sectorGrid:function(id){
      var resolvedId=typeof id==='object'?id.value:id;
      return { id: resolvedId, sectorGrid: true };
    }
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
// Record behavior attachments (setEntity with a behavior field) so the
// per-tick behavior machine can drive each entity's step script.
var __origSetEntity=hostApi.runtime.setEntity;
hostApi.runtime.setEntity=function(id,data){
  var resolvedId=typeof id==='object'?id.value:id;
  if(data&&typeof data==='object'&&data.behavior!==undefined){
    var b=data.behavior;
    var bName=typeof b==='object'?b.name:b;
    var bVal=typeof b==='object'?b:(b!==undefined?{name:b}:undefined);
    globalThis.__behaviors=globalThis.__behaviors||{};
    globalThis.__behaviors[resolvedId]=bVal;
    globalThis.__logs=globalThis.__logs||[];
    globalThis.__logs.push('behavior attached: '+resolvedId+' -> '+(typeof bName==='object'?bName.name:bName));
  }
  if(__origSetEntity) return __origSetEntity(id,data);
  return undefined;
};
globalThis.hostApi=hostApi;
// Extraction only needs the module to run to completion so its runtime
// declarations (containers/entities/actions) are baked; UI nodes are captured
// by the persistent sim install path, not here. Provide no-op ui factories so
// the module does not abort on hostApi.ui.panel/field/containerView.
(function(){
  var u=hostApi.ui;
  var noop=function(){return null;};
  if(!u.panel)u.panel=noop;
  if(!u.window)u.window=noop;
  if(!u.div)u.div=noop;
  if(!u.field)u.field=noop;
  if(!u.text)u.text=noop;
  if(!u.image)u.image=noop;
  if(!u.canvas)u.canvas=noop;
  if(!u.containerView)u.containerView=noop;
  if(!u.entityList)u.entityList=noop;
  if(!u.getSpritePNG)u.getSpritePNG=function(p){return p;};
  if(!u.getAnimation)u.getAnimation=function(){return null;};
})();
var __mod=globalThis.__module_default||__module_default;
if(typeof __mod==='function'){__mod(hostApi);}
"#
}
