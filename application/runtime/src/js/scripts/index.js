// Host API
globalThis.host = {
  emitEvent(name) { globalThis.__pendingEffects = globalThis.__pendingEffects || []; globalThis.__pendingEffects.push({ name: (name && typeof name === 'object' && typeof name.name === 'string') ? name.name : String(name), payload: {} }); },
  registerEvent(ev) { let n='unknown'; if(ev&&typeof ev==='object'){if(typeof ev.name==='string')n=ev.name;else if(ev.apply&&typeof ev.apply==='function'&&ev.apply.name)n=ev.apply.name;} globalThis.__registeredEvents = globalThis.__registeredEvents || []; globalThis.__registeredEvents.push(ev); },
  registerAction(ev) { let n='unknown'; if(ev&&typeof ev==='object'){if(typeof ev.name==='string')n=ev.name;else if(ev.apply&&typeof ev.apply==='function'&&ev.apply.name)n=ev.apply.name;} globalThis.__registeredActions = globalThis.__registeredActions || []; globalThis.__registeredActions.push(ev); },
  registerEffect(ev) { let n='unknown'; if(ev&&typeof ev==='object'){if(typeof ev.name==='string')n=ev.name;else if(ev.apply&&typeof ev.apply==='function'&&ev.apply.name)n=ev.apply.name;} globalThis.__registeredEffects = globalThis.__registeredEffects || []; globalThis.__registeredEffects.push(ev); },
  registerPanel(p) { try{var t=p;if(p&&typeof p==='object')t=JSON.stringify(p);else if(typeof p==='string')t=JSON.stringify({id:p});else t=JSON.stringify({id:String(p)});globalThis.__registeredPanels=globalThis.__registeredPanels||[];globalThis.__registeredPanels.push(t);}catch(e){}},
  createEntity(obj) { globalThis.__createdEntities = globalThis.__createdEntities || []; try{if(obj&&typeof obj==='object'&&typeof obj.firstName==='string')globalThis.__createdEntities.push({firstName:obj.firstName});else globalThis.__createdEntities.push(obj);}catch(e){} },
  setEntity(id,data) { globalThis.__entityData=globalThis.__entityData||{}; if(typeof id==='string'&&data&&typeof data==='object')globalThis.__entityData[id]=data; },
  log(msg) { try{globalThis.__logs=globalThis.__logs||[];globalThis.__logs.push(String(msg));}catch(e){} },
  number:{of:function(n){return n;}}, string:{of:function(s){return s;}}, texture:{of:function(t){return t;}} };
};

globalThis.string = { of: function(s) { return s; } };
globalThis.number = { of: function(n) { return n; } };

// Entity context
function buildEffectContext() {
  var hostApi = globalThis.host || {};
  function getEntityBy(filterFn) {
    var targetIds = [];
    if (filterFn && typeof filterFn.toString === 'function') {
      var src = filterFn.toString();
      var re1 = /string\.of\(\s*["']([^"']+)["']\s*\)/g;
      var m;
      while ((m = re1.exec(src)) !== null) targetIds.push(m[1]);
    }
    var matchedEntities = [];
    for (var i = 0; i < globalThis.__entityStore.length; i++) {
      var e = globalThis.__entityStore[i];
      if (!e || !e.textMap_name) continue;
      if (targetIds.length === 0 || targetIds.indexOf(e.textMap_name) >= 0) {
        matchedEntities.push(e);
      }
    }
    return {
      map: function(cb) {
        for (var j = 0; j < matchedEntities.length; j++) cb(makeEntityWrapper(matchedEntities[j]));
      },
      randomElement: function() {
        var ent = matchedEntities.length > 0 ? matchedEntities[0] : null;
        return { ifPresent: function(cb2) {
          if (ent) cb2(makeEntityWrapper(ent)); else cb2(null);
        }};
      }
    };
  }
  function makeValueWrapper(v, entityRef, keyRef) {
    return {
      map: function(cb) { cb(v); },
      sum: function(addend) {
        var nv = (v || 0) + addend;
        if (entityRef && entityRef.numberMap && keyRef) entityRef.numberMap[keyRef] = nv;
        return makeValueWrapper(nv, entityRef, keyRef);
      }
    };
  }
  function makeEntityWrapper(entity) {
    return {
      getNumber: function(key) {
        var val = null;
        if (entity.numberMap && entity.numberMap[key] !== undefined) val = entity.numberMap[key];
        return makeValueWrapper(val, entity, key);
      },
      getText: function(key) {
        var val = null;
        if (entity.textMap && entity.textMap[key] !== undefined) val = entity.textMap[key];
        return { concat: function(s) { return String(val || '') + s; }, map: function(cb) { cb(val); }, ifPresent: function(cb2) { if (val != null) cb2(String(val)); else cb2(null); }};
      }
    };
  }
  return {
    getEntityBy: getEntityBy,
    emitEffect: function(name, payload) { globalThis.__pendingEffects = globalThis.__pendingEffects || []; globalThis.__pendingEffects.push({ name: name, payload: payload }); },
    createEntity: function(obj) { globalThis.__createdEntities = globalThis.__createdEntities || []; globalThis.__entityStore.push(JSON.parse(JSON.stringify(obj))); },
    entity: { filter: { create: function() { return { byId: function(fn) { return fn; } }; } } },
    string: { of: function(s) { return s; } },
    number: { of: function(n) { return n; } }
  };
}

// Simulation template
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
