use anyhow::Result;
use super::context_builders::{
    build_effect_context_pending, sync_entity_data,
    eval_reoccur_interval, collect_logs, lookup_effect,
    call_effect_prepare, call_effect_apply, sync_entity_data_back,
};

pub fn process_pending_effects(
    _files: &std::collections::HashMap<String, String>,
    current_elapsed: i64,
) -> Result<()> {
    let effects = crate::state::pending_effects().lock().unwrap().clone();
    if effects.is_empty() { return Ok(()); }
    crate::state::clear_pending_effects();

    // Prefer the persistent sim context: the module is already installed
    // there with its registered effects and a live hostApi, so firing an
    // effect does not require re-evaluating the module entry (which would
    // re-run UI node declarations and can throw in a bare context).
    if let Some(ctx) = super::sim_ctx::ctx() {
        return run_pending_effects_in_sim(ctx, &effects, current_elapsed);
    }

    // Fallback: no sim context installed — build a fresh one and re-evaluate
    // the module entry (the legacy extraction path).
    let (_rt, ctx) = crate::js_executor::simulate::prepare_runtime_and_ctx()?;
    crate::js_host_api::install_host_api(&ctx)?;
    let source = crate::js_executor::simulate::select_entry_source(_files);
    let _ = crate::js_executor::simulate::eval_entry_in_ctx(&ctx, &source);
    run_pending_effects_in_sim(&ctx, &effects, current_elapsed)
}

/// Fire a batch of pending effects in the given context. Looks each effect up
/// in `__registeredEvents`, builds the effect ctx, runs prepare/apply, and
/// surfaces the effect's log lines. Returns after the batch (the caller owns
/// context lifecycle).
fn run_pending_effects_in_sim(
    ctx: &rquickjs::Context,
    effects: &[String],
    current_elapsed: i64,
) -> Result<()> {
    // Rebuild the effect context against the context's live entity data.
    sync_entity_data(ctx);

    for effect_name in effects.iter() {
        let found = lookup_effect(ctx, effect_name);
        if !found { continue; }

        build_effect_context_pending(ctx);
        call_effect_prepare(ctx);
        call_effect_apply(ctx);

        let ri = eval_reoccur_interval(ctx);
        if ri > 0.0 {
            let iv = ri as i64;
            let next = ((current_elapsed / iv) + 1) * iv;
            crate::state::add_scheduled_effect(
                effect_name.clone(),
                serde_json::Value::Object(serde_json::Map::new()),
                next, iv,
            );
        }

        sync_entity_data_back(ctx);
        super::entity_sync_back::sync_container_data_back(ctx);
        collect_logs(ctx);
    }
    Ok(())
}

#[cfg(test)]
mod tests {
    use super::*;
    use std::collections::HashMap;

    fn lock_test() -> std::sync::MutexGuard<'static, ()> {
        crate::state::test_lock()
    }

    #[test]
    fn reoccurring_effect_schedules_and_refires() {
        let _g = lock_test();
        crate::state::clear_state();
        super::super::sim_ctx::reset();

        let module = r#"
export default (hostApi) => {
  const { number, string } = hostApi.runtime;
  hostApi.runtime.setEntity({ id: string.of("game"), numberMap: { count: number.of(0) } });
  hostApi.runtime.setContainer({ id: string.of("inv"), entities: [] });
  const eff = hostApi.runtime.setEffect({
    name: string.of("tick"),
    reoccurAfterMs: () => hostApi.runtime.maybe.of(number.of(10)),
    apply: (context) => {
      context.getMutableEntityBy({
        filters: [(e) => e.id.isContainingExactly(string.of("game"))],
      }).map((g) => {
        const c = g.number_map.get("count").orElse(number.of(0));
        const n = c.sum(number.of(1));
        g.number_map.put("count", () => hostApi.runtime.maybe.of(n));
      });
    },
  });
  hostApi.runtime.emitEvent(eff, {});
};
"#;
        let mut files = HashMap::new();
        files.insert("index.js".to_string(), module.to_string());
        files.insert("manifest.json".to_string(), r#"{"entry":"index.js"}"#.to_string());

        super::super::sim_ctx::install(&files).unwrap();

        // In the real flow, process_module extracts __pendingEffects from JS
        // into Rust state. We set it directly here to match.
        crate::state::set_pending_effects(vec!["tick".to_string()]);
        assert!(!crate::state::pending_effects().lock().unwrap().is_empty());

        crate::state::add_elapsed_time_units(10);
        let elapsed = crate::state::get_elapsed_time_units();

        process_pending_effects(&HashMap::new(), elapsed).unwrap();

        assert!(!crate::state::scheduled_effects().lock().unwrap().is_empty(),
            "expected scheduled effect after pending processing");

        let count1 = crate::state::last_entity_number_data().lock().unwrap()
            .get("game").and_then(|m| m.get("count")).copied().unwrap_or(-1.0);
        assert_eq!(count1, 1.0);

        crate::state::add_elapsed_time_units(10);
        let elapsed2 = crate::state::get_elapsed_time_units();

        super::super::scheduled_effects::process_scheduled_effects(&HashMap::new(), elapsed2).unwrap();

        let count2 = crate::state::last_entity_number_data().lock().unwrap()
            .get("game").and_then(|m| m.get("count")).copied().unwrap_or(-1.0);
        assert_eq!(count2, 2.0, "reoccurring effect should have fired again");

        super::super::sim_ctx::reset();
    }
}
