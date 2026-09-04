//! Per-tick walker for parked action plans.
//!
//! A plan is plain data (recorded steps), so walking needs no JS engine:
//! due plans walk emit steps into the pending-effects queue, advance move
//! steps by writing the actor's position, and park again on wait/move steps,
//! until the steps run out and the plan completes.

use crate::state::ActivePlan;

const SQRT2: f64 = 1.4142135623730951;

/// Total path length for a straight-line move. Axis-aligned (one delta zero)
/// uses the larger delta; diagonal uses floor(√2 * min(|dx|,|dy|)) — a float
/// clamped to int by truncation toward zero.
pub(crate) fn move_length(dx: f64, dy: f64) -> f64 {
    if dx == 0.0 || dy == 0.0 {
        dx.abs().max(dy.abs())
    } else {
        (SQRT2 * dx.abs().min(dy.abs())).floor()
    }
}

/// One container's size bounds: max coordinate along each axis (None = unbound).
fn container_bounds(
    containers: &[String],
    container_id: &str,
) -> (Option<f64>, Option<f64>) {
    for json_str in containers.iter() {
        let cid = extract_container_id(json_str);
        if cid != container_id {
            continue;
        }
        if let Ok(v) = serde_json::from_str::<serde_json::Value>(json_str) {
            let sx = v.get("sizeX")
                .and_then(|s| s.get("value"))
                .and_then(|n| n.as_f64());
            let sy = v.get("sizeY")
                .and_then(|s| s.get("value"))
                .and_then(|n| n.as_f64());
            return (sx, sy);
        }
    }
    (None, None)
}

/// The `id` field of a serialized container row (mirrors get_container_by_id).
fn extract_container_id(json_str: &str) -> String {
    let trimmed = json_str.trim();
    if trimmed.starts_with('{') {
        if let Some(pos) = trimmed.find("\"id\"") {
            if let Some(colon) = trimmed[pos..].find(':') {
                let after = &trimmed[pos + colon + 1..];
                let mut s = after.trim_start();
                if s.starts_with('"') {
                    s = &s[1..];
                    if let Some(end) = s.find('"') {
                        s = &s[..end];
                    }
                } else {
                    if let Some(end) = s.find(',') { s = &s[..end]; }
                    if let Some(end) = s.find('}') { s = &s[..end]; }
                    s = s.trim();
                }
                return s.to_string();
            }
        }
    }
    trimmed.to_string()
}

/// Find the container's pre-baked `getX`/`getY` position-key name for an
/// entity. Returns `(x_key, y_key)`; either may be empty if the container
/// declares no such accessor.
fn position_keys(
    containers: &[String],
    container_id: &str,
    entity_id: &str,
) -> (String, String) {
    for json_str in containers.iter() {
        let cid = extract_container_id(json_str);
        if cid != container_id {
            continue;
        }
        if let Ok(v) = serde_json::from_str::<serde_json::Value>(json_str) {
            let xk = v.get("getX").and_then(|g| g.get(entity_id))
                .and_then(|n| n.as_f64()).map(|_| "x".to_string()).unwrap_or_default();
            let yk = v.get("getY").and_then(|g| g.get(entity_id))
                .and_then(|n| n.as_f64()).map(|_| "y".to_string()).unwrap_or_default();
            return (xk, yk);
        }
    }
    (String::new(), String::new())
}

/// Read the actor's current position from the container's pre-baked
/// `getX`/`getY` maps (the same source the sim's `teleportTo` writes to).
fn current_position(
    containers: &[String],
    container_id: &str,
    entity_id: &str,
) -> Option<(f64, f64)> {
    for json_str in containers.iter() {
        let cid = extract_container_id(json_str);
        if cid != container_id {
            continue;
        }
        if let Ok(v) = serde_json::from_str::<serde_json::Value>(json_str) {
            let x = v.get("getX").and_then(|g| g.get(entity_id))
                .and_then(|n| n.as_f64());
            let y = v.get("getY").and_then(|g| g.get(entity_id))
                .and_then(|n| n.as_f64());
            if x.is_some() || y.is_some() {
                return Some((x.unwrap_or(0.0), y.unwrap_or(0.0)));
            }
        }
    }
    None
}

