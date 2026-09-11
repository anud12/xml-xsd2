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
  let actionObj = null;
  for (let a of acts) {
    if (typeof a === 'string') { if (a === actionName) { actionObj = a; break; } }
    else if (a && typeof a === 'object') {
      if (typeof a.name === 'string' && a.name === actionName) { actionObj = a; break; }
      if (a.apply && typeof a.apply === 'function' && a.apply.name === actionName) { actionObj = a; break; }
    }
  }
  globalThis.__plannedMoves = globalThis.__plannedMoves || [];
  if (actionObj) {
    const wrappedEmit = function(name, payload) { return emitEvent(name, payload); };
    const ctx = { emitEffect: wrappedEmit, emitEvent: wrappedEmit,
      createEntity: recordCreated,
      entity: { create: ()=>({ withTextMap: tm => tm }) },
      textMap: { create: ()=>({ put: (k,v)=>{ const o={}; o[k]=v; return o; } }) },
      string: { of: s => s },
      // Parks a move plan for the actor. The walker (process_active_plans)
      // advances it one cell per tick, re-parking until the target is reached.
      moveTo: function(spec) {
        var s = spec || {};
        var c = (typeof s.containerId === 'object' && s.containerId !== null) ? s.containerId.value : s.containerId;
        var e = (typeof s.entityId === 'object' && s.entityId !== null) ? s.entityId.value : s.entityId;
        var sp = s.speed;
        if (typeof sp === 'object' && sp !== null) sp = sp.value;
        globalThis.__plannedMoves.push({
          containerId: c, entityId: e,
          x: Number(s.x), y: Number(s.y), speed: Number(sp) || 0
        });
      },
      teleportTo: function(spec) {
        // A teleport is a zero-length, immediate move: write the position now.
        var s = spec || {};
        var c = (typeof s.containerId === 'object' && s.containerId !== null) ? s.containerId.value : s.containerId;
        var e = (typeof s.entityId === 'object' && s.entityId !== null) ? s.entityId.value : s.entityId;
        globalThis.__plannedMoves.push({
          containerId: c, entityId: e,
          x: Number(s.x), y: Number(s.y), speed: Infinity, teleport: true
        });
      },
      allowInterrupt: function() { globalThis.__interruptible = true; },
      denyInterrupt: function() { globalThis.__interruptible = false; },
      args: globalThis.__actionArgs || {} };
    try {
      if (typeof actionObj === 'object' && typeof actionObj.apply === 'function') actionObj.apply(ctx);
      else if (typeof actionObj === 'function') { try { actionObj(ctx); } catch(e) {} }
    } catch(e) {}
  }
  return JSON.stringify({ created: globalThis.__createdEntities,
    store: globalThis.__entityStore,
    pendingEffects: globalThis.__pendingEffects || [],
    plannedMoves: (globalThis.__plannedMoves || []).map(function(m){ return m; }),
    interruptible: !!globalThis.__interruptible });
})(ACTION_PLACEHOLDER, STORE_PLACEHOLDER)"#;

pub fn get_part3() -> &'static str { SIM_TPL_P3 }
