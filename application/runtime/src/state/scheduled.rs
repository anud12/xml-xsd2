pub fn add_scheduled_effect(
    name: String, payload: serde_json::Value,
    next_exec_time: i64, reoccurrence_interval: i64,
) {
    let mut effects = super::scheduled_effects().lock().unwrap();
    effects.retain(|e| e.name != name);
    effects.push(super::ScheduledEffect {
        name, payload, next_exec_time, reoccurrence_interval,
        execution_count: 0,
    });
}

pub fn remove_scheduled_effect(name: &str) {
    let mut effects = super::scheduled_effects().lock().unwrap();
    effects.retain(|e| e.name != name);
}

pub fn get_due_scheduled_effects(current_elapsed: i64)
    -> Vec<super::ScheduledEffect>
{
    let mut effects = super::scheduled_effects().lock().unwrap();
    let mut due = Vec::new();
    for effect in effects.iter_mut() {
        while current_elapsed >= effect.next_exec_time
            && effect.reoccurrence_interval > 0
        {
            effect.execution_count += 1;
            due.push(effect.clone());
            effect.next_exec_time += effect.reoccurrence_interval;
        }
    }
    due
}
