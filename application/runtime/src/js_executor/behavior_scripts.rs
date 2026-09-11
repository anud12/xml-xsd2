use anyhow::Result;
use super::sim_ctx;

/// Drives attached behavior step scripts in the persistent sim context.
/// The module install already populated `__behaviorDefinitions`,
/// `__behaviors` (setEntity attachments) and `__registeredActions`; the
/// per-entity step machine (`__behaviorScripts`) persists across ticks in
/// the same context and is reset when a new archive is installed.
const BEHAVIOR_MACHINE_JS: &str = r#"(function(total){
  globalThis.__logs = [];
  globalThis.__behaviorScripts =
      globalThis.__behaviorScripts || {};
  const defs = globalThis.__behaviorDefinitions || {};
  const atts = globalThis.__behaviors || {};
  const acts = globalThis.__registeredActions || [];
  function findAction(name) {
    for (let i = 0; i < acts.length; i++) {
      const a = acts[i];
      if (a && typeof a === 'object'
          && typeof a.name === 'string' && a.name === name) return a;
    }
    return null;
  }
  for (const entityId in atts) {
    const handle = atts[entityId];
    if (!handle) continue;
    const handleName = typeof handle.name === 'object'
      ? handle.name.value : handle.name;
    const def = handleName && defs[handleName];
    if (!def) continue;
    const branch = def.priority && def.priority[0];
    const rule = branch && branch.utility && branch.utility[0];
    if (!rule || !Array.isArray(rule.steps)) continue;
    const st = globalThis.__behaviorScripts[entityId]
      || (globalThis.__behaviorScripts[entityId]
         = { stepIdx: 0, waitUntil: null });
    const steps = rule.steps;
    while (st.stepIdx < steps.length) {
      const step = steps[st.stepIdx];
      if (step && step.wait !== undefined) {
        if (st.waitUntil === null) {
          st.waitUntil = total + Number(step.wait);
        }
        if (total < st.waitUntil) break;
        st.waitUntil = null;
        st.stepIdx++;
      } else if (step && step.action !== undefined) {
        const act = findAction(step.action);
        if (act && typeof act.apply === 'function') {
          try { act.apply({}); } catch (e) {}
        }
        st.stepIdx++;
      } else {
        st.stepIdx++;
      }
    }
  }
  globalThis.__behaviorResult = {
    logs: globalThis.__logs || []
  };
  return globalThis.__behaviorResult;
})"#;

pub fn process_behavior_machine(total: i64) -> Result<()> {
    let Some(ctx) = sim_ctx::ctx() else { return Ok(()); };
    let invoke = format!("({})({})", BEHAVIOR_MACHINE_JS, total);
    if let Err(e) = sim_ctx::sim_with(ctx, |c| c.eval::<(), _>(invoke)) {
        runtime_log!("behavior machine failed: {:?}", e);
        return Ok(());
    }
    let raw = sim_ctx::sim_with(ctx, |c| c.eval::<String, _>(
            "JSON.stringify(globalThis.__behaviorResult \
             || { logs: [] })"))
        .unwrap_or_else(|_| "{\"logs\":[]}".to_string());
    let res: serde_json::Value =
        serde_json::from_str(&raw).unwrap_or_default();
    if let Some(logs) = res.get("logs").and_then(|l| l.as_array()) {
        for l in logs {
            if let Some(s) = l.as_str() {
                runtime_log!("{}", s);
            }
        }
    }
    Ok(())
}

/// Dispatch a registered action in the PERSISTENT sim context (the module is
/// already installed there), collecting any move/teleport plans it parks and
/// persisting them to `crate::state` so the per-tick walker can advance them.
/// Unlike the fresh-context `simulate_action`, this does not re-evaluate the
/// module entry (which would re-declare the same UI node ids and throw), and
/// the entity-store writes it makes are synced back to the shared state the
/// .ui layer reads.
pub fn dispatch_action_in_sim(action_name: &str) -> Result<()> {
    dispatch_action_in_sim_for(action_name, "")
}

