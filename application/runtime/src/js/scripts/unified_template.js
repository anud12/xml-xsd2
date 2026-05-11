// Unified runtime template - __USER_SOURCE__ is replaced at compile time
globalThis.host = {
emitEvent(name) { globalThis.__pendingEffects = globalThis.__pendingEffects || []; globalThis.__pendingEffects.push({ name: (name && typeof name === 'object' && typeof name.name === 'string') ? name.name : String(name), payload: {} }); },
registerEvent(ev) { let n='unknown'; if(ev&&typeof ev==='object'){if(typeof ev.name==='string')n=ev.name;else if(ev.apply&&typeof ev.apply==='function'&&ev.apply.name)n=ev.apply.name;} globalThis.__registeredEvents = globalThis.__registeredEvents || []; globalThis.__registeredEvents.push(ev); },
registerAction(ev) { let n='unknown'; if(ev&&typeof ev==='object'){if(typeof ev.name==='string')n=ev.name;else if(ev.apply&&typeof ev.apply==='function'&&ev.apply.name)n=ev.apply.name;} globalThis.__registeredActions = globalThis.__registeredActions || []; globalThis.__registeredActions.push(ev); },
registerEffect(ev) { let n='unknown'; if(ev&&typeof ev==='object'){if(typeof ev.name==='string')n=ev.name;else if(ev.apply&&typeof ev.apply==='function'&&ev.apply.name)n=ev.apply.name;} globalThis.__registeredEvents = globalThis.__registeredEvents || []; globalThis.__registeredEvents.push(ev); },
registerPanel(p) { try{var t=p;if(p&&typeof p==='object')t=JSON.stringify(p);else if(typeof p==='string')t=JSON.stringify({id:p});else t=JSON.stringify({id:String(p)});globalThis.__registeredPanels=globalThis.__registeredPanels||[];globalThis.__registeredPanels.push(t);}catch(e){}},
createEntity(obj) { globalThis.__createdEntities = globalThis.__createdEntities || []; try{if(obj&&typeof obj==='object'&&typeof obj.firstName==='string')globalThis.__createdEntities.push({firstName:obj.firstName});else globalThis.__createdEntities.push(obj);}catch(e){} },
setEntity(id,data) { globalThis.__entityData=globalThis.__entityData||{}; if(typeof id==='string'&&data&&typeof data==='object')globalThis.__entityData[id]=data; },
log(msg) { try{globalThis.__logs=globalThis.__logs||[];globalThis.__logs.push(String(msg));}catch(e){} },
number:{of:function(n){return n;}}, string:{of:function(s){return s;}}, texture:{of:function(t){return t;}} };

// Top-level globals for user module code that references string.of() / number.of() directly.
globalThis.string = { of: function(s) { return s; } };
globalThis.number = { of: function(n) { return n; } };

// Provide hostApi as a global alias so patched user source ({...hostApi}) works
var hostApi = globalThis.host;

// User module source will be injected here at compile time
__USER_SOURCE__

globalThis.__entityData = globalThis.__entityData || {};
(globalThis.__logs||[]).push("DEBUG: about to call localSetEntity");
function localSetEntity(id, data) {
  (globalThis.__logs||[]).push("DEBUG: localSetEntity called with id=" + id);
  if (typeof id === 'string' && data && typeof data === 'object') {
    globalThis.__entityData[id] = JSON.parse(JSON.stringify(data));
  }
}

(globalThis.__logs||[]).push("DEBUG: __module_default type=" + (typeof __module_default) + ", isFunction=" + (typeof __module_default === 'function'));
if (typeof __module_default === 'function') {
  try {
    var unifiedHostApi = {
      string: globalThis.string,
      number: globalThis.number,
      entity: { create: function() { return { withTextMap: function(tm){ return tm; } }; }, filter: { create: function() { return { byId: function(fn){ return fn; } }; } } },
      textMap: { create: function() { return { put: function(k,v){ const o={}; o[k]=v; return o; } }; } },
      texture: { of: function(p){ return p; } },
      emitEvent: host.emitEvent, registerEvent: host.registerEvent, registerAction: host.registerAction,
      registerEffect: host.registerEffect, registerPanel: host.registerPanel,
      setEntity: localSetEntity, log: host.log
    };
    globalThis.__moduleHostApi = unifiedHostApi;
    (globalThis.__logs||[]).push("DEBUG: calling __module_default with unifiedHostApi");
    __module_default(unifiedHostApi);
    (globalThis.__logs||[]).push("DEBUG: after __module_default, registeredEvents=" + JSON.stringify(globalThis.__registeredEvents || []));
  } catch(e) {
    (globalThis.__logs||[]).push("DEBUG ERROR calling __module_default: " + e.message);
  }
} else { globalThis.__moduleHostApi = host; }

