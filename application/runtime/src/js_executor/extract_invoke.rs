pub fn get_invoke_js() -> &'static str {
    r#"
var h=globalThis.host;
if(!h){throw new Error("host is undefined");}
var hostApi={
  string:{of:function(s){return s;}},
  number:{of:function(n){return n;}},
  texture:{of:function(p){return p;}},
  emitEvent:h.emitEvent,
  registerEvent:h.registerEvent,
  registerAction:h.registerAction,
  registerEffect:h.registerEffect,
  registerPanel:h.registerPanel,
  setEntity:h.setEntity,
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
};
globalThis.hostApi=hostApi;
var __mod=globalThis.__module_default||__module_default;
if(typeof __mod==='function'){__mod(hostApi);}
"#
}
