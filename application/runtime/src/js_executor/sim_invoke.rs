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
"#
}
