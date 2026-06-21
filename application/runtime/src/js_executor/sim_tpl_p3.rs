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
  if (actionObj) {
    const wrappedEmit = function(name, payload) { return emitEvent(name, payload); };
    const ctx = { emitEffect: wrappedEmit, emitEvent: wrappedEmit,
      createEntity: recordCreated,
      entity: { create: ()=>({ withTextMap: tm => tm }) },
      textMap: { create: ()=>({ put: (k,v)=>{ const o={}; o[k]=v; return o; } }) },
      string: { of: s => s }};
    try {
      if (typeof actionObj === 'object' && typeof actionObj.apply === 'function') actionObj.apply(ctx);
      else if (typeof actionObj === 'function') { try { actionObj(ctx); } catch(e) {} }
    } catch(e) {}
  }
  return JSON.stringify({ created: globalThis.__createdEntities,
    store: globalThis.__entityStore,
    pendingEffects: globalThis.__pendingEffects || [] });
})(ACTION_PLACEHOLDER, STORE_PLACEHOLDER)"#;

pub fn get_part3() -> &'static str { SIM_TPL_P3 }
