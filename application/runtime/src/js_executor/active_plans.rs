//! Per-tick walker for parked action plans.
//!
//! A plan is plain data (recorded steps), so walking needs no JS engine:
//! due plans walk emit steps into the pending-effects queue and park
//! again on wait steps, until the steps run out and the plan completes.

use crate::state::ActivePlan;

pub fn process_active_plans(now: i64) {
    let mut due: Vec<ActivePlan> = {
        let plans = crate::state::active_plans().lock().unwrap();
        plans.iter().filter(|p| p.resume_at <= now).cloned().collect()
    };
    if due.is_empty() { return; }

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
}
