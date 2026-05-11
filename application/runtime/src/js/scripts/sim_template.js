(function(actionName, initialStore){
  globalThis.__entityStore = initialStore || [];
  globalThis.__createdEntities = globalThis.__createdEntities || [];
  const acts = globalThis.__registeredActions || [];
  globalThis.__logs = [];
  const evs = (globalThis.__registeredEvents || []).concat(globalThis.__registeredEffects || []);

  function recordCreated(obj) {
    if (obj && typeof obj === 'object') {
      const keys = Object.keys(obj);
      if (keys.length === 1) { const k=keys[0]; const v=String(obj[k]); const o={};o[k]=v;globalThis.__entityStore.push(o);globalThis.__createdEntities.push(v);return; }
      if (typeof obj.firstName==='string'){globalThis.__entityStore.push({firstName:obj.firstName});globalThis.__createdEntities.push(obj.firstName);return;}
    } else { globalThis.__entityStore.push({textMap_name:String(obj)});globalThis.__createdEntities.push(String(obj)); }
  }

  function logFn(msg){globalThis.__logs=globalThis.__logs||[];globalThis.__logs.push(String(msg));}

  function findEffectByName(name) { for(let e of evs){if(typeof e==='string'){if(e===name)return e;}else if(e&&typeof e==='object'){if(typeof e.name==='string'&&e.name===name)return e;}} return null;}

  function buildEventContext() {
    return { createEntity: recordCreated, log:logFn,getEntityBy: function(filter) { return { randomElement: function(){return{ifPresent:function(cb){let found=null;try{let src=filter.toString();let m=src.match(/isContainingExactly\(hostApi\.string\.of\("([^"]+)"\)\)/);if(m){const v=m[1];for(let i=0;i<globalThis.__entityStore.length;i++){const e=globalThis.__entityStore[i];for(let key in e){if(String(e[key]).includes(v)){found=e;break;}}if(found)break;}}}catch(e){}if(!found&&globalThis.__entityStore.length>0)found=globalThis.__entityStore[0];if(!found)return cb(null);const wrapper={getText:function(key){return{ifPresent:function(cb2){const nameObj={concat:function(s){try{if(found&&typeof found==='object'){if(key in found){found[key]=String(found[key])+String(s);}else{const pk=Object.keys(found)[0];if(pk)found[pk]=String(found[pk])+String(s);}}}catch(e){}}};cb2(nameObj);}}},ifPresent:function(cb3){cb3(wrapper);}};cb(wrapper);}}}};}};}

  function applyEffectByName(name, payload) { const ef=findEffectByName(name);if(!ef)return;let prepared;if(typeof ef.prepare==='function'){try{prepared=ef.prepare(payload);}catch(e){}}if(typeof ef.apply==='function'){try{ef.apply(buildEventContext(),prepared);}catch(e){}}}

  function emitEvent(name, payload) { globalThis.__pendingEffects=globalThis.__pendingEffects||[];globalThis.__pendingEffects.push({name: name,payload:payload}); }
  globalThis.__processPendingEffects=function(){const p=globalThis.__pendingEffects||[];globalThis.__pendingEffects=[];for(let i=0;i<p.length;i++)applyEffectByName(p[i].name,p[i].payload);};

  let actionObj=null;for(let a of acts){if(typeof a==='string'){if(a===actionName){actionObj=a;break;}}else if(a&&typeof a==='object'){if(typeof a.name==='string'&&a.name===actionName){actionObj=a;break;}}}

  if(actionObj){const wef=function(n,p){return emitEvent(n,p);};const ctx_obj={emitEffect:wef,emitEvent:wef,createEntity:recordCreated,log:logFn,entity:{create:()=>({withTextMap:tm=>tm}),filter:{create:()=>({byId:fn=>fn})}},textMap:{create:()=>({put:(k,v)=>{const o={};o[k]=v;return o}})},string:{of:s=>s},number:{of:n=>n}};try{if(typeof actionObj==='object'&&typeof actionObj.apply==='function')actionObj.apply(ctx_obj);else if(typeof actionObj==='function')actionObj(ctx_obj);}catch(e){}}

  return JSON.stringify({created:globalThis.__createdEntities,store:globalThis.__entityStore,pendingEffects:globalThis.__pendingEffects||[]});
})(ACTION_PLACEHOLDER, STORE_PLACEHOLDER)
