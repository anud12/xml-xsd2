//! Per-tick walker for parked action plans.
//!
//! A plan is plain data (recorded steps), so walking needs no JS engine:
//! due plans walk emit steps into the pending-effects queue, advance move
//! steps by writing the actor's position, and park again on wait/move steps,
//! until the steps run out and the plan completes.

use crate::state::ActivePlan;

/// Q16.16 fixed-point scale: 16 fractional bits. A speed of 1 is 1 GTU per
/// tick; the direction components and the per-axis accumulators are expressed
/// in 1/65536 GTU so pooled (sub-unit) motion survives integer math.
const Q16: i64 = 1 << 16;

/// Truncating integer square root of `n` (Newton iteration, no floats).
/// A perfect square rounds up; otherwise the result is the largest `r` with
/// `r*r <= n`.
fn isqrt(n: i64) -> i64 {
    if n <= 0 {
        return 0;
    }
    let mut x = n;
    let mut y = (x + 1) / 2;
    while y < x {
        x = y;
        y = (x + n / x) / 2;
    }
    x
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

/// Find the entity number-data keys holding the actor's position for this
/// container: the persisted `xKey`/`yKey` (the keys the container's position
/// accessors read), falling back to the literal `x`/`y` when the container
/// JSON carries no key names. Either may be empty if the container declares
/// no such accessor for the entity.
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
            let x_in = v.get("getX").and_then(|g| g.get(entity_id))
                .and_then(|n| n.as_f64()).is_some();
            let y_in = v.get("getY").and_then(|g| g.get(entity_id))
                .and_then(|n| n.as_f64()).is_some();
            let xk = v.get("xKey").and_then(|k| k.as_str()).filter(|s| !s.is_empty())
                .map(|s| s.to_string()).unwrap_or_else(|| "x".to_string());
            let yk = v.get("yKey").and_then(|k| k.as_str()).filter(|s| !s.is_empty())
                .map(|s| s.to_string()).unwrap_or_else(|| "y".to_string());
            return (
                if x_in { xk } else { String::new() },
                if y_in { yk } else { String::new() },
            );
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

/// Apply a teleport to the actor: write its position into the entity number
/// data (under the container's position keys) and rebake the container's
/// `getX`/`getY` so `get_container_by_id` reflects it immediately. A bound is
/// the container's max coordinate; the lower bound is 0. A `None` bound leaves
/// that axis unclamped. This is a synchronous write — a teleport is not walked.
pub fn apply_teleport(
    containers: &mut Vec<String>,
    container_id: &str,
    entity_id: &str,
    x: f64,
    y: f64,
    clamp: bool,
) {
    let (bx, by) = container_bounds(containers, container_id);
    let mut nx = x;
    let mut ny = y;
    if clamp {
        nx = nx.max(0.0);
        ny = ny.max(0.0);
        if let Some(b) = bx { if nx > b { nx = b; } }
        if let Some(b) = by { if ny > b { ny = b; } }
    }
    write_position(containers, container_id, entity_id, nx, ny);
}

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
                // Advance the move one cell per elapsed GTU. The move step
                // tracks `lastTick` — the GTU it last advanced on (absent for
                // a fresh step, i.e. not yet advanced). Each call covers the
                // GTUs since then, so a normally-due plan advances one cell
                // and a plan resumed after the caller jumped several GTU in
                // one RunIteration catches up by one cell per GTU.
                let last = plan.steps[i].get("move")
                    .and_then(|m| m.get("lastTick"))
                    .and_then(|v| v.as_i64());
                let ticks = match last {
                    Some(l) => (now - l).max(1),
                    None => 1,
                };
                let mut remaining = true;
                for _ in 0..ticks.min(100000) {
                    remaining = advance_move_step(&mut plan.steps[i], now, &mut containers);
                    if !remaining { break; }
                }
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
///
/// Movement is integer Bresenham stepping in Q16.16 fixed-point: each axis
/// pools the exact sub-unit motion `delta * speed` per tick into a signed
/// remainder, and releases a whole GTU only when the remainder crosses the
/// distance scale (`step = trunc(rem / (dist * Q16))`). Because the remainder
/// carries the truncation loss, each axis advances at its exact rate
/// `delta / dist` and the move lands exactly on the target — a 45° move covers
/// 1 GTU of *path* per tick at speed 1 (not √2), and an odd angle (e.g. 16.7°)
/// walks a straight staircase toward the target rather than a 45°-then-axis
/// detour.
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
    let tx = move_obj.get("x").and_then(|v| v.as_f64()).map(|n| n.round() as i64).unwrap_or(0);
    let ty = move_obj.get("y").and_then(|v| v.as_f64()).map(|n| n.round() as i64).unwrap_or(0);

    // Lazy setup: capture the start position, segment deltas and distance
    // exactly once, on the first advance. The per-axis remainders start at 0.
    if move_obj.get("startX").is_none() {
        let (cx, cy) = match current_position(containers, &container_id, &entity_id) {
            Some(c) => c,
            None => return false,
        };
        let sx = cx.round() as i64;
        let sy = cy.round() as i64;
        move_obj.insert("startX".into(), serde_json::json!(sx));
        move_obj.insert("startY".into(), serde_json::json!(sy));
        move_obj.insert("deltaX".into(), serde_json::json!(tx - sx));
        move_obj.insert("deltaY".into(), serde_json::json!(ty - sy));
        let dist = isqrt((tx - sx) * (tx - sx) + (ty - sy) * (ty - sy));
        move_obj.insert("dist".into(), serde_json::json!(dist));
        move_obj.insert("remX".into(), serde_json::json!(0));
        move_obj.insert("remY".into(), serde_json::json!(0));
        move_obj.insert("posX".into(), serde_json::json!(sx));
        move_obj.insert("posY".into(), serde_json::json!(sy));
    }

    let sx = move_obj.get("startX").and_then(|v| v.as_i64()).unwrap_or(0);
    let sy = move_obj.get("startY").and_then(|v| v.as_i64()).unwrap_or(0);
    let dx = move_obj.get("deltaX").and_then(|v| v.as_i64()).unwrap_or(0);
    let dy = move_obj.get("deltaY").and_then(|v| v.as_i64()).unwrap_or(0);
    let dist = move_obj.get("dist").and_then(|v| v.as_i64()).unwrap_or(0);
    let mut remx = move_obj.get("remX").and_then(|v| v.as_i64()).unwrap_or(0);
    let mut remy = move_obj.get("remY").and_then(|v| v.as_i64()).unwrap_or(0);
    // The logical position (in GTU, unclamped): the source of truth for
    // arrival. The *written* position is this clamped to the container bounds,
    // so an out-of-bounds target parks the actor at the boundary edge.
    let mut lx = move_obj.get("posX").and_then(|v| v.as_i64()).unwrap_or(sx);
    let mut ly = move_obj.get("posY").and_then(|v| v.as_i64()).unwrap_or(sy);

    // Zero-length move (start == target): nothing to do.
    if dist == 0 {
        return false;
    }

    // Resolve speed (re-read each tick so mid-move changes take effect).
    let speed = move_obj.get("speed")
        .and_then(|v| {
            if let Some(n) = v.as_f64() { return Some(n); }
            if let Some(s) = v.as_str() { return s.parse::<f64>().ok(); }
            None
        })
        .unwrap_or(0.0);

    // speed <= 0: "try, then stop" — no advance, the move ends here.
    if speed <= 0.0 {
        return false;
    }
    // Pooled motion this tick, per axis, in Q16.16 (exact for any speed):
    // delta * speed, keeping the sub-unit fraction in the remainder.
    let dxp = (dx as f64 * speed * Q16 as f64).round() as i64;
    let dyp = (dy as f64 * speed * Q16 as f64).round() as i64;

    // Pool and release: the axis moves a whole GTU when the remainder crosses
    // the distance scale. Truncation toward zero keeps negative deltas
    // advancing in the negative direction.
    remx += dxp;
    remy += dyp;
    let denom = dist * Q16;
    let mut stepx = if remx >= 0 { remx / denom } else { -((-remx) / denom) };
    let mut stepy = if remy >= 0 { remy / denom } else { -((-remy) / denom) };
    // Clamp each axis to the path still remaining to the target. On the final
    // tick the pooled motion (delta * speed) can exceed what is left, and
    // truncation would otherwise release the axis's *full* remaining delta —
    // landing past the target (e.g. (0,0)->(10,10) at speed 10 lands at
    // (11,11)) so the arrival check never fires and the move parks forever.
    // Clamping stops the actor exactly at the destination. Out-of-bounds
    // targets are unaffected: the remaining delta exceeds the segment, so the
    // clamp never binds and the logical position keeps tracking the segment.
    if dx >= 0 {
        let rem = tx - lx;
        if stepx > rem { stepx = rem; }
    } else {
        let rem = tx - lx;
        if stepx < rem { stepx = rem; }
    }
    if dy >= 0 {
        let rem = ty - ly;
        if stepy > rem { stepy = rem; }
    } else {
        let rem = ty - ly;
        if stepy < rem { stepy = rem; }
    }
    remx -= stepx * denom;
    remy -= stepy * denom;
    lx += stepx;
    ly += stepy;

    // Clamp the logical position into the container's [0, bound] for writing.
    let (bx, by) = container_bounds(containers, &container_id);
    let mut nx = lx;
    let mut ny = ly;
    if nx < 0 { nx = 0; }
    if ny < 0 { ny = 0; }
    if let Some(b) = bx { let b = b.round() as i64; if nx > b { nx = b; } }
    if let Some(b) = by { let b = b.round() as i64; if ny > b { ny = b; } }

    move_obj.insert("remX".into(), serde_json::json!(remx));
    move_obj.insert("remY".into(), serde_json::json!(remy));
    move_obj.insert("posX".into(), serde_json::json!(lx));
    move_obj.insert("posY".into(), serde_json::json!(ly));
    // Record this GTU so a later (jumped) call advances one step per GTU.
    move_obj.insert("lastTick".into(), serde_json::json!(now));
    write_position(containers, &container_id, &entity_id, nx as f64, ny as f64);

    // Exhausted once the *logical* position reaches the target. An out-of-
    // bounds target never satisfies this (the logical position keeps tracking
    // the segment), so the actor holds at the bound edge and the move stays
    // parked — "it tries, then stops."
    !(lx == tx && ly == ty)
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

        // Bresenham stepping: dist = isqrt(200) = 14, each axis pools
        // 10/14 ≈ 0.714 GTU per tick and moves a whole unit when the remainder
        // crosses the distance scale. Both axes stay in lockstep (symmetric
        // deltas), moving on ticks 2,3,5,6,7,9,10,12,13,14 — the dispatch tick
        // (0) pools (no whole unit yet). Positions after ticks 0..13:
        let expected: [(i32, i32); 14] = [
            (0,0),(1,1),(2,2),(2,2),(3,3),(4,4),(5,5),(5,5),
            (6,6),(7,7),(7,7),(8,8),(9,9),(10,10),
        ];
        for (step, &(ex, ey)) in expected.iter().enumerate() {
            process_active_plans(step as i64);
            let (x, y) = pos_of("e1");
            assert_eq!((x as i32, y as i32), (ex, ey), "step {}", step);
            if step < 13 {
                assert!(crate::state::has_active_plan("diag"), "still moving at step {}", step);
            }
        }
        assert!(!crate::state::has_active_plan("diag"));
    }

    #[test]
    fn odd_angle_move_walks_straight_line_at_speed_1() {
        let _g = lock_test();
        crate::state::clear_state();
        crate::state::set_last_containers(vec![
            container_with_pos("grid", "e1", 0.0, 0.0, Some(20.0), Some(20.0))
        ]);
        // Target (10,3): atan2(3,10) ≈ 16.7° — neither 45° nor axis-aligned.
        plan("odd", "e1", vec![move_step("grid", "e1", 10.0, 3.0, 1.0)], 0);

        // dist = isqrt(109) = 10. x pools 10/10 = 1.0 GTU per tick (moves
        // every tick); y pools 3/10 = 0.3 and moves a whole unit on ticks 4,
        // 7 and 10. The actor walks a straight ~16.7° staircase — no
        // 45°-then-axis detour — and lands exactly on the target at tick 10:
        let expected: [(i32, i32); 10] = [
            (1,0),(2,0),(3,0),(4,1),(5,1),(6,1),(7,2),(8,2),(9,2),(10,3),
        ];
        for (step, &(ex, ey)) in expected.iter().enumerate() {
            process_active_plans(step as i64);
            let (x, y) = pos_of("e1");
            assert_eq!((x as i32, y as i32), (ex, ey), "step {}", step);
            if step < 9 {
                assert!(crate::state::has_active_plan("odd"), "still moving at step {}", step);
            }
        }
        assert!(!crate::state::has_active_plan("odd"));
    }

    #[test]
    fn speed_greater_than_one_covers_multiple_cells() {
        let _g = lock_test();
        crate::state::clear_state();
        crate::state::set_last_containers(vec![
            container_with_pos("grid", "e1", 0.0, 0.0, Some(20.0), Some(20.0))
        ]);
        // 10 cells at speed 3: the walker covers `speed` GTU of *path* per
        // tick, so a pure-x move advances 3 cells/tick → 3, 6, 9. On the final
        // tick the pooled motion (3) exceeds the 1 GTU still remaining, so the
        // step is clamped to the remaining path and the move lands exactly at
        // 10 (no overshoot to 12) and exhausts.
        plan("dash", "e1", vec![move_step("grid", "e1", 10.0, 0.0, 3.0)], 0);

        process_active_plans(0);
        assert_eq!(pos_of("e1").0, 3.0);
        process_active_plans(1);
        assert_eq!(pos_of("e1").0, 6.0);
        process_active_plans(2);
        assert_eq!(pos_of("e1").0, 9.0);
        process_active_plans(3);
        assert_eq!(pos_of("e1").0, 10.0);
        // Move exhausted exactly at the destination.
        assert!(!crate::state::has_active_plan("dash"));
    }

    #[test]
    fn speed_greater_than_one_does_not_overshoot_on_final_tick() {
        let _g = lock_test();
        crate::state::clear_state();
        crate::state::set_last_containers(vec![
            container_with_pos("grid", "e1", 0.0, 0.0, Some(20.0), Some(20.0))
        ]);
        // (0,0) -> (10,0) at speed 10: the final tick's pooled amount (10 *
        // 10 * Q16) exceeds the remaining path, so without clamping the axis
        // would release its whole remaining delta and land at 20 (past the
        // target). The move must stop exactly at the destination.
        plan("no-overshoot", "e1", vec![move_step("grid", "e1", 10.0, 0.0, 10.0)], 0);

        process_active_plans(0);
        assert_eq!(pos_of("e1"), (10.0, 0.0), "must land exactly on the target");
        assert!(!crate::state::has_active_plan("no-overshoot"), "move must be exhausted");
    }

    #[test]
    fn speed_greater_than_one_diagonal_does_not_overshoot() {
        let _g = lock_test();
        crate::state::clear_state();
        crate::state::set_last_containers(vec![
            container_with_pos("grid", "e1", 0.0, 0.0, Some(20.0), Some(20.0))
        ]);
        // (0,0) -> (10,10) at speed 10: dist = isqrt(200) = 14. Without the
        // remaining-path clamp, each axis's final-tick pooled amount crosses the
        // distance scale and the logical position lands past (10,10) — the move
        // would never "land" and keep parking. It must stop exactly at (10,10).
        plan("no-overshoot-diag", "e1", vec![move_step("grid", "e1", 10.0, 10.0, 10.0)], 0);

        for step in 0..20 {
            process_active_plans(step);
            if !crate::state::has_active_plan("no-overshoot-diag") {
                let (x, y) = pos_of("e1");
                assert_eq!((x, y), (10.0, 10.0), "must land exactly on the target");
                return;
            }
        }
        panic!("move never exhausted (overshot the destination)");
    }

    #[test]
    fn negative_axis_move_advances_toward_zero() {
        let _g = lock_test();
        crate::state::clear_state();
        crate::state::set_last_containers(vec![
            container_with_pos("grid", "e1", 5.0, 3.0, Some(10.0), Some(10.0))
        ]);
        // Move from (5,3) toward the origin: negative x AND negative y, speed 1.
        // dist = isqrt(34) = 5. x pools -5/5 = -1.0 (moves a whole unit every
        // tick); y pools -3/5 = -0.6 and moves on ticks 2, 4 and 5 (the pooled
        // remainder crosses the distance scale). Both axes land on the
        // destination at tick 5:
        let expected: [(i32, i32); 5] = [
            (4,3),(3,2),(2,2),(1,1),(0,0),
        ];
        plan("back", "e1", vec![move_step("grid", "e1", 0.0, 0.0, 1.0)], 0);
        for (step, &(ex, ey)) in expected.iter().enumerate() {
            process_active_plans(step as i64);
            let (x, y) = pos_of("e1");
            assert_eq!((x as i32, y as i32), (ex, ey), "step {}", step);
            if step < 4 {
                assert!(crate::state::has_active_plan("back"), "still moving at step {}", step);
            }
        }
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
        // Target x=10 exceeds sizeX=5 → the written position clamps at 5 while
        // the *logical* position keeps tracking toward 10, so the move never
        // "lands" and stays parked (the actor holds at the bound edge).
        plan("oob", "e1", vec![move_step("grid", "e1", 10.0, 0.0, 1.0)], 0);

        for step in 0..5 {
            process_active_plans(step);
        }
        let (x, _) = pos_of("e1");
        assert_eq!(x, 5.0);
        // Out-of-bounds target: the move is still active (holds at the bound).
        assert!(crate::state::has_active_plan("oob"));
        // Still clamped on a further tick.
        process_active_plans(6);
        assert_eq!(pos_of("e1").0, 5.0);
        assert!(crate::state::has_active_plan("oob"));
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

    #[test]
    fn move_writes_position_to_persisted_position_keys() {
        let _g = lock_test();
        crate::state::clear_state();
        let c = json!({
            "id": "grid",
            "entities": ["e1"],
            "getX": { "e1": 0.0 },
            "getY": { "e1": 0.0 },
            "xKey": "column",
            "yKey": "row",
            "sizeX": { "value": 10.0, "outOfBounds": "clamp" },
            "sizeY": { "value": 10.0, "outOfBounds": "clamp" }
        });
        crate::state::set_last_containers(vec![serde_json::to_string(&c).unwrap()]);
        plan("walk", "e1", vec![move_step("grid", "e1", 2.0, 0.0, 1.0)], 0);
        process_active_plans(0);

        // The position lands under the container's real key names, not x/y.
        let nd = crate::state::last_entity_number_data().lock().unwrap();
        let em = nd.get("e1").expect("entity number data written");
        assert_eq!(em.get("column"), Some(&1.0));
        assert_eq!(em.get("row"), Some(&0.0));
        assert!(em.get("x").is_none());
    }
}