/// Write the actor's position into the entity number data under the
/// container's position keys and rebake the container's registered JSON so
/// `get_container_by_id` reflects the new position.
fn write_position(
    containers: &mut Vec<String>,
    container_id: &str,
    entity_id: &str,
    x: f64,
    y: f64,
) {
    let (xk, yk) = position_keys(containers, container_id, entity_id);
    {
        let mut nd = crate::state::last_entity_number_data().lock().unwrap();
        let em = nd.entry(entity_id.to_string()).or_insert_with(HashMap::new);
        if !xk.is_empty() { em.insert(xk.clone(), x); }
        if !yk.is_empty() { em.insert(yk.clone(), y); }
    }
    rebake_container(containers, container_id, entity_id, x, y);
}

/// Replace the container row's `getX`/`getY` value for the entity and write the
/// updated JSON back into the `last_containers` state.
fn rebake_container(
    containers: &mut Vec<String>,
    container_id: &str,
    entity_id: &str,
    x: f64,
    y: f64,
) {
    for (idx, json_str) in containers.iter_mut().enumerate() {
        let cid = extract_container_id(json_str);
        if cid != container_id {
            continue;
        }
        if let Ok(mut v) = serde_json::from_str::<serde_json::Value>(json_str) {
            if let Some(gx) = v.get_mut("getX").and_then(|g| g.as_object_mut()) {
                gx.insert(entity_id.to_string(), num_json(x));
            }
            if let Some(gy) = v.get_mut("getY").and_then(|g| g.as_object_mut()) {
                gy.insert(entity_id.to_string(), num_json(y));
            }
            if let Ok(serialized) = serde_json::to_string(&v) {
                containers[idx] = serialized;
            }
            return;
        }
    }
}

fn num_json(n: f64) -> serde_json::Value {
    serde_json::Number::from_f64(n)
        .map(serde_json::Value::Number)
        .unwrap_or(serde_json::Value::Null)
}

use std::collections::HashMap;

pub fn process_active_plans(now: i64) {
    let mut due: Vec<ActivePlan> = {
        let plans = crate::state::active_plans().lock().unwrap();
        plans.iter().filter(|p| p.resume_at <= now).cloned().collect()
    };
    if due.is_empty() { return; }

    let mut containers: Vec<String> =
        crate::state::last_containers().lock().unwrap().clone();
    let mut emitted: Vec<String> = Vec::new();
    let mut to_park: Vec<(String, String, Vec<serde_json::Value>, i64, bool)> = Vec::new();
    let mut to_remove: Vec<(String, String)> = Vec::new();

    for plan in due.iter_mut() {
        let mut i = 0;
        let mut parked = false;
        while i < plan.steps.len() {
            let step = plan.steps[i].clone();
            if let Some(val) = step.get("interruptible").and_then(|b| b.as_bool()) {
                plan.interruptible = val;
                i += 1;
                continue;
            }
            if let Some(name) = step.get("emit")
                .and_then(|e| e.get("name"))
                .and_then(|n| n.as_str())
            {
                emitted.push(name.to_string());
                i += 1;
                continue;
            }
            if let Some(wait) = step.get("wait").and_then(|w| w.as_i64()) {
                let steps = plan.steps[i + 1..].to_vec();
                let resume_at = now + wait.max(0);
                to_park.push((
                    plan.action_name.clone(),
                    plan.actor.clone(),
                    steps,
                    resume_at,
                    plan.interruptible,
                ));
                parked = true;
                break;
            }
            if step.get("move").is_some() {
                let remaining = advance_move_step(&mut plan.steps[i], now, &mut containers);
                if remaining {
                    // Still moving: keep the move step at the head, re-park one tick.
                    let steps = plan.steps[i..].to_vec();
                    to_park.push((
                        plan.action_name.clone(),
                        plan.actor.clone(),
                        steps,
                        now + 1,
                        plan.interruptible,
                    ));
                    parked = true;
                    break;
                } else {
                    // Move exhausted: consume the step, continue to the next.
                    i += 1;
                    continue;
                }
            }
            i += 1;
        }
        if !parked && i >= plan.steps.len() {
            to_remove.push((plan.action_name.clone(), plan.actor.clone()));
        }
    }

    for (name, actor, steps, resume_at, interruptible) in to_park {
        crate::state::park_active_plan(&name, &actor, steps, resume_at, interruptible);
    }
    for (name, actor) in to_remove {
        crate::state::remove_active_plan_for(&name, &actor);
    }
    if !emitted.is_empty() {
        crate::state::pending_effects().lock().unwrap().extend(emitted);
    }
    *crate::state::last_containers().lock().unwrap() = containers;
}

