use std::collections::HashMap;
use std::sync::atomic::{AtomicI64, Ordering};
use std::sync::Mutex;

pub fn last_file_rows() -> &'static Mutex<Vec<Vec<String>>> {
    super::persisted_flag(); unsafe { super::LAST_FILE_ROWS.expect("file rows initialized") }
}
pub fn last_entity_rows() -> &'static Mutex<Vec<Vec<String>>> {
    super::persisted_flag(); unsafe { super::LAST_ENTITY_ROWS.expect("entity rows initialized") }
}
pub fn last_action_rows() -> &'static Mutex<Vec<Vec<String>>> {
    super::persisted_flag(); unsafe { super::LAST_ACTION_ROWS.expect("action rows initialized") }
}
pub fn last_event_rows() -> &'static Mutex<Vec<Vec<String>>> {
    super::persisted_flag(); unsafe { super::LAST_EVENT_ROWS.expect("event rows initialized") }
}
pub fn last_module_rows() -> &'static Mutex<Vec<Vec<String>>> {
    super::persisted_flag(); unsafe { super::LAST_MODULE_ROWS.expect("module rows initialized") }
}
pub fn last_entity_patterns() -> &'static Mutex<Vec<String>> {
    super::persisted_flag(); unsafe { super::LAST_ENTITY_PATTERNS.expect("entity patterns initialized") }
}
pub fn last_panels() -> &'static Mutex<Vec<String>> {
    super::persisted_flag(); unsafe { super::LAST_PANELS.expect("panels initialized") }
}
pub fn last_created_by() -> &'static Mutex<HashMap<String, Vec<String>>> {
    super::persisted_flag(); unsafe { super::LAST_CREATED_BY.expect("created by map initialized") }
}
pub fn pending_effects() -> &'static Mutex<Vec<String>> {
    super::persisted_flag(); unsafe { super::PENDING_EFFECTS.expect("pending effects initialized") }
}
pub fn scheduled_effects() -> &'static Mutex<Vec<super::ScheduledEffect>> {
    super::persisted_flag(); unsafe { super::SCHEDULED_EFFECTS.expect("scheduled effects initialized") }
}
pub fn active_plans() -> &'static Mutex<Vec<super::ActivePlan>> {
    super::persisted_flag(); unsafe { super::ACTIVE_PLANS.expect("active plans initialized") }
}
/// Park a plan for (actor, action_name). One plan per actor: parking for a
/// non-empty actor first drops any plan that actor already had (the interrupt:
/// the prior plan is discarded, never queued). `interruptible` records whether
/// the actor opted in to being interrupted while parked (allowInterrupt()).
pub fn set_active_plan(
    action_name: String,
    actor: String,
    steps: Vec<serde_json::Value>,
    resume_at: i64,
    interruptible: bool,
) {
    let mut plans = active_plans().lock().unwrap();
    if !actor.is_empty() {
        plans.retain(|p| p.actor != actor);
        plans.push(super::ActivePlan { actor, action_name, steps, resume_at, interruptible });
        return;
    }
    if let Some(p) = plans.iter_mut().find(|p| p.action_name == action_name && p.actor.is_empty())
    {
        p.steps = steps;
        p.resume_at = resume_at;
        p.interruptible = interruptible;
    } else {
        plans.push(super::ActivePlan { actor, action_name, steps, resume_at, interruptible });
    }
}
pub fn park_active_plan(
    action_name: &str,
    actor: &str,
    steps: Vec<serde_json::Value>,
    resume_at: i64,
    interruptible: bool,
) {
    set_active_plan(action_name.to_string(), actor.to_string(), steps, resume_at, interruptible);
}
pub fn remove_active_plan(action_name: &str) {
    let mut plans = active_plans().lock().unwrap();
    plans.retain(|p| p.action_name != action_name);
}
pub fn remove_active_plan_for(action_name: &str, actor: &str) {
    let mut plans = active_plans().lock().unwrap();
    plans.retain(|p| !(p.action_name == action_name && p.actor == actor));
}
pub fn has_active_plan(action_name: &str) -> bool {
    active_plans().lock().unwrap().iter().any(|p| p.action_name == action_name)
}
/// True while any plan for this actor is parked (per-actor serialization:
/// a busy actor rejects all further actions, not just the same one).
pub fn actor_is_busy(actor: &str) -> bool {
    if actor.is_empty() { return false; }
    active_plans().lock().unwrap().iter().any(|p| p.actor == actor)
}
/// True while the actor's parked plan was marked interruptible via
/// allowInterrupt(). A busy, interruptible actor accepts a new action (which
/// interrupts the parked plan); a busy, non-interruptible actor rejects it.
pub fn actor_plan_interruptible(actor: &str) -> bool {
    if actor.is_empty() { return false; }
    active_plans().lock().unwrap().iter().any(|p| p.actor == actor && p.interruptible)
}
/// The name of the action whose plan is currently parked for this actor, or
/// `None` while the actor is free. One plan per actor, so at most one name.
pub fn actor_active_action(actor: &str) -> Option<String> {
    if actor.is_empty() { return None; }
    active_plans().lock().unwrap().iter()
        .find(|p| p.actor == actor)
        .map(|p| p.action_name.clone())
}
/// Drop every plan parked for this actor. An empty actor matches nothing.
pub fn remove_active_plans_for_actor(actor: &str) {
    if actor.is_empty() { return; }
    active_plans().lock().unwrap().retain(|p| p.actor != actor);
}
pub fn last_entity_data() -> &'static Mutex<HashMap<String, HashMap<String, String>>> {
    super::persisted_flag(); unsafe { super::LAST_ENTITY_DATA.expect("entity data initialized") }
}
pub fn last_entity_number_data() -> &'static Mutex<HashMap<String, HashMap<String, f64>>> {
    super::persisted_flag(); unsafe { super::LAST_ENTITY_NUMBER_DATA.expect("entity number data initialized") }
}
pub fn initial_entity_data() -> &'static Mutex<HashMap<String, HashMap<String, String>>> {
    super::persisted_flag(); unsafe { super::INITIAL_ENTITY_DATA.expect("initial entity data initialized") }
}
pub fn last_containers() -> &'static Mutex<Vec<String>> {
    super::persisted_flag(); unsafe { super::LAST_CONTAINERS.expect("containers initialized") }
}
pub fn set_last_containers(rows: Vec<String>) {
    *last_containers().lock().unwrap() = rows;
}
pub fn elapsed_time_units() -> &'static AtomicI64 {
    super::persisted_flag(); unsafe { super::ELAPSED_TIME_UNITS.expect("elapsed time units initialized") }
}
pub fn set_last_file_rows(rows: Vec<Vec<String>>) { *last_file_rows().lock().unwrap() = rows; }
pub fn set_last_entity_rows(rows: Vec<Vec<String>>) { *last_entity_rows().lock().unwrap() = rows; }
pub fn append_entity_row(row: Vec<String>) { last_entity_rows().lock().unwrap().push(row); }
pub fn set_last_action_rows(rows: Vec<Vec<String>>) { *last_action_rows().lock().unwrap() = rows; }
pub fn set_last_event_rows(rows: Vec<Vec<String>>) { *last_event_rows().lock().unwrap() = rows; }
pub fn set_last_module_rows(rows: Vec<Vec<String>>) { *last_module_rows().lock().unwrap() = rows; }
pub fn set_last_entity_patterns(rows: Vec<String>) { *last_entity_patterns().lock().unwrap() = rows; }
pub fn set_last_panels(rows: Vec<String>) { *last_panels().lock().unwrap() = rows; }
pub fn set_last_created_by(map: HashMap<String, Vec<String>>) {
    *last_created_by().lock().unwrap() = map;
}
pub fn set_last_entity_data(data: HashMap<String, HashMap<String, String>>) {
    *last_entity_data().lock().unwrap() = data;
}
pub fn set_last_entity_number_data(data: HashMap<String, HashMap<String, f64>>) {
    *last_entity_number_data().lock().unwrap() = data;
}
pub fn set_initial_entity_data(data: HashMap<String, HashMap<String, String>>) {
    *initial_entity_data().lock().unwrap() = data;
}
pub fn set_pending_effects(effects: Vec<String>) { *pending_effects().lock().unwrap() = effects; }
pub fn clear_pending_effects() { pending_effects().lock().unwrap().clear(); }
pub fn add_elapsed_time_units(units: i64) {
    elapsed_time_units().fetch_add(units, Ordering::SeqCst);
}
pub fn get_elapsed_time_units() -> i64 {
    elapsed_time_units().load(Ordering::SeqCst)
}
pub fn archive_files() -> &'static Mutex<HashMap<String, String>> {
    super::persisted_flag(); unsafe { super::ARCHIVE_FILES.expect("archive files initialized") }
}
pub fn set_archive_files(map: HashMap<String, String>) {
    *archive_files().lock().unwrap() = map;
}
