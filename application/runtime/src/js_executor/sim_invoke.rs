pub fn get_invoke_js() -> &'static str {
    r#"
var h=globalThis.host;
if(!h){throw new Error("host is undefined");}
var hostApi={
  ui:{
    texture:{of:function(p){return p;}},
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
"#
}
