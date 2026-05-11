globalThis.host = {
  emitEvent(name) {
    globalThis.__pendingEffects = globalThis.__pendingEffects || [];
    globalThis.__pendingEffects.push({
      name: (name && typeof name === 'object' && typeof name.name === 'string') ? name.name : String(name),
      payload: {}
    });
    globalThis.__logs = globalThis.__logs || [];
    globalThis.__logs.push('DEBUG: emitEvent called');
    if (name && typeof name === 'object' && typeof name.name === 'string') {
      globalThis.__logs.push(`event: ${name.name}`);
    } else {
      globalThis.__logs.push(`event: ${String(name)}`);
    }
  },
  registerEvent(ev) {
    let n = 'unknown';
    if (ev && typeof ev === 'object') {
      if (typeof ev.name === 'string') n = ev.name;
      else if (ev.apply && typeof ev.apply === 'function' && ev.apply.name) n = ev.apply.name;
    } else if (typeof ev === 'string') {
      n = ev;
    }
    globalThis.__registeredEvents = globalThis.__registeredEvents || [];
    globalThis.__registeredEvents.push(ev);
    globalThis.__logs = globalThis.__logs || [];
    globalThis.__logs.push(`Events registered: ${n}`);
  },
  registerAction(ev) {
    let n = 'unknown';
    if (ev && typeof ev === 'object') {
      if (typeof ev.name === 'string') n = ev.name;
      else if (ev.apply && typeof ev.apply === 'function' && ev.apply.name) n = ev.apply.name;
    } else if (typeof ev === 'string') {
      n = ev;
    }
    globalThis.__registeredActions = globalThis.__registeredActions || [];
    globalThis.__registeredActions.push(ev);
    globalThis.__logs = globalThis.__logs || [];
    globalThis.__logs.push(`Actions registered: ${n}`);
  },
  registerEffect(ev) {
    let n = 'unknown';
    if (ev && typeof ev === 'object') {
      if (typeof ev.name === 'string') n = ev.name;
      else if (ev.apply && typeof ev.apply === 'function' && ev.apply.name) n = ev.apply.name;
    } else if (typeof ev === 'string') {
      n = ev;
    }
    globalThis.__registeredEvents = globalThis.__registeredEvents || [];
    globalThis.__registeredEvents.push(ev);
    globalThis.__logs = globalThis.__logs || [];
    globalThis.__logs.push(`Effects registered: ${n}`);
  },
  registerPanel(p) {
    try {
      var toPush = p;
      if (p && typeof p === 'object') {
        toPush = JSON.stringify(p);
      } else if (typeof p === 'string') {
        toPush = JSON.stringify({ id: p });
      } else {
        toPush = JSON.stringify({ id: String(p) });
      }
      globalThis.__registeredPanels = globalThis.__registeredPanels || [];
      globalThis.__registeredPanels.push(toPush);
    } catch (e) { /* ignore */ }
  },
  createEntity(obj) {
    globalThis.__createdEntities = globalThis.__createdEntities || [];
    try {
      if (obj && typeof obj === 'object' && typeof obj.firstName === 'string') {
        globalThis.__createdEntities.push({ firstName: obj.firstName });
        globalThis.__logs = globalThis.__logs || [];
        globalThis.__logs.push(`entity created: ${obj.firstName}`);
      } else {
        globalThis.__createdEntities.push(obj);
        globalThis.__logs = globalThis.__logs || [];
        globalThis.__logs.push(`entity created: ${String(obj)}`);
      }
    } catch (e) {
      globalThis.__createdEntities.push(String(obj));
      globalThis.__logs = globalThis.__logs || [];
      globalThis.__logs.push(`entity created: ${String(obj)}`);
    }
  },
  setEntity(id, data) {
    globalThis.__entityData = globalThis.__entityData || {};
    if (typeof id === 'string' && data && typeof data === 'object') {
      globalThis.__entityData[id] = data;
    }
  },
  log(msg) {
    try {
      globalThis.__logs = globalThis.__logs || [];
      globalThis.__logs.push(String(msg));
    } catch (e) { }
  },
  number: { of: function(n) { return n; } },
  string: { of: function(s) { return s; } },
  texture: { of: function(t) { return t; } },
  entity: {
    filter: {
      create: function() {
        return { byId: function(fn) { return fn; } };
      }
    }
  }
};

globalThis.createEntity = function(o) { return globalThis.host.createEntity(o); };
globalThis.entity = globalThis.entity || {};
globalThis.entity.create = function(o) { return globalThis.host.createEntity(o); };
function string_of(s) { return s; }
globalThis.string = { of: function(s) { return s; } };
globalThis.number = { of: function(n) { return n; } };
