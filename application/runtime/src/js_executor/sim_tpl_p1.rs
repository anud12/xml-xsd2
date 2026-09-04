// sim_template part 1: setup, recordCreated, findEffectByName

const SIM_TPL_P1: &str = r#"(function(actionName, initialStore, actionArgs){
  globalThis.__entityStore = initialStore || [];
  globalThis.__createdEntities = globalThis.__createdEntities || [];
  const acts = globalThis.__registeredActions || [];
  globalThis.__logs = [];
  const evs = globalThis.__registeredEvents || [];
  function recordCreated(obj) {
    if (obj && typeof obj === 'object') {
      const keys = Object.keys(obj);
      if (keys.length === 1) {
        const k = keys[0]; const v = String(obj[k]);
        const o = {}; o[k]=v; globalThis.__entityStore.push(o);
        globalThis.__createdEntities.push(v); return;
      }
      if (typeof obj.firstName === 'string') {
        globalThis.__entityStore.push({ firstName: obj.firstName });
        globalThis.__createdEntities.push(obj.firstName); return;
      }
      try { globalThis.__entityStore.push({ textMap_name: JSON.stringify(obj) });
        globalThis.__createdEntities.push(JSON.stringify(obj));
      } catch(e) { globalThis.__entityStore.push({ textMap_name: String(obj) });
        globalThis.__createdEntities.push(String(obj)); }
    } else {
      globalThis.__entityStore.push({ textMap_name: String(obj) });
      globalThis.__createdEntities.push(String(obj));
    }
  }
  function findEffectByName(name) {
    for (let e of evs) {
      if (typeof e === 'string') { if (e === name) return e; }
      else if (e && typeof e === 'object') {
        if (typeof e.name === 'string' && e.name === name) return e;
        if (e.apply && typeof e.apply === 'function'
            && e.apply.name === name) return e;
      }
    }
    return null;
  }
  const __actionArgsObj = {};
  if (Array.isArray(actionArgs)) {
    for (const pair of actionArgs) {
      if (pair && pair.length >= 2) __actionArgsObj[pair[0]] = pair[1];
    }
  } else if (actionArgs && typeof actionArgs === 'object') {
    for (const [k, v] of Object.entries(actionArgs)) __actionArgsObj[k] = v;
  }
  Object.freeze(__actionArgsObj);
  globalThis.__actionArgsObj = __actionArgsObj;
  globalThis.__actionArgs = __actionArgsObj;"#;

pub fn get_part1() -> &'static str { SIM_TPL_P1 }