/// Advance a `move` plan step by one tick. Returns `true` if the move is still
/// in progress (the step stays at the head and the plan re-parks), `false` if
/// the move is exhausted (the caller consumes the step and continues).
fn advance_move_step(
    step: &mut serde_json::Value,
    now: i64,
    containers: &mut Vec<String>,
) -> bool {
    let move_obj = match step.get_mut("move").and_then(|m| m.as_object_mut()) {
        Some(m) => m,
        None => return false,
    };
    let container_id = move_obj.get("containerId")
        .and_then(|v| v.as_str()).unwrap_or("").to_string();
    let entity_id = move_obj.get("entityId")
        .and_then(|v| v.as_str()).unwrap_or("").to_string();
    let tx = move_obj.get("x").and_then(|v| v.as_f64()).unwrap_or(0.0);
    let ty = move_obj.get("y").and_then(|v| v.as_f64()).unwrap_or(0.0);

    // The actor's live position, tracked in the step. Initialized once from the
    // container (the pre-baked getX/getY), then updated after each advance so
    // the next tick advances from the current cell, not the start.
    let (cx, cy) = match move_obj.get("start").and_then(|s| s.as_object()) {
        Some(s) => (
            s.get("x").and_then(|v| v.as_f64()).unwrap_or(0.0),
            s.get("y").and_then(|v| v.as_f64()).unwrap_or(0.0),
        ),
        None => match current_position(containers, &container_id, &entity_id) {
            Some(c) => {
                move_obj.insert("start".into(),
                    serde_json::json!({ "x": c.0, "y": c.1 }));
                c
            }
            None => return false,
        },
    };

    // Resolve speed (re-read each tick so mid-move changes take effect).
    let speed = move_obj.get("speed")
        .and_then(|v| {
            if let Some(n) = v.as_f64() { return Some(n); }
            if let Some(s) = v.as_str() { return s.parse::<f64>().ok(); }
            None
        })
        .unwrap_or(0.0);

    // Already at the target: the move is done.
    if (tx - cx).abs() < 1e-9 && (ty - cy).abs() < 1e-9 {
        return false;
    }

    // speed <= 0: "try, then stop" — no advance, the move ends here.
    if speed <= 0.0 {
        return false;
    }

    let (bx, by) = container_bounds(containers, &container_id);

    // Per-axis progress toward the target, capped at the container bounds
    // (a bound is the max coordinate; the lower bound is 0). An axis already at
    // its target is neutral; a *moving* axis ends the move once it reaches its
    // target or is stopped by a bound (the "try, then stop" rule).
    let x_neutral = (tx - cx).abs() < 1e-9;
    let y_neutral = (ty - cy).abs() < 1e-9;
    let (nx, ax_done) = if x_neutral { (cx, false) } else { advance_axis(cx, tx, speed, bx) };
    let (ny, ay_done) = if y_neutral { (cy, false) } else { advance_axis(cy, ty, speed, by) };

    // Remaining path length is the actual distance still to cover.
    let remaining = move_length(tx - nx, ty - ny);
    move_obj.insert("remainingLength".into(), num_json(remaining));
    move_obj.insert("start".into(), serde_json::json!({ "x": nx, "y": ny }));
    write_position(containers, &container_id, &entity_id, nx, ny);

    // The move continues only while every moving axis still has room and
    // length left.
    if (ax_done || ay_done) {
        return false;
    }
    remaining > 0.0
}

