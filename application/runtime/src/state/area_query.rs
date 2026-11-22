//! Spatial overlap between peers in at least one common named list.
use std::collections::BTreeSet;

use super::{area::{self, Poly}, entity_map, EntityMap};

pub fn set_entity_area(entity_id: &str, area_id: &str, poly: Vec<(f64, f64)>) {
    let mut entities = entity_map().lock().unwrap();
    if poly.len() >= 3 {
        super::ensure_entity(&mut entities, entity_id).polygons.insert(area_id.into(), poly);
    } else if let Some(entity) = entities.get_mut(entity_id) {
        entity.polygons.remove(area_id);
    }
}

pub fn entity_area(entity_id: &str, area_id: &str) -> Option<Vec<(f64, f64)>> {
    entity_map().lock().unwrap().get(entity_id)?.polygons.get(area_id).cloned()
}

pub fn entity_area_ids(entity_id: &str) -> Vec<String> {
    entity_map().lock().unwrap().get(entity_id)
        .map(|entity| entity.polygons.keys().cloned().collect()).unwrap_or_default()
}

/// Derived scan; no reverse index is stored or updated.
pub fn memberships(entities: &EntityMap, member: &str) -> Vec<(String, String)> {
    let mut result = Vec::new();
    for (id, group) in entities {
        if let Some(lists) = &group.entity_list_map {
            for (name, members) in lists {
                if members.iter().any(|id| id == member) {
                    result.push((id.clone(), name.clone()));
                }
            }
        }
    }
    result.sort();
    result
}

pub fn entities_inside_area(self_id: &str, area_id: &str) -> Vec<String> {
    let entities = entity_map().lock().unwrap();
    let Some(owner) = entities.get(self_id) else { return vec![] };
    let Some(local) = owner.polygons.get(area_id).and_then(|p| area::make_area(p))
        else { return vec![] };
    let (x, y) = owner.position();
    let world = local.translated(x, y);
    let mut peers = BTreeSet::new();
    for (group, list) in memberships(&entities, self_id) {
        if let Some(members) = entities[&group].members(&list) {
            peers.extend(members.iter().filter(|id| id.as_str() != self_id));
        }
    }
    peers.into_iter().filter_map(|id| {
        let member = entities.get(id)?;
        let (x, y) = member.position();
        let mut shapes = member.polygons.values().filter_map(|p| area::make_area(p)).peekable();
        let hit = if shapes.peek().is_none() {
            area::area_contains(&world, x, y)
        } else {
            shapes.any(|shape| area::areas_overlap(&world, &shape.translated(x, y)))
        };
        hit.then(|| id.clone())
    }).collect()
}

pub fn world_pieces(entity_id: &str, area_id: &str, x: f64, y: f64) -> Option<Vec<Poly>> {
    let local = entity_area(entity_id, area_id)?;
    Some(area::make_area(&local)?.translated(x, y))
}

#[cfg(test)]
mod tests {
    use super::*;
    use crate::state::test_lock;

    #[test]
    fn point_members_inside_a_rect_are_found() {
        let _g = test_lock();
        crate::state::clear_state();
        set_entity_area("room", "", vec![
            (0.0, 0.0), (100.0, 0.0), (100.0, 100.0), (0.0, 100.0),
        ]);
        crate::state::seed_group("c1", "members", &[
            ("room", 10.0, 10.0),
            ("npc-a", 30.0, 40.0),
            ("npc-b", 200.0, 200.0),
        ]);
        let got = entities_inside_area("room", "");
        assert_eq!(got, vec!["npc-a".to_string()]);
        crate::state::entity_map().lock().unwrap().clear();
    }

    #[test]
    fn polygon_member_overlaps_the_area() {
        let _g = test_lock();
        crate::state::clear_state();
        set_entity_area("room", "", vec![
            (0.0, 0.0), (100.0, 0.0), (100.0, 100.0), (0.0, 100.0),
        ]);
        set_entity_area("crate", "", vec![
            (0.0, 0.0), (20.0, 0.0), (20.0, 20.0), (0.0, 20.0),
        ]);
        crate::state::seed_group("c1", "members", &[
            ("room", 10.0, 10.0),
            ("crate", 40.0, 40.0), // 40..60 x 40..60: fully inside
            ("far", 150.0, 150.0),
        ]);
        let got = entities_inside_area("room", "");
        assert_eq!(got, vec!["crate".to_string()]);
        crate::state::entity_map().lock().unwrap().clear();
    }

