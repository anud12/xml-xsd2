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

    // Return DIRECT REFERENCES so mutations propagate.
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

  function makeValueWrapper(initialV, entityRef, keyRef) {
    var current = { value: initialV };
    function readValue() {
      return current.value !== null && current.value !== undefined ? current.value : 0;
    }
    var self;
    function sum(addend) {
      var nv = readValue() + addend;
      current.value = nv;
      if (entityRef && entityRef.numberMap && keyRef) entityRef.numberMap[keyRef] = nv;
      return self;
    }
    self = {
      map: function(cb) { cb(self); },
      sum: sum
    };
    return self;
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