// Build entity store from __entityData. Use direct refs for mutation propagation.
globalThis.__entityStore = [];
if (globalThis.__entityData) {
  for (var id in globalThis.__entityData) {
    var entryObj = globalThis.__entityData[id];
    entryObj.textMap_name = id;
    globalThis.__entityStore.push(entryObj);
  }
}

// Effect context function
__EFFECT_CONTEXT__

// Execute all registered effects.
var regEffects = globalThis.__registeredEvents || [];
(globalThis.__logs||[]).push("DEBUG_EFFECTS: registered count=" + (regEffects ? regEffects.length : "null"));
for (var i = 0; i < regEffects.length; i++) {
  (globalThis.__logs||[]).push("DEBUG_EFFECTS: entry[" + i + "]=" + JSON.stringify(regEffects[i]));
  if (regEffects[i] && typeof regEffects[i].apply === 'function') {
    try {
      var efCtx = buildEffectContext();
      var prepared = null;
      if (typeof regEffects[i].prepare === 'function') {
        try { prepared = regEffects[i].prepare({}); } catch(ex) {}
      }
      (globalThis.__logs||[]).push("DEBUG_EFFECTS: calling apply for '" + regEffects[i].name + "'");
      regEffects[i].apply(efCtx, prepared);
      (globalThis.__logs||[]).push("DEBUG_EFFECTS: __entityData after effect=" + JSON.stringify(globalThis.__entityData));
    } catch(e) { (globalThis.__logs||[]).push("DEBUG_EFFECTS: error in effect '" + regEffects[i].name + "': " + e.message); }
  } else { (globalThis.__logs||[]).push("DEBUG_EFFECTS: skipping entry, hasApply=" + (regEffects[i] && typeof regEffects[i].apply === 'function')); }
}

// Sync entity store changes back to __entityData after effects ran.
if (globalThis.__entityData && globalThis.__entityStore) {
  for (var k in globalThis.__entityData) {
    for (var i = 0; i < globalThis.__entityStore.length; i++) {
      var entry = globalThis.__entityStore[i];
      if (entry && entry.textMap_name === k) {
        globalThis.__entityData[k] = JSON.parse(JSON.stringify(entry));
      }
    }
  }
}

// Extract declarations and return as JSON string.
var out = { events: [], actions: [], functions: [], entities: [], creators: {}, emits: {}, panels: [], entity_data: {} };
var re = globalThis.__registeredEvents || [];
out.events = re.map(function(ev) { if (typeof ev === 'string') return ev; if (ev && typeof ev === 'object') { if (typeof ev.name === 'string') return ev.name; if (ev.apply && typeof ev.apply === 'function' && ev.apply.name) return ev.apply.name; try { return JSON.stringify(ev); } catch(e) { return String(ev); } } return String(ev); });
var ra = globalThis.__registeredActions || [];
out.actions = ra.map(function(ev) { if (typeof ev === 'string') return ev; if (ev && typeof ev === 'object') { if (typeof ev.name === 'string') return ev.name; if (ev.apply && typeof ev.apply === 'function' && ev.apply.name) return ev.apply.name; try { return JSON.stringify(ev); } catch(e) { return String(ev); } } return String(ev); });
var ce = globalThis.__createdEntities || [];
out.entities = ce.map(function(en) { if (typeof en === 'string') return en; if (en && typeof en === 'object') { if (typeof en.firstName === 'string') return en.firstName; try { return JSON.stringify(en); } catch(e) { return String(en); } } return String(en); });
out.logs = globalThis.__logs || [];
out.functions = Object.getOwnPropertyNames(globalThis).filter(function(k) { try { return typeof globalThis[k] === 'function' && !k.startsWith('_') && k !== 'host'; } catch(e) { return false; } }).sort();
out.creators = globalThis.__createdEntitiesFor || {};
out.emits = globalThis.__emitsMap || {};
out.panels = globalThis.__registeredPanels || [];
out.entity_data = globalThis.__entityData || {};

JSON.stringify(out)
