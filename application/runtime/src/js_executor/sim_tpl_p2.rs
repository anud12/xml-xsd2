// sim_template part 2: buildEventContext

const SIM_TPL_P2: &str = r#"
  function buildEventContext() {
    return { createEntity: recordCreated,
      getEntityBy: function(filter) {
        return globalThis.__evalEntityFilter(filter);
      }
    };
  }"#;

pub fn get_part2() -> &'static str { SIM_TPL_P2 }
