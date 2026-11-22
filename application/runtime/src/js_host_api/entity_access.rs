use crate::state::{entity_map, entities_inside_area, Entity};
use rquickjs::{function::Func, Context};

pub fn install(ctx: &Context) -> rquickjs::Result<()> {
    ctx.with(|ctx| {
        let globals = ctx.globals();
        globals.set("__nativeEntityRead", Func::from(|id: String| {
            serde_json::to_string(&entity_map().lock().unwrap().get(&id)).unwrap()
        }))?;
        globals.set("__nativeEntityIds", Func::from(|| {
            serde_json::to_string(&entity_map().lock().unwrap().keys().collect::<Vec<_>>()).unwrap()
        }))?;
        globals.set("__nativeEntityDeclare", Func::from(|json: String| {
            if let Ok(mut entity) = serde_json::from_str::<Entity>(&json) {
                let mut entities = entity_map().lock().unwrap();
                entity.normalize();
                entity.log_dangling(&entities);
                entities.insert(entity.id.clone(), entity);
            }
        }))?;
        globals.set("__nativeEntityField", Func::from(|id: String, map: String, key: String, json: String| {
            let mut entities = entity_map().lock().unwrap();
            let Some(entity) = entities.get_mut(&id) else { return };
            match map.as_str() {
                "numberMap" => match serde_json::from_str::<f64>(&json) {
                    Ok(value) => { entity.number_map.insert(key, value); }
                    Err(_) if json == "null" => { entity.number_map.remove(&key); }
                    Err(_) => {}
                },
                "stringMap" => match serde_json::from_str::<String>(&json) {
                    Ok(value) => { entity.string_map.insert(key, value); }
                    Err(_) if json == "null" => { entity.string_map.remove(&key); }
                    Err(_) => {}
                },
                _ => {}
            }
        }))?;
        globals.set("__nativeMembership", Func::from(|id: String, list: String, member: String, add: bool| {
            let mut entities = entity_map().lock().unwrap();
            let dangling = !entities.contains_key(&member);
            let Some(members) = entities.get_mut(&id)
                .and_then(|group| group.entity_list_map.as_mut())
                .and_then(|lists| lists.get_mut(&list)) else { return };
            if add {
                if !members.contains(&member) {
                    if dangling { runtime_log!("group {:?} list {:?}: dangling member {:?}", id, list, member); }
                    members.push(member);
                }
            } else {
                members.retain(|item| item != &member);
            }
        }))?;
        globals.set("__nativeInsideArea", Func::from(|id: String, area: String| {
            serde_json::to_string(&entities_inside_area(&id, &area)).unwrap()
        }))?;
        ctx.eval::<(), _>("delete globalThis.__entityMap;")
    })
}

#[cfg(test)]
mod tests {
    use super::*;
    use crate::state::{memberships, seed_group};

    #[test]
    fn membership_edits_are_named_deduplicated_and_keep_dangling_ids() {
        let _guard = crate::state::test_lock();
        crate::state::clear_state();
        seed_group("group", "board", &[("token", 2.0, 1.0)]);
        seed_group("group", "bench", &[("token", 2.0, 1.0)]);
        let rt = crate::js_runtime::create_runtime().unwrap();
        let ctx = crate::js_runtime::create_context(&rt).unwrap();
        crate::js_host_api::install_host_api(&ctx).unwrap();
        install(&ctx).unwrap();
        ctx.with(|ctx| ctx.eval::<(), _>(r#"
            const token = host.setEntity({ id: host.string.of("token"), numberMap: { x: 2 } });
            if (String(token.id) !== "token" || token.number_map.get("x").orElse(null).value !== 2)
                throw Error("setEntity must return a readable expression for the declared entity");
            if (token.addMember || token.number_map.put) throw Error("setEntity result is mutable");
            const group = __evalEntityFilter({ filters: [e => e.id.isContainingExactly("group")] }, true)
                .exactlyOne().orElse(null);
            group.addMember("board", token);
            const added = group.addMember("board", "missing");
            if (String(added.id) !== "group" || added.addMember || added.string_map.put)
                throw Error("addMember must return the group as read-only");
            group.addMember("board", "missing");
            const removed = group.removeMember("bench", { id: "token" });
            if (String(removed.id) !== "group" || typeof removed.addMember !== "function")
                throw Error("removeMember must return the group as mutable");
            if (group.members("board").count().value !== 2) throw Error("board count");
            if (group.members("bench").count().value !== 0) throw Error("bench count");
        "#)).unwrap();
        let entities = entity_map().lock().unwrap();
        assert_eq!(entities["group"].members("board").unwrap(), ["token", "missing"]);
        assert_eq!(memberships(&entities, "token"), vec![("group".into(), "board".into())]);
        assert!(!entities.contains_key("missing"));
    }
}
