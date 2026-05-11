// Execute all registered effects
var regEffects = globalThis.__registeredEvents || [];
for (var i = 0; i < regEffects.length; i++) {
  if (regEffects[i] && typeof regEffects[i].apply === 'function') {
    try {
      var efCtx = buildEffectContext();
      var prepared = null;
      if (typeof regEffects[i].prepare === 'function') {
        try { prepared = regEffects[i].prepare({}); } catch(ex) {}
      }
      regEffects[i].apply(efCtx, prepared);
    } catch(e) {}
  }
}

// Sync entity store back to __entityData
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

// Extract declarations
var out = {
  events: [], actions: [], functions: [], entities: [],
  creators: {}, emits: {}, panels: [], entity_data: {}
};
var re = globalThis.__registeredEvents || [];
out.events = re.map(function(ev) {
  if (typeof ev === 'string') return ev;
  if (ev && typeof ev === 'object') {
    if (typeof ev.name === 'string') return ev.name;
    if (ev.apply && typeof ev.apply === 'function' && ev.apply.name) return ev.apply.name;
    try { return JSON.stringify(ev); } catch(e) { return String(ev); }
  }
  return String(ev);
});
var ra = globalThis.__registeredActions || [];
out.actions = ra.map(function(ev) {
  if (typeof ev === 'string') return ev;
  if (ev && typeof ev === 'object') {
    if (typeof ev.name === 'string') return ev.name;
    if (ev.apply && typeof ev.apply === 'function' && ev.apply.name) return ev.apply.name;
    try { return JSON.stringify(ev); } catch(e) { return String(ev); }
  }
  return String(ev);
});
var ce = globalThis.__createdEntities || [];
out.entities = ce.map(function(en) {
  if (typeof en === 'string') return en;
  if (en && typeof en === 'object') {
    if (typeof en.firstName === 'string') return en.firstName;
    try { return JSON.stringify(en); } catch(e) { return String(en); }
  }
  return String(en);
});
out.logs = globalThis.__logs || [];
out.functions = Object.getOwnPropertyNames(globalThis).filter(function(k) {
  try { return typeof globalThis[k] === 'function' && !k.startsWith('_') && k !== 'host'; }
  catch(e) { return false; }
}).sort();
out.creators = globalThis.__createdEntitiesFor || {};
out.emits = globalThis.__emitsMap || {};
out.panels = globalThis.__registeredPanels || [];
out.entity_data = globalThis.__entityData || {};

JSON.stringify(out)