/// Dispatch a registered action in the persistent sim context, binding the
/// resulting plan (moves, teleports, wait/emit step sequences) to `actor` so
/// the per-actor busy/interruptible state keys on it.
pub fn dispatch_action_in_sim_for(action_name: &str, actor: &str) -> Result<()> {
    let Some(ctx) = sim_ctx::ctx() else { return Ok(()); };
    let name_json = serde_json::to_string(action_name)
        .unwrap_or_else(|_| "\"\"".to_string());
    let actor_json = serde_json::to_string(actor)
        .unwrap_or_else(|_| "\"\"".to_string());
    let script = format!(
        "(function(name){{\
          globalThis.__plannedMoves = [];\
          globalThis.__interruptible = false;\
          globalThis.__pendingEffects = [];\
          globalThis.__planSteps = [];\
          var acts = globalThis.__registeredActions || [];\
          var actionObj = null;\
          for (var i = 0; i < acts.length; i++) {{\
            var a = acts[i];\
            if (a && typeof a === 'object'\
                && typeof a.name === 'string' && a.name === name) {{ actionObj = a; break; }}\
            if (a && typeof a === 'function' && a.name === name) {{ actionObj = a; break; }}\
          }}\
          if (actionObj) {{\
            var ctx = {{\
              emitEffect: function(n,p){{\
                globalThis.__planSteps.push({{emit:{{name:n,payload:p||{{}}}}}});\
              }},\
              emitEvent: function(n,p){{\
                globalThis.__planSteps.push({{emit:{{name:n,payload:p||{{}}}}}});\
              }},\
              wait: function(d){{\
                var v;\
                if (typeof d === 'object' && d !== null) {{\
                  v = (d.ticks !== undefined) ? d.ticks\
                    : (d.frames !== undefined) ? d.frames\
                    : (d.seconds !== undefined) ? d.seconds * 60\
                    : (d.value !== undefined) ? d.value : 0;\
                }} else {{ v = d; }}\
                globalThis.__planSteps.push({{wait: Number(v) || 0}});\
              }},\
              createEntity: function(o){{\
                globalThis.__createdEntities = globalThis.__createdEntities || [];\
                if (o && typeof o === 'object' && typeof o.firstName === 'string') {{\
                  globalThis.__entityStore = globalThis.__entityStore || [];\
                  var e = {{}}; e[o.firstName]=o.firstName; globalThis.__entityStore.push(e);\
                  globalThis.__createdEntities.push(o.firstName);\
                }} else if (o && typeof o === 'object') {{\
                  globalThis.__entityStore = globalThis.__entityStore || [];\
                  globalThis.__entityStore.push(o);\
                  globalThis.__createdEntities.push(JSON.stringify(o));\
                }} else {{\
                  globalThis.__entityStore = globalThis.__entityStore || [];\
                  globalThis.__entityStore.push({{textMap_name: String(o)}});\
                  globalThis.__createdEntities.push(String(o));\
                }}\
              }},\
              entity: {{ create: function(){{ return {{ withTextMap: function(tm){{ return tm; }} }}; }} }},\
              textMap: {{ create: function(){{ return {{ put: function(k,v){{ var o={{}}; o[k]=v; return o; }} }}; }} }},\
               string: {{ of: function(s){{ return s; }} }},\
               args: globalThis.__actionArgs || {{}},\
               actor: {{ id: {actor_json}, containers: {{}} }},\
              moveTo: function(spec){{\
                var s = spec || {{}};\
                var c = (typeof s.containerId === 'object' && s.containerId !== null) ? s.containerId.value : s.containerId;\
                var e = (typeof s.entityId === 'object' && s.entityId !== null) ? s.entityId.value : s.entityId;\
                var sp = s.speed;\
                if (typeof sp === 'object' && sp !== null) sp = sp.value;\
                globalThis.__plannedMoves.push({{ containerId: c, entityId: e,\
                  x: Number(s.x), y: Number(s.y), speed: Number(sp) || 0 }});\
              }},\
              teleportTo: function(spec){{\
                var s = spec || {{}};\
                var c = (typeof s.containerId === 'object' && s.containerId !== null) ? s.containerId.value : s.containerId;\
                var e = (typeof s.entityId === 'object' && s.entityId !== null) ? s.entityId.value : s.entityId;\
                globalThis.__plannedMoves.push({{ containerId: c, entityId: e,\
                  x: Number(s.x), y: Number(s.y), speed: Infinity, teleport: true }});\
              }},\
              allowInterrupt: function(){{\
                globalThis.__interruptible = true;\
                globalThis.__planSteps.push({{interruptible: true}});\
              }},\
              denyInterrupt: function(){{\
                globalThis.__interruptible = false;\
                globalThis.__planSteps.push({{interruptible: false}});\
              }}\
            }};\
            try {{\
              if (typeof actionObj === 'object' && typeof actionObj.apply === 'function') actionObj.apply(ctx);\
              else if (typeof actionObj === 'function') actionObj(ctx);\
            }} catch(e) {{}}\
          }}\
          return JSON.stringify({{ plannedMoves: globalThis.__plannedMoves || [],\
            planSteps: globalThis.__planSteps || [],\
            interruptible: !!globalThis.__interruptible,\
            pendingEffects: globalThis.__pendingEffects || [] }});\
        }})({name_json})"
    );
    let raw = match sim_ctx::sim_with(ctx, |c| c.eval::<String, _>(script.as_str())) {
        Ok(s) => s,
        Err(e) => {
            runtime_log!("dispatch_action_in_sim eval failed: {:?}", e);
            return Ok(());
        }
    };
    // Surface + drain any logs the action's `apply` pushed (the module's
    // `hostApi.runtime.log` writes `globalThis.__logs` in this same context).
    // Draining keeps install-time logs from leaking into later dispatches.
    if let Ok(logs_raw) = sim_ctx::sim_with(ctx, |c| c.eval::<String, _>("JSON.stringify(globalThis.__logs||[])")) {
        if let Some(arr) = serde_json::from_str::<Vec<serde_json::Value>>(&logs_raw).ok() {
            for l in arr {
                if let Some(s) = l.as_str() {
                    runtime_log!("{}", s);
                }
            }
        }
        let _ = sim_ctx::sim_with(ctx, |c| c.eval::<(), _>("globalThis.__logs = []"));
    }
    let v: serde_json::Value = match serde_json::from_str(&raw) {
        Ok(v) => v,
        Err(_) => return Ok(()),
    };

    // Persist pending effects (so the effect path can run them this tick).
    if let Some(pe) = v.get("pendingEffects").and_then(|p| p.as_array()) {
        let names: Vec<String> = pe.iter()
            .filter_map(|e| e.get("name").and_then(|n| n.as_str()).map(|s| s.to_string()))
            .collect();
        if !names.is_empty() {
            let mut cur = crate::state::pending_effects().lock().unwrap();
            cur.extend(names);
        }
    }

    // Park any plan the action recorded, keyed on the actor:
    //  - teleports are applied synchronously (an immediate position write,
    //    clamped to the container bounds), never walked;
    //  - moves park as a single-step plan the per-tick walker advances;
    //  - emit/wait/interrupt step sequences park as a multi-step plan the
    //    walker runs over successive ticks.
    // resume_at = now: the plan is due this tick so the walker processes any
    // leading emit/interrupt steps in the same RunIteration. A `wait` step
    // re-parks with resume_at = now + wait so the next segment runs later.
    let total = crate::state::get_elapsed_time_units();
    let interruptible = v.get("interruptible").and_then(|b| b.as_bool()).unwrap_or(false);
    let moves = v.get("plannedMoves").and_then(|m| m.as_array()).cloned();
    let plan_steps = v.get("planSteps").and_then(|m| m.as_array()).cloned();
    let actor = actor.trim().to_string();

    if let Some(moves) = moves {
        let mut containers: Vec<String> =
            crate::state::last_containers().lock().unwrap().clone();
        for m in moves {
            let cid = m.get("containerId").and_then(|c| c.as_str()).unwrap_or("").to_string();
            let eid = m.get("entityId").and_then(|c| c.as_str()).unwrap_or("").to_string();
            let x = m.get("x").and_then(|n| n.as_f64()).unwrap_or(0.0);
            let y = m.get("y").and_then(|n| n.as_f64()).unwrap_or(0.0);
            let is_teleport = m.get("teleport").and_then(|t| t.as_bool()).unwrap_or(false)
                || m.get("speed").and_then(|s| s.as_str()).map(|s| s == "Infinity").unwrap_or(false);
            if is_teleport {
                // clamp: true is the teleport default in the fixtures.
                super::active_plans::apply_teleport(&mut containers, &cid, &eid, x, y, true);
            } else {
                let speed = m.get("speed").and_then(|n| n.as_f64()).unwrap_or(0.0);
                let step = serde_json::json!({
                    "move": { "containerId": cid, "entityId": eid, "x": x, "y": y, "speed": speed }
                });
                // The actor (if bound) owns the plan; the moved entity is the
                // position target. A bound actor is the busy/interruptible key.
                let who = if !actor.is_empty() { actor.clone() } else { eid.clone() };
                crate::state::set_active_plan(
                    action_name.to_string(),
                    who,
                    vec![step],
                    total,
                    interruptible,
                );
            }
        }
        *crate::state::last_containers().lock().unwrap() = containers;
    }

    // Park a wait/emit/interrupt step sequence for the walker. Only when the
    // action is not purely a move/teleport (those already parked a plan).
    if let Some(steps) = plan_steps {
        // An "instant" action records only interrupt markers (no emit/wait):
        // it runs without parking, so it discards the actor's parked plan
        // (the active action becomes none). A real step sequence parks.
        let has_step = steps.iter().any(|s| {
            s.get("emit").is_some() || s.get("wait").is_some()
        });
        if has_step {
            crate::state::set_active_plan(
                action_name.to_string(),
                actor.clone(),
                steps,
                total,
                interruptible,
            );
        } else if !actor.is_empty() {
            crate::state::active_plans().lock().unwrap()
                .retain(|p| p.actor != actor);
        }
    }

    // Sync entity-store writes back to shared state (setEntity/createEntity).
    let _ = sim_ctx::sim_with(ctx, |c| c.eval::<(), _>(
        "if (typeof globalThis.__syncEntityDataBack === 'function') globalThis.__syncEntityDataBack();"));
    Ok(())
}

