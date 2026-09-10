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
    if let Err(e) = ctx.with(|c| c.eval::<(), _>(invoke)) {
        runtime_log!("behavior machine failed: {:?}", e);
        return Ok(());
    }
    let raw = ctx
        .with(|c| c.eval::<String, _>(
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