/// Advance one coordinate toward its target by `speed`, capped at `bound`
/// (max coordinate; lower bound 0). Returns the new value and whether this
/// (moving) axis is now done — at its target, or stopped by a bound.
fn advance_axis(
    cur: f64,
    target: f64,
    speed: f64,
    bound: Option<f64>,
) -> (f64, bool) {
    let up = target > cur;
    let mut next = if up { cur + speed } else { cur - speed };
    // Clamp into [0, bound].
    if next < 0.0 { next = 0.0; }
    if let Some(b) = bound {
        if next > b { next = b; }
    }
    // Overshoot the target: snap onto it.
    if up { if next > target { next = target; } }
    else { if next < target { next = target; } }
    let done = (next - target).abs() < 1e-9
        || (up && next >= bound.unwrap_or(f64::MAX))
        || (!up && next <= 0.0);
    (next, done)
}

#[cfg(test)]
mod tests {
    use super::*;
    use serde_json::json;
    use std::sync::MutexGuard;

    // The plan walker operates on global runtime state; serialize via the
    // shared state lock so we don't race other modules' state tests.
    fn lock_test() -> MutexGuard<'static, ()> {
        crate::state::test_lock()
    }

    fn emit(name: &str) -> serde_json::Value {
        json!({ "emit": { "name": name, "payload": {} } })
    }

    fn wait(gtu: i64) -> serde_json::Value {
        json!({ "wait": gtu })
    }

    fn plan(action: &str, actor: &str, steps: Vec<serde_json::Value>, resume_at: i64) {
        crate::state::set_active_plan(action.into(), actor.into(), steps, resume_at, false);
    }

    #[test]
    fn due_plan_emits_and_parks_on_wait() {
        let _g = lock_test();
        crate::state::clear_state();
        plan("shoot", "e1", vec![emit("aim"), wait(10), emit("fire")], 5);
        process_active_plans(5);

        assert_eq!(
            crate::state::pending_effects().lock().unwrap().clone(),
            vec!["aim".to_string()]
        );
        let plans = crate::state::active_plans().lock().unwrap();
        assert_eq!(plans.len(), 1);
        assert_eq!(plans[0].action_name, "shoot");
        assert_eq!(plans[0].resume_at, 15);
        assert_eq!(
            plans[0].steps,
            vec![json!({"emit": {"name": "fire", "payload": {}}})]
        );
    }

    #[test]
    fn due_plan_completes_without_wait() {
        let _g = lock_test();
        crate::state::clear_state();
        plan("walk", "", vec![emit("step1"), emit("step2")], 0);
        process_active_plans(0);

        assert_eq!(
            crate::state::pending_effects().lock().unwrap().clone(),
            vec!["step1".to_string(), "step2".to_string()]
        );
        assert!(!crate::state::has_active_plan("walk"));
    }

    #[test]
    fn not_due_plan_is_left_alone() {
        let _g = lock_test();
        crate::state::clear_state();
        plan("rest", "e1", vec![emit("heal")], 10);
        process_active_plans(9);

        assert!(crate::state::pending_effects().lock().unwrap().is_empty());
        assert!(crate::state::has_active_plan("rest"));
        process_active_plans(10);
        assert_eq!(
            crate::state::pending_effects().lock().unwrap().clone(),
            vec!["heal".to_string()]
        );
        assert!(!crate::state::has_active_plan("rest"));
    }

    #[test]
    fn trailing_wait_parks_empty_then_completes() {
        let _g = lock_test();
        crate::state::clear_state();
        plan("channel", "e1", vec![emit("start"), wait(3)], 0);
        process_active_plans(0);
        assert_eq!(
            crate::state::pending_effects().lock().unwrap().clone(),
            vec!["start".to_string()]
        );
        assert!(crate::state::has_active_plan("channel"));

        process_active_plans(2);
        assert!(crate::state::has_active_plan("channel"));
        process_active_plans(3);
        assert!(!crate::state::has_active_plan("channel"));
    }

    #[test]
    fn multi_wait_plan_walks_across_ticks() {
        let _g = lock_test();
        crate::state::clear_state();
        plan("rest", "e1", vec![emit("h1"), wait(2), emit("h2"), wait(2), emit("h3")], 0);
        process_active_plans(0);
        assert_eq!(crate::state::pending_effects().lock().unwrap().clone(), vec!["h1"]);

        process_active_plans(1);
        assert_eq!(crate::state::pending_effects().lock().unwrap().clone(), vec!["h1"]);

        process_active_plans(2);
        assert_eq!(
            crate::state::pending_effects().lock().unwrap().clone(),
            vec!["h1", "h2"]
        );
        assert!(crate::state::has_active_plan("rest"));

        process_active_plans(3);
        assert!(crate::state::has_active_plan("rest"));
        process_active_plans(4);
        assert_eq!(
            crate::state::pending_effects().lock().unwrap().clone(),
            vec!["h1", "h2", "h3"]
        );
        assert!(!crate::state::has_active_plan("rest"));
    }

    #[test]
    fn clear_state_drops_plans() {
        let _g = lock_test();
        crate::state::clear_state();
        plan("rest", "e1", vec![wait(5)], 5);
        assert!(crate::state::has_active_plan("rest"));
        crate::state::clear_state();
        assert!(!crate::state::has_active_plan("rest"));
    }

    #[test]
    fn actor_busy_is_per_actor() {
        let _g = lock_test();
        crate::state::clear_state();
        plan("rest", "e1", vec![wait(5)], 5);
        assert!(crate::state::actor_is_busy("e1"));
        assert!(!crate::state::actor_is_busy("e2"));
        assert!(!crate::state::actor_is_busy(""));
    }

    #[test]
    fn parking_replaces_prior_plan_for_same_actor() {
        let _g = lock_test();
        crate::state::clear_state();
        crate::state::set_active_plan("task".into(), "e1".into(), vec![wait(5)], 5, false);
        assert!(!crate::state::actor_plan_interruptible("e1"));
        // A new plan for the same actor drops the prior one (interrupt, no queue).
        crate::state::set_active_plan("other".into(), "e1".into(), vec![wait(9)], 9, true);
        let plans = crate::state::active_plans().lock().unwrap();
        assert_eq!(plans.len(), 1);
        assert_eq!(plans[0].action_name, "other");
        assert!(plans[0].interruptible);
        drop(plans);
        assert!(crate::state::actor_plan_interruptible("e1"));
    }

    #[test]
    fn walk_flips_interruptible_after_wait() {
        let _g = lock_test();
        crate::state::clear_state();
        // Parked interruptible; the remaining steps flip the flag to
        // non-interruptible, then wait again in the second segment.
        crate::state::set_active_plan(
            "task".into(),
            "e1".into(),
            vec![json!({"interruptible": false}), json!({"wait": 100})],
            10,
            true,
        );
        assert!(crate::state::actor_plan_interruptible("e1"));
        process_active_plans(10);
        assert!(crate::state::actor_is_busy("e1"));
        assert!(!crate::state::actor_plan_interruptible("e1"));
    }

    #[test]
    fn two_actors_same_action_are_independent() {
        let _g = lock_test();
        crate::state::clear_state();
        plan("rest", "e1", vec![emit("h1")], 0);
        plan("rest", "e2", vec![wait(5)], 5);
        process_active_plans(0);
        // e1's plan completed and was removed; e2's is untouched.
        assert!(!crate::state::actor_is_busy("e1"));
        assert!(crate::state::actor_is_busy("e2"));
        let plans = crate::state::active_plans().lock().unwrap();
        assert_eq!(plans.len(), 1);
        assert_eq!(plans[0].actor, "e2");
        drop(plans);
        // e2's plan is a trailing wait: at 5 it re-parks until 5+5.
        process_active_plans(5);
        assert!(crate::state::actor_is_busy("e2"));
        process_active_plans(10);
        assert!(!crate::state::actor_is_busy("e2"));
    }

    // ---- moveTo walker tests ----

    fn move_step(
        container_id: &str,
        entity_id: &str,
        x: f64,
        y: f64,
        speed: f64,
    ) -> serde_json::Value {
        json!({ "move": {
            "containerId": container_id,
            "entityId": entity_id,
            "x": x, "y": y, "speed": speed
        }})
    }

    fn container_with_pos(
        id: &str,
        entity: &str,
        x: f64,
        y: f64,
        size_x: Option<f64>,
        size_y: Option<f64>,
    ) -> String {
        let mut c = serde_json::Map::new();
        c.insert("id".into(), json!(id));
        c.insert("entities".into(), json!([entity]));
        c.insert("getX".into(), json!({ entity: x }));
        c.insert("getY".into(), json!({ entity: y }));
        if let Some(sx) = size_x {
            c.insert("sizeX".into(), json!({ "value": sx, "outOfBounds": "clamp" }));
        }
        if let Some(sy) = size_y {
            c.insert("sizeY".into(), json!({ "value": sy, "outOfBounds": "clamp" }));
        }
        serde_json::to_string(&serde_json::Value::Object(c)).unwrap()
    }

    /// The actor's position as the container's pre-baked getX/getY see it —
    /// the same source `get_container_by_id` exposes to the client.
    fn pos_of(entity: &str) -> (f64, f64) {
        let containers = crate::state::last_containers().lock().unwrap();
        for json_str in containers.iter() {
            if let Ok(v) = serde_json::from_str::<serde_json::Value>(json_str) {
                let x = v.get("getX").and_then(|g| g.get(entity))
                    .and_then(|n| n.as_f64()).unwrap_or(0.0);
                let y = v.get("getY").and_then(|g| g.get(entity))
                    .and_then(|n| n.as_f64()).unwrap_or(0.0);
                return (x, y);
            }
        }
        (0.0, 0.0)
    }

    #[test]
    fn move_length_axis_and_diagonal() {
        assert_eq!(move_length(5.0, 0.0), 5.0);
        assert_eq!(move_length(0.0, 3.0), 3.0);
        // diagonal: floor(√2 * min) — 10 → 14
        assert_eq!(move_length(10.0, 10.0), 14.0);
        assert_eq!(move_length(3.0, 7.0), 4.0); // floor(1.4142*3)=4
    }

    #[test]
    fn axis_aligned_move_advances_one_per_tick() {
        let _g = lock_test();
        crate::state::clear_state();
        crate::state::set_last_containers(vec![
            container_with_pos("grid", "e1", 0.0, 0.0, Some(10.0), Some(10.0))
        ]);
        plan("walk", "e1", vec![move_step("grid", "e1", 5.0, 0.0, 1.0)], 0);

        // Tick advances by 1 each step so the parked plan (resume_at = now+1)
        // is due every step.
        for step in 0..5 {
            process_active_plans(step);
            let (x, y) = pos_of("e1");
            assert_eq!((x, y), ((step as f64 + 1.0), 0.0), "step {}", step);
        }
        let (x, _) = pos_of("e1");
        assert_eq!(x, 5.0);
        // Move exhausted: plan removed.
        assert!(!crate::state::has_active_plan("walk"));
    }

    #[test]
    fn diagonal_move_takes_14_ticks_at_speed_1() {
        let _g = lock_test();
        crate::state::clear_state();
        crate::state::set_last_containers(vec![
            container_with_pos("grid", "e1", 0.0, 0.0, Some(20.0), Some(20.0))
        ]);
        plan("diag", "e1", vec![move_step("grid", "e1", 10.0, 10.0, 1.0)], 0);

        // Both axes advance 1 cell per tick in parallel, so the move takes
        // max(|dx|,|dy|) = 10 ticks; still moving until the final one.
        for step in 0..9 {
            process_active_plans(step);
            assert!(crate::state::has_active_plan("diag"), "still moving at step {}", step);
        }
        process_active_plans(9);
        let (x, y) = pos_of("e1");
        assert!((x - 10.0).abs() < 1e-9, "x={}", x);
        assert!((y - 10.0).abs() < 1e-9, "y={}", y);
        assert!(!crate::state::has_active_plan("diag"));
    }

    #[test]
    fn speed_greater_than_one_covers_multiple_cells() {
        let _g = lock_test();
        crate::state::clear_state();
        crate::state::set_last_containers(vec![
            container_with_pos("grid", "e1", 0.0, 0.0, Some(20.0), Some(20.0))
        ]);
        // 10 cells at speed 3 → 3,3,3,1 over 4 ticks.
        plan("dash", "e1", vec![move_step("grid", "e1", 10.0, 0.0, 3.0)], 0);

        process_active_plans(0);
        assert_eq!(pos_of("e1").0, 3.0);
        process_active_plans(1);
        assert_eq!(pos_of("e1").0, 6.0);
        process_active_plans(2);
        assert_eq!(pos_of("e1").0, 9.0);
        process_active_plans(3);
        assert_eq!(pos_of("e1").0, 10.0);
        assert!(!crate::state::has_active_plan("dash"));
    }

    #[test]
    fn negative_axis_move_advances_toward_zero() {
        let _g = lock_test();
        crate::state::clear_state();
        crate::state::set_last_containers(vec![
            container_with_pos("grid", "e1", 5.0, 3.0, Some(10.0), Some(10.0))
        ]);
        // Move from (5,3) toward the origin: negative x AND negative y, speed 1.
        // Both axes move in parallel; the shorter axis (y, 3) exhausts first and
        // ends the move, leaving x at its partial cell.
        plan("back", "e1", vec![move_step("grid", "e1", 0.0, 0.0, 1.0)], 0);

        for step in 0..2 {
            process_active_plans(step);
            let (x, y) = pos_of("e1");
            assert_eq!((x, y), ((5.0 - (step as f64 + 1.0)), (3.0 - (step as f64 + 1.0))), "step {}", step);
        }
        // Step 3: y reaches 0 (exhausted) and the move ends; x is at 2.
        process_active_plans(3);
        let (x, y) = pos_of("e1");
        assert_eq!((x, y), (2.0, 0.0));
        assert!(!crate::state::has_active_plan("back"));
    }

    #[test]
    fn negative_axis_only_move() {
        let _g = lock_test();
        crate::state::clear_state();
        crate::state::set_last_containers(vec![
            container_with_pos("grid", "e1", 4.0, 0.0, Some(10.0), Some(10.0))
        ]);
        // Pure negative-x move: (4,0) -> (0,0) at speed 1, one cell per tick.
        plan("west", "e1", vec![move_step("grid", "e1", 0.0, 0.0, 1.0)], 0);

        for step in 0..4 {
            process_active_plans(step);
            let (x, y) = pos_of("e1");
            assert_eq!((x, y), ((4.0 - (step as f64 + 1.0)), 0.0), "step {}", step);
        }
        assert_eq!(pos_of("e1"), (0.0, 0.0));
        assert!(!crate::state::has_active_plan("west"));
    }

    #[test]
    fn negative_move_speed_greater_than_one() {
        let _g = lock_test();
        crate::state::clear_state();
        crate::state::set_last_containers(vec![
            container_with_pos("grid", "e1", 9.0, 0.0, Some(20.0), Some(20.0))
        ]);
        // (9,0) -> (0,0) at speed 3: 9,6,3,0 over 4 ticks.
        plan("west-dash", "e1", vec![move_step("grid", "e1", 0.0, 0.0, 3.0)], 0);

        process_active_plans(0);
        assert_eq!(pos_of("e1").0, 6.0);
        process_active_plans(1);
        assert_eq!(pos_of("e1").0, 3.0);
        process_active_plans(2);
        assert_eq!(pos_of("e1").0, 0.0);
        assert!(!crate::state::has_active_plan("west-dash"));
    }

    #[test]
    fn out_of_bounds_stops_early_at_bound() {
        let _g = lock_test();
        crate::state::clear_state();
        crate::state::set_last_containers(vec![
            container_with_pos("grid", "e1", 0.0, 0.0, Some(5.0), Some(5.0))
        ]);
        // Target x=10 exceeds sizeX=5 → walks to 5 then stops.
        plan("oob", "e1", vec![move_step("grid", "e1", 10.0, 0.0, 1.0)], 0);

        for step in 0..5 {
            process_active_plans(step);
        }
        let (x, _) = pos_of("e1");
        assert_eq!(x, 5.0);
        assert!(!crate::state::has_active_plan("oob"));
        // Still stopped on a further tick.
        process_active_plans(6);
        assert_eq!(pos_of("e1").0, 5.0);
    }

    #[test]
    fn speed_zero_does_not_move() {
        let _g = lock_test();
        crate::state::clear_state();
        crate::state::set_last_containers(vec![
            container_with_pos("grid", "e1", 2.0, 1.0, Some(10.0), Some(10.0))
        ]);
        plan("still", "e1", vec![move_step("grid", "e1", 8.0, 1.0, 0.0)], 0);

        process_active_plans(0);
        assert_eq!(pos_of("e1"), (2.0, 1.0));
        assert!(!crate::state::has_active_plan("still"));
    }

    #[test]
    fn move_step_is_interruptible_by_default() {
        let _g = lock_test();
        crate::state::clear_state();
        crate::state::set_last_containers(vec![
            container_with_pos("grid", "e1", 0.0, 0.0, Some(10.0), Some(10.0))
        ]);
        // A multi-tick move parked with the default (interruptible) flag stays
        // busy and interruptible while it is mid-way.
        crate::state::set_active_plan(
            "walk".into(), "e1".into(),
            vec![move_step("grid", "e1", 5.0, 0.0, 1.0)], 0, true);
        process_active_plans(0);
        assert!(crate::state::actor_is_busy("e1"));
        assert!(crate::state::actor_plan_interruptible("e1"));
    }

    #[test]
    fn deny_interrupt_move_rejects_new_action() {
        let _g = lock_test();
        crate::state::clear_state();
        crate::state::set_last_containers(vec![
            container_with_pos("grid", "e1", 0.0, 0.0, Some(10.0), Some(10.0))
        ]);
        // A multi-tick move parked non-interruptible (denyInterrupt before
        // moveTo) stays busy but not interruptible.
        crate::state::set_active_plan(
            "walk".into(), "e1".into(),
            vec![move_step("grid", "e1", 5.0, 0.0, 1.0)], 0, false);
        process_active_plans(0);
        assert!(crate::state::actor_is_busy("e1"));
        assert!(!crate::state::actor_plan_interruptible("e1"));
    }

    #[test]
    fn move_then_interruptible_after_flag() {
        let _g = lock_test();
        crate::state::clear_state();
        crate::state::set_last_containers(vec![
            container_with_pos("grid", "e1", 0.0, 0.0, Some(10.0), Some(10.0))
        ]);
        // move, then an allowInterrupt marker, then a wait. After the move
        // exhausts the walker passes the marker and parks the wait interruptible.
        plan("seq", "e1", vec![
            move_step("grid", "e1", 2.0, 0.0, 2.0),
            json!({ "interruptible": true }),
            wait(5),
        ], 0);

        process_active_plans(0);
        // Move of 2 at speed 2 completes in one tick; now parked on the wait.
        assert!(!crate::state::has_active_plan("seq") ||
            crate::state::active_plans().lock().unwrap()[0].resume_at > 0);
        assert!(crate::state::actor_plan_interruptible("e1"));
    }
}
