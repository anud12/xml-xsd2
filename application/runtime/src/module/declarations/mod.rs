use crate::js_host_api::Declarations;

mod entity_data;
mod patterns;
mod action_map;
mod state_updates;

pub use patterns::collect_patterns;
pub use action_map::build_action_to_created;

pub fn print_events_from_declarations(
    dec: &Declarations,
) -> std::collections::HashSet<String> {
    let mut seen = std::collections::HashSet::new();
    for ev in dec.events.iter() {
        runtime_log!("event: {}", ev);
        runtime_log!("event registered: {}", ev);
        seen.insert(ev.clone());
    }
    seen
}

pub fn apply_declarations(dec: &Declarations) {
    runtime_log!(
        "module process: extract_from_source succeeded"
    );
    crate::state::mark_persisted_has_data();
    print_events_from_declarations(dec);
    log_actions_and_panels(dec);
    set_state_from_declarations(dec);
    entity_data::store_entity_data(dec);
}

fn log_actions_and_panels(dec: &Declarations) {
    for action in dec.actions.iter() {
        runtime_log!("action: {}", action);
        runtime_log!("action registered: {}", action);
    }
    for l in dec.logs.iter() {
        runtime_log!("{}", l);
    }
    runtime_log!("creators: {:?}", dec.creators);
    runtime_log!("emits: {:?}", dec.emits);
    for p in dec.panels.iter() {
        runtime_log!("panel: {}", p);
    }
}

fn set_state_from_declarations(dec: &Declarations) {
    let patterns = collect_patterns(dec);
    crate::state::set_last_entity_patterns(patterns);
    state_updates::append_panels_to_cache(dec);
    crate::state::set_last_action_rows(
        dec.actions.iter().map(|a| vec![a.clone()]).collect()
    );
    crate::state::set_last_event_rows(
        dec.events.iter().map(|e| vec![e.clone()]).collect()
    );
    let action_map = build_action_to_created(dec);
    crate::state::set_last_created_by(action_map);
    state_updates::store_pending_effects(dec);
}
