// sim_template part 3: applyEffectByName, emitEvent, action, return

const SIM_TPL_P3: &str = r#"
  function applyEffectByName(name, payload) {
    const ef = findEffectByName(name);
    if (!ef) return;
    let prepared;
    if (typeof ef.prepare === 'function') { try { prepared = ef.prepare(payload); } catch(e) {} }
    if (typeof ef.apply === 'function') { try { ef.apply(buildEventContext(), prepared); } catch(e) {} }
  }
  function emitEvent(name, payload) {
    globalThis.__pendingEffects = globalThis.__pendingEffects || [];
    globalThis.__pendingEffects.push({ name: name, payload: payload });
  }
  globalThis.__processPendingEffects = function() {
    const pending = globalThis.__pendingEffects || [];
    globalThis.__pendingEffects = [];
    for (let i = 0; i < pending.length; i++) applyEffectByName(pending[i].name, pending[i].payload);
  };
  function extractPositionKey(fn) {
    if (typeof fn !== 'function') return null;
    var m = String(fn).match(/get\(\s*["']([^"']+)["']\s*\)/);
    return m ? m[1] : null;
  }
  function reBakeContainer(cid) {
    var c = (globalThis.__containerData || {})[cid];
    if (!c) return;
    var out = { id: cid };
    if (c.entities) out.entities = c.entities.map(function(e) { return String(e); });
    if (c.textMap) out.textMap = c.textMap;
    if (c.numberMap) out.numberMap = c.numberMap;
    if (typeof c.getX === 'function') out.getX = globalThis.evalPositionFn(c.getX);
    if (typeof c.getY === 'function') out.getY = globalThis.evalPositionFn(c.getY);
    if (typeof c.getSpanX === 'function') out.getSpanX = globalThis.evalPositionFn(c.getSpanX);
    if (typeof c.getSpanY === 'function') out.getSpanY = globalThis.evalPositionFn(c.getSpanY);
    if (c.sizeX) out.sizeX = { value: c.sizeX.value, outOfBounds: c.sizeX.outOfBounds };
    if (c.sizeY) out.sizeY = { value: c.sizeY.value, outOfBounds: c.sizeY.outOfBounds };
    var serialized = JSON.stringify(out);
    var reg = globalThis.__registeredContainers || (globalThis.__registeredContainers = []);
    var idx = -1;
    for (var i = 0; i < reg.length; i++) {
      try {
        var parsed = JSON.parse(reg[i]);
        if (parsed && parsed.id === cid) { idx = i; break; }
      } catch(e) {}
    }
    if (idx >= 0) reg[idx] = serialized; else reg.push(serialized);
  }
  function teleportTo(o) {
    if (!o || typeof o !== 'object') return;
    var cid = (typeof o.containerId === 'object' && o.containerId !== null)
      ? o.containerId.value : o.containerId;
    var eid = (typeof o.entityId === 'object' && o.entityId !== null)
      ? o.entityId.value : o.entityId;
    cid = String(cid); eid = String(eid);
    if (!cid || !eid) return;
    var c = (globalThis.__containerData || {})[cid];
    if (!c) return;
    var ents = c.entities || [];
    var inContainer = false;
    for (var i = 0; i < ents.length; i++) {
      if (String(ents[i]) === eid) { inContainer = true; break; }
    }
    if (!inContainer) return;
    var x = Number(o.x), y = Number(o.y);
    if (o.clamp) {
      if (c.sizeX && Number.isFinite(Number(c.sizeX.value)))
        x = Math.max(0, Math.min(x, Number(c.sizeX.value)));
      if (c.sizeY && Number.isFinite(Number(c.sizeY.value)))
        y = Math.max(0, Math.min(y, Number(c.sizeY.value)));
    }
    var keyX = extractPositionKey(c.getX);
    var keyY = extractPositionKey(c.getY);
    var e = (globalThis.__entityData || {})[eid];
    if (!e) return;
    if (!e.numberMap) e.numberMap = {};
    if (keyX !== null) e.numberMap[keyX] = x;
    if (keyY !== null) e.numberMap[keyY] = y;
    reBakeContainer(cid);
  }
  let actionObj = null;
  for (let a of acts) {
    if (typeof a === 'string') { if (a === actionName) { actionObj = a; break; } }
    else if (a && typeof a === 'object') {
      if (typeof a.name === 'string' && a.name === actionName) { actionObj = a; break; }
      if (a.apply && typeof a.apply === 'function' && a.apply.name === actionName) { actionObj = a; break; }
    }
  }
  // apply is a declaration phase: emitEffect/wait/moveTo record plan steps;
  // the recorded plan is walked once, immediately up to the first wait or move.
  const plan = [];
  let activePlan = null;
  if (actionObj) {
    const wrappedEmit = function(name, payload) {
      plan.push({ emit: { name: String(name), payload: payload } });
    };
    const wrappedWait = function(duration) {
      let g = 0;
      if (typeof duration === 'number') g = duration;
      else if (duration && typeof duration === 'object') {
        if (typeof duration.ticks === 'number') g = duration.ticks;
        else if (typeof duration.value === 'number') g = duration.value;
      }
      if (g < 0 || !isFinite(g)) g = 0;
      globalThis.__logs = globalThis.__logs || [];
      globalThis.__logs.push('plan: wait ' + g + ' GTU');
      plan.push({ wait: g });
    };
    const numVal = function(v) {
      if (typeof v === 'number') return v;
      if (v && typeof v === 'object') {
        if (typeof v.value === 'number') return v.value;
        if (typeof v.n === 'number') return v.n;
      }
      return NaN;
    };
    const wrappedMoveTo = function(o) {
      const step = { move: {
        containerId: (o && o.containerId) || '',
        entityId: (o && o.entityId) || '',
        x: (o && o.x) || 0,
        y: (o && o.y) || 0,
        speed: (o && o.speed) || 0
      }};
      globalThis.__logs = globalThis.__logs || [];
      globalThis.__logs.push('plan: move to ' + step.move.x + ',' + step.move.y
        + ' speed ' + numVal(step.move.speed));
      plan.push(step);
    };
    const ctx = { emitEffect: wrappedEmit, emitEvent: wrappedEmit,
      wait: wrappedWait,
      moveTo: wrappedMoveTo,
      allowInterrupt: function() { plan.push({ interruptible: true }); },
      denyInterrupt: function() { plan.push({ interruptible: false }); },
      createEntity: recordCreated,
      teleportTo: teleportTo,
      actor: { containers: [] },
      entity: { create: ()=>({ withTextMap: tm => tm }) },
      textMap: { create: ()=>({ put: (k,v)=>{ const o={}; o[k]=v; return o; } }) },
      string: { of: s => s }};
    try {
      if (typeof actionObj === 'object' && typeof actionObj.apply === 'function') actionObj.apply(ctx);
      else if (typeof actionObj === 'function') { try { actionObj(ctx); } catch(e) {} }
    } catch(e) {}
    let currentInterruptible = false;
    for (let i = 0; i < plan.length; i++) {
      const st = plan[i];
      if (st && typeof st.interruptible === 'boolean') {
        currentInterruptible = st.interruptible;
      } else if (st && st.emit) {
        emitEvent(st.emit.name, st.emit.payload);
      } else if (st && typeof st.wait === 'number') {
        activePlan = { wait: st.wait, steps: plan.slice(i + 1), interruptible: currentInterruptible };
        break;
      } else if (st && st.move) {
        // The move step is walked by the Rust walker across ticks; park on it
        // immediately (wait 0 resumes it this tick) so dispatch never consumes it.
        activePlan = { wait: 0, steps: plan, interruptible: currentInterruptible };
        break;
      }
    }
  }
  return JSON.stringify({ created: globalThis.__createdEntities,
    store: globalThis.__entityStore,
    pendingEffects: globalThis.__pendingEffects || [],
    containers: globalThis.__registeredContainers || [],
    activePlan: activePlan });
})(ACTION_PLACEHOLDER, STORE_PLACEHOLDER)"#;

pub fn get_part3() -> &'static str { SIM_TPL_P3 }