    #[test]
    fn self_is_excluded_and_order_is_id_ascending() {
        let _g = test_lock();
        crate::state::clear_state();
        set_entity_area("room", "", vec![
            (0.0, 0.0), (100.0, 0.0), (100.0, 100.0), (0.0, 100.0),
        ]);
        crate::state::seed_group("c1", "members", &[
            ("room", 10.0, 10.0),
            ("zeta", 20.0, 20.0),
            ("alpha", 30.0, 30.0),
        ]);
        let got = entities_inside_area("room", "");
        assert_eq!(got, vec!["alpha".to_string(), "zeta".to_string()]);
        crate::state::entity_map().lock().unwrap().clear();
    }

    #[test]
    fn no_area_means_no_presence_and_empty_result() {
        let _g = test_lock();
        crate::state::clear_state();
        crate::state::seed_group("c1", "members", &[
            ("room", 10.0, 10.0),
            ("npc-a", 30.0, 40.0),
        ]);
        assert_eq!(entities_inside_area("room", ""), Vec::<String>::new());
        // An area-less *member* is a point; a point outside the area is not found.
        set_entity_area("room", "", vec![
            (0.0, 0.0), (10.0, 0.0), (10.0, 10.0), (0.0, 10.0),
        ]);
        let got = entities_inside_area("room", "");
        assert_eq!(got, Vec::<String>::new());
        crate::state::entity_map().lock().unwrap().clear();
    }

    #[test]
    fn point_in_aabb_but_outside_the_polygon_is_not_reported_inside() {
        let _g = test_lock();
        crate::state::clear_state();
        // "room" is a concave L (the cliff shape): a 200x128 box with the
        // top-right pocket (x 96..200, y 64..128) cut out.
        set_entity_area("room", "", vec![
            (0.0, 0.0), (200.0, 0.0), (200.0, 64.0), (96.0, 64.0), (96.0, 128.0), (0.0, 128.0),
        ]);
        crate::state::seed_group("c1", "members", &[
            ("room", 10.0, 10.0),
            // npc-a at world (150, 96): inside the room's AABB (10..210 x 10..138)
            // but in the pocket, which is OUTSIDE the polygon -> not inside.
            ("npc-a", 150.0, 96.0),
            // npc-b at world (40, 96): inside both the AABB and the polygon body
            // -> inside.
            ("npc-b", 40.0, 96.0),
        ]);
        let got = entities_inside_area("room", "");
        assert_eq!(got, vec!["npc-b".to_string()]);
        crate::state::entity_map().lock().unwrap().clear();
    }

    #[test]
    fn union_across_multiple_containers_is_deduped() {
        let _g = test_lock();
        crate::state::clear_state();
        set_entity_area("hall", "", vec![
            (0.0, 0.0), (50.0, 0.0), (50.0, 50.0), (0.0, 50.0),
        ]);
        // hall is a member of two containers; npc-x sits in both, once each.
        crate::state::seed_group("c1", "members", &[("hall", 5.0, 5.0), ("npc-x", 10.0, 10.0)]);
            crate::state::seed_group("c2", "members", &[("hall", 5.0, 5.0), ("npc-x", 10.0, 10.0), ("npc-y", 99.0, 99.0)]);
        let got = entities_inside_area("hall", "");
        // npc-x appears in both containers (deduped); npc-y is far away.
        assert_eq!(got, vec!["npc-x".to_string()]);
        crate::state::entity_map().lock().unwrap().clear();
    }

    #[test]
    fn named_lists_are_separate_spaces_and_share_intrinsic_position() {
        let _g = test_lock();
        crate::state::clear_state();
        set_entity_area("room", "floor", vec![(0.0, 0.0), (4.0, 0.0), (4.0, 4.0), (0.0, 4.0)]);
        crate::state::seed_group("group", "floor", &[("room", 0.0, 0.0), ("peer", 2.0, 2.0)]);
        crate::state::seed_group("group", "other", &[("unrelated", 2.0, 2.0)]);
        crate::state::seed_group("second", "members", &[("room", 0.0, 0.0), ("peer", 2.0, 2.0)]);
        assert_eq!(entities_inside_area("room", "floor"), ["peer"]);

        entity_map().lock().unwrap().get_mut("peer").unwrap().number_map.insert("x".into(), 10.0);
        assert!(entities_inside_area("room", "floor").is_empty());
    }

