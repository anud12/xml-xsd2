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
var __mod=globalThis.__module_default||__module_default;
if(typeof __mod==='function'){__mod(hostApi);}
"#
}
