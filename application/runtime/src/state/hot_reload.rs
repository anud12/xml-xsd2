use super::EntityMap;

/// Keep live scalar values, while fresh declarations own polygons and named
/// membership for redeclared entities. Runtime-created entities survive.
pub fn preserve_live_entities(entities: &mut EntityMap, preserved: EntityMap) {
    for (id, previous) in preserved {
        match entities.entry(id) {
            std::collections::hash_map::Entry::Occupied(mut entry) => {
                let fresh = entry.get_mut();
                fresh.string_map.extend(previous.string_map);
                fresh.number_map.extend(previous.number_map);
            }
            std::collections::hash_map::Entry::Vacant(entry) => { entry.insert(previous); }
        }
    }
}

/// Drops pending and scheduled effects that the reloaded module no longer
/// registers: their JS closures lived in the replaced sim context, so firing
/// them would be a silent no-op at best. Effects the module re-declares
/// resolve in the fresh context and keep their schedule.
pub fn drop_unresolvable_effects() {
    let Some(ctx) = crate::js_executor::sim_ctx::ctx() else { return; };
    let lookup = |name: &str| {
        crate::js_executor::lookup_effect(ctx, name)
    };
    crate::state::pending_effects().lock().unwrap().retain(|n| lookup(n));
    crate::state::scheduled_effects().lock().unwrap().retain(|e| lookup(&e.name));
}

#[cfg(test)]
mod tests {
    use super::*;
    use crate::state::ensure_entity;

    #[test]
    fn live_values_survive_without_recomputing_member_placement() {
        let mut declared = EntityMap::new();
        let entity = ensure_entity(&mut declared, "e1");
        entity.number_map.extend([("column".into(), 0.0), ("row".into(), 1.0)]);
        entity.string_map.insert("xKey".into(), "column".into());
        entity.string_map.insert("yKey".into(), "row".into());
        entity.string_map.insert("status".into(), "idle".into());
        let mut preserved = EntityMap::new();
        let live = ensure_entity(&mut preserved, "e1");
        live.number_map.insert("column".into(), 3.0);
        live.string_map.insert("status".into(), "busy".into());
        ensure_entity(&mut preserved, "created").number_map.insert("hp".into(), 7.0);

        preserve_live_entities(&mut declared, preserved);

        assert_eq!(declared["e1"].position(), (3.0, 1.0));
        assert_eq!(declared["e1"].string_map["status"], "busy");
        assert_eq!(declared["created"].number("hp"), Some(7.0));
    }
}