    #[test]
    fn entity_not_in_any_container_has_empty_result() {
        let _g = test_lock();
        crate::state::clear_state();
        set_entity_area("room", "", vec![
            (0.0, 0.0), (100.0, 0.0), (100.0, 100.0), (0.0, 100.0),
        ]);
        crate::state::seed_group("c1", "members", &[("other", 30.0, 40.0)]);
        assert_eq!(entities_inside_area("room", ""), Vec::<String>::new());
        crate::state::entity_map().lock().unwrap().clear();
    }

    #[test]
    fn named_area_is_resolved_before_the_query() {
        let _g = test_lock();
        crate::state::clear_state();
        // "room" declares two named areas: a small "home" box and a big "zone"
        // box. The named query resolves the requested area first.
        set_entity_area("room", "home", vec![
            (0.0, 0.0), (20.0, 0.0), (20.0, 20.0), (0.0, 20.0),
        ]);
        set_entity_area("room", "zone", vec![
            (0.0, 0.0), (100.0, 0.0), (100.0, 100.0), (0.0, 100.0),
        ]);
        crate::state::seed_group("c1", "members", &[
            ("room", 10.0, 10.0),
            ("npc-near", 15.0, 15.0),  // inside both home and zone
            ("npc-far", 80.0, 80.0),   // inside zone only
        ]);
        // The "home" area admits only the near npc ...
        assert_eq!(
            entities_inside_area("room", "home"),
            vec!["npc-near".to_string()]
        );
        // ... while the "zone" area admits both.
        assert_eq!(
            entities_inside_area("room", "zone"),
            vec!["npc-far".to_string(), "npc-near".to_string()]
        );
        // An unknown area name yields empty (resolved first, not found).
        assert_eq!(
            entities_inside_area("room", "nope"),
            Vec::<String>::new()
        );
        crate::state::entity_map().lock().unwrap().clear();
    }

    #[test]
    fn each_named_area_is_independent() {
        let _g = test_lock();
        crate::state::clear_state();
        set_entity_area("room", "home", vec![
            (0.0, 0.0), (20.0, 0.0), (20.0, 20.0), (0.0, 20.0),
        ]);
        set_entity_area("room", "zone", vec![
            (0.0, 0.0), (100.0, 0.0), (100.0, 100.0), (0.0, 100.0),
        ]);
        crate::state::seed_group("c1", "members", &[
            ("room", 10.0, 10.0),
            ("npc-near", 15.0, 15.0),  // inside both home and zone
            ("npc-far", 80.0, 80.0),   // inside zone only
        ]);
        // The name is required: querying "home" returns only the near npc.
        assert_eq!(
            entities_inside_area("room", "home"),
            vec!["npc-near".to_string()]
        );
        // Querying "zone" returns both (the far one is in zone but not home).
        assert_eq!(
            entities_inside_area("room", "zone"),
            vec!["npc-far".to_string(), "npc-near".to_string()]
        );
        // There is no implicit union: querying the default (unnamed) area
        // yields empty when the entity has no unnamed area.
        assert_eq!(
            entities_inside_area("room", ""),
            Vec::<String>::new()
        );
        crate::state::entity_map().lock().unwrap().clear();
    }

    #[test]
    fn area_ids_are_listed_and_cleared_per_area() {
        let _g = test_lock();
        crate::state::clear_state();
        set_entity_area("room", "b", vec![(0.0, 0.0), (5.0, 0.0), (5.0, 5.0), (0.0, 5.0)]);
        set_entity_area("room", "a", vec![(0.0, 0.0), (9.0, 0.0), (9.0, 9.0), (0.0, 9.0)]);
        assert_eq!(entity_area_ids("room"), vec!["a".to_string(), "b".to_string()]);
        // Clearing one area leaves the others intact.
        set_entity_area("room", "a", vec![]);
        assert_eq!(entity_area_ids("room"), vec!["b".to_string()]);
        // Clearing the last one drops the entity from the map entirely.
        set_entity_area("room", "b", vec![]);
        assert_eq!(entity_area_ids("room"), Vec::<String>::new());
        crate::state::entity_map().lock().unwrap().clear();
    }
}
