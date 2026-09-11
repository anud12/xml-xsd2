use std::sync::{Mutex, MutexGuard};
use crate::state::{ActivePlan, Portal, Room};

/// The parked action plans, one entry per actor (a plan arrives for a busy
/// actor by interrupting, so there is at most one plan per actor).
pub fn active_plans() -> &'static std::sync::Mutex<Vec<crate::state::ActivePlan>> {
    super::persisted_flag();
    unsafe { super::ACTIVE_PLANS.expect("active plans initialized") }
}

/// A shared process-wide lock so the plan-walker tests (which mutate global
/// runtime state) do not race other modules' state tests.
static TEST_LOCK: Mutex<()> = Mutex::new(());

pub fn test_lock() -> MutexGuard<'static, ()> {
    TEST_LOCK.lock().unwrap_or_else(|e| e.into_inner())
}

/// True while the actor has a parked plan (a move in progress or a future
/// wait). A free actor has nothing queued: it is not running or waiting on
/// any action.
pub fn actor_is_busy(actor: &str) -> bool {
    let a = actor.trim();
    if a.is_empty() { return false; }
    active_plans().lock().unwrap().iter().any(|p| p.actor == a)
}

/// True while the actor's parked plan was marked interruptible (allowInterrupt);
/// a busy, interruptible actor accepts a new action, otherwise it is dropped.
pub fn actor_plan_interruptible(actor: &str) -> bool {
    let a = actor.trim();
    if a.is_empty() { return false; }
    active_plans().lock().unwrap()
        .iter()
        .find(|p| p.actor == a)
        .map(|p| p.interruptible)
        .unwrap_or(false)
}

/// The name of the action whose plan is currently parked for this actor, or
/// `None` while the actor is free.
pub fn actor_active_action(actor: &str) -> Option<String> {
    let a = actor.trim();
    if a.is_empty() { return None; }
    active_plans().lock().unwrap()
        .iter()
        .find(|p| p.actor == a)
        .map(|p| p.action_name.clone())
}

pub fn has_active_plan(name: &str) -> bool {
    active_plans().lock().unwrap().iter().any(|p| p.action_name == name)
}

/// Park a plan for an actor, replacing any prior plan for the same actor. A new
/// plan for a busy actor is an interrupt, not a queue: the prior plan is
/// dropped (the interruptible flag decides whether it is *allowed*, but a plan
/// arriving while busy still takes over the actor).
pub fn set_active_plan(
    action_name: String,
    actor: String,
    steps: Vec<serde_json::Value>,
    resume_at: i64,
    interruptible: bool,
) {
    park_active_plan(&action_name, &actor, steps, resume_at, interruptible);
}

pub fn park_active_plan(
    action_name: &str,
    actor: &str,
    steps: Vec<serde_json::Value>,
    resume_at: i64,
    interruptible: bool,
) {
    let actor = actor.trim();
    let mut plans = active_plans().lock().unwrap();
    plans.retain(|p| !(p.actor == actor && p.action_name == action_name));
    if !actor.is_empty() {
        plans.retain(|p| p.actor != actor);
    }
    plans.push(ActivePlan {
        action_name: action_name.to_string(),
        actor: actor.to_string(),
        steps,
        resume_at,
        interruptible,
    });
}

pub fn remove_active_plan_for(action_name: &str, actor: &str) {
    let mut plans = active_plans().lock().unwrap();
    plans.retain(|p| !(p.actor == actor && p.action_name == action_name));
}

pub fn rooms() -> &'static std::sync::Mutex<Vec<Room>> {
    super::persisted_flag();
    unsafe { super::ROOMS.expect("rooms initialized") }
}

pub fn portals() -> &'static std::sync::Mutex<Vec<Portal>> {
    super::persisted_flag();
    unsafe { super::PORTALS.expect("portals initialized") }
}

pub fn set_rooms(rs: Vec<Room>) {
    *rooms().lock().unwrap() = rs;
}

pub fn set_portals(ps: Vec<Portal>) {
    *portals().lock().unwrap() = ps;
}

pub fn fetch_room_by_id(id: &str) -> Option<Room> {
    rooms().lock().unwrap().iter().find(|r| r.id == id).cloned()
}

/// Rooms + portals as JSON: `{"rooms":[...],"portals":[...]}`.
pub fn fetch_rooms_json() -> String {
    let rooms = rooms().lock().unwrap().clone();
    let portals = portals().lock().unwrap().clone();
    serde_json::json!({ "rooms": rooms, "portals": portals })
        .to_string()
}
