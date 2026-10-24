// sim_template part 2: buildPrepareContext, buildApplyContext

const SIM_TPL_P2: &str = r#"
  function buildPrepareContext() {
    return { emitEvent: emitEvent,
      getEntityBy: function(filter) {
        return globalThis.__evalEntityFilter(filter);
      }
    };
  }
  function buildApplyContext() {
    return { createEntity: recordCreated,
      getMutableEntityBy: function(filter) {
        return globalThis.__evalEntityFilter(filter);
      }
    };
  }"#;

pub fn get_part2() -> &'static str { SIM_TPL_P2 }
