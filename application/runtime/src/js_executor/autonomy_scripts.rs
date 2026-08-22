use anyhow::Result;
use crate::js_runtime::{create_runtime, create_context};
use crate::js_host_api::install_host_api;
use super::sim_entry::{select_entry_source, eval_entry_in_ctx};

const AUTONOMY_SCRIPTS_JS: &str = r#"(function(total, prevScripts){
  globalThis.__logs = [];
  globalThis.__autonomyScripts = prevScripts || {};
  const defs = globalThis.__autonomyDefinitions || {};
  const atts = globalThis.__autonomies || {};
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
    const def = handle && defs[handle.name];
    if (!def) continue;
    const branch = def.priority && def.priority[0];
    const rule = branch && branch.utility && branch.utility[0];
    if (!rule || !Array.isArray(rule.steps)) continue;
    const st = globalThis.__autonomyScripts[entityId]
      || (globalThis.__autonomyScripts[entityId]
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
  globalThis.__autonomyResult = {
    logs: globalThis.__logs || [],
    scripts: globalThis.__autonomyScripts
  };
  return globalThis.__autonomyResult;
})"#;

pub fn process_autonomy_scripts(
    files: &std::collections::HashMap<String, String>,
    total: i64,
) -> Result<()> {
    fn eval_with_detail(
        ctx: &rquickjs::Context,
        label: &str,
        script: String,
    ) -> Result<()> {
        use rquickjs::CatchResultExt;
        let res: Result<(), String> = ctx.with(|c| {
            match c.eval::<(), _>(script).catch(&c) {
                Ok(()) => Ok(()),
                Err(e) => {
                    let value = match &e {
                        rquickjs::CaughtError::Exception(ex) => {
                            ex.clone().into_value()
                        }
                        rquickjs::CaughtError::Value(v) => v.clone(),
                        rquickjs::CaughtError::Error(_) => {
                            return Err(String::new())
                        }
                    };
                    let detail = match value.into_object() {
                        Some(obj) => {
                            let name: Result<String, _> = obj.get("name");
                            let message: Result<String, _> =
                                obj.get("message");
                            match (name.ok(), message.ok()) {
                                (Some(n), Some(m)) if !m.is_empty() => {
                                    format!("{}: {}", n, m)
                                }
                                (Some(n), _) if !n.is_empty() => n,
                                (_, Some(m)) => m,
                                _ => String::new(),
                            }
                        }
                        None => String::new(),
                    };
                    Err(detail)
                }
            }
        });
        match res {
            Ok(()) => Ok(()),
            Err(detail) => Err(anyhow::anyhow!(
                "{}: {}",
                label,
                if detail.is_empty() {
                    "QuickJS exception".to_string()
                } else { detail })),
        }
    }
    let rt = create_runtime()?;
    let ctx = create_context(&rt)?;
    install_host_api(&ctx)?;
    let source = select_entry_source(files);
    eval_entry_in_ctx(&ctx, &source)?;
    let prev = crate::state::autonomy_scripts()
        .lock().unwrap().clone();
    let script = format!(
        "JSON.stringify(({})({}, {}))",
        AUTONOMY_SCRIPTS_JS, total, prev);
    eval_with_detail(&ctx, "autonomy scripts", script)?;
    let raw = ctx.with(|c| c.eval::<String, _>(
        "JSON.stringify(globalThis.__autonomyResult)"))?;
    let res: serde_json::Value = serde_json::from_str(&raw)?;
    if let Some(logs) = res.get("logs").and_then(|l| l.as_array()) {
        for l in logs {
            if let Some(s) = l.as_str() { runtime_log!("{}", s); }
        }
    }
    if let Some(scripts) = res.get("scripts") {
        *crate::state::autonomy_scripts().lock().unwrap() =
            scripts.to_string();
    }
    Ok(())
}

