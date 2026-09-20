//! Entity areas: the polygonal shape each entity occupies in the simulation.
//!
//! An area is declared on the entity (like `numberMap`) and is intrinsic —
//! it moves wherever the entity goes. Vertices are entity-local; the world
//! position is the container's `getX`/`getY` for that member, so areas only
//! compare within one container (no global space, consistent with sectors).
//!
//! The geometry (convex decomposition, AABB, SAT overlap) lives in
//! [`crate::state::area`]; this module wires it to the shared runtime state:
//! the per-entity declared polygon, the container membership, and the
//! `getEntitiesInsideArea()` query itself.

use std::collections::{BTreeSet, HashMap};
use std::sync::Mutex;

use crate::state::area::{self, Poly};

fn entity_areas() -> &'static Mutex<HashMap<String, HashMap<String, Vec<(f64, f64)>>>> {
    crate::state::entity_areas_inner()
}

/// Declare (or clear) one named area on an entity's entityMap. An empty `poly`
/// removes that area: when the entity has no areas left it has no presence in
/// the simulation.
pub fn set_entity_area(entity_id: &str, area_id: &str, poly: Vec<(f64, f64)>) {
    let mut map = entity_areas().lock().unwrap();
    let entry = map.entry(entity_id.to_string()).or_default();
    if poly.len() >= 3 {
        entry.insert(area_id.to_string(), poly);
    } else {
        entry.remove(area_id);
        if entry.is_empty() {
            map.remove(entity_id);
        }
    }
}

/// The declared local polygon for one named area of an entity, if it exists.
pub fn entity_area(entity_id: &str, area_id: &str) -> Option<Vec<(f64, f64)>> {
    entity_areas()
        .lock()
        .unwrap()
        .get(entity_id)
        .and_then(|m| m.get(area_id))
        .cloned()
}

/// Every area id an entity declares (id ascending). An entity with no areas
/// yields an empty vec.
pub fn entity_area_ids(entity_id: &str) -> Vec<String> {
    let mut ids: Vec<String> = entity_areas()
        .lock()
        .unwrap()
        .get(entity_id)
        .map(|m| m.keys().cloned().collect())
        .unwrap_or_default();
    ids.sort();
    ids
}

/// The entityMap of every entity that declares at least one area (entity id ->
/// area id -> local polygon). Used by the precomputed inside-area map.
fn all_areas() -> HashMap<String, HashMap<String, Vec<(f64, f64)>>> {
    entity_areas().lock().unwrap().clone()
}

pub fn clear_entity_areas() {
    entity_areas().lock().unwrap().clear();
}

/// One candidate in the inside-area query: an entity's id, its world position
/// (the container's `getX`/`getY`), and its world area pieces (or `None` for
/// an area-less entity, which is the point at its position).
struct Candidate {
    id: String,
    x: f64,
    y: f64,
    pieces: Option<Vec<Poly>>,
}

/// Parse a serialized container row (mirrors `get_container_by_id`/`active_plans`):
/// id, members, and each member's pre-baked `getX`/`getY` world position.
fn parse_container_row(json_str: &str) -> Option<(String, Vec<(String, f64, f64)>)> {
    let v = serde_json::from_str::<serde_json::Value>(json_str).ok()?;
    let id = v.get("id").and_then(|s| s.as_str())?.to_string();
    let mut members = Vec::new();
    if let Some(entities) = v.get("entities").and_then(|e| e.as_array()) {
        for e in entities {
            if let Some(eid) = e.as_str() {
                let x = v.get("getX").and_then(|g| g.get(eid)).and_then(|n| n.as_f64()).unwrap_or(0.0);
                let y = v.get("getY").and_then(|g| g.get(eid)).and_then(|n| n.as_f64()).unwrap_or(0.0);
                members.push((eid.to_string(), x, y));
            }
        }
    }
    Some((id, members))
}

/// The entities inside `self_id`'s *named* area.
///
/// The area name is required: the named area is resolved first, then the
/// containment/overlap test runs against it. Only members of a container
/// `self_id` belongs to are considered (areas have no global space); an entity
/// in several containers is tested within each and the results are the union,
/// deduped by id. A member with no area is the point at its `(getX, getY)`.
/// The entity itself is excluded. The result is deterministic: entity id
/// ascending. Returns empty (not an error) when the entity has no such area.
pub fn entities_inside_area(
    self_id: &str,
    area_id: &str,
    containers: &[String],
) -> Vec<String> {
    let Some(local_poly) = entity_area(self_id, area_id) else {
        return Vec::new();
    };
    let Some(self_area) = area::make_area(&local_poly) else {
        return Vec::new();
    };
    query_with_area(self_id, &self_area, containers)
}

/// Given `self_id`'s world area shape (already `make_area`d), gather every
/// other member of every container `self_id` belongs to and test each against
/// it. Shared by the named and the union query.
fn query_with_area(
    self_id: &str,
    self_area: &area::Area,
    containers: &[String],
) -> Vec<String> {
    // self's position in each container it belongs to, plus every other
    // member of those containers as a candidate.
    let mut self_positions: HashMap<String, (f64, f64)> = HashMap::new();
    let mut candidates: Vec<(String, Candidate)> = Vec::new(); // (container_id, candidate)

    for row in containers.iter() {
        let Some((cid, members)) = parse_container_row(row) else { continue };
        let Some((_, sx, sy)) = members.iter().find(|(eid, _, _)| eid == self_id) else {
            continue; // self is not a member of this container
        };
        self_positions.insert(cid.clone(), (*sx, *sy));
        for (eid, x, y) in members.iter() {
            if eid == self_id {
                continue;
            }
            // A member's own presence: its area, if it declares one (any name),
            // else the point at its position.
            let pieces = member_world_pieces(eid, *x, *y);
            candidates.push((
                cid.clone(),
                Candidate { id: eid.to_string(), x: *x, y: *y, pieces },
            ));
        }
    }
    if self_positions.is_empty() {
        return Vec::new();
    }

    let mut found: BTreeSet<String> = BTreeSet::new();
    for (cid, cand) in candidates.iter() {
        let (sx, sy) = self_positions[cid];
        let world_self = self_area.translated(sx, sy);
        let hit = match &cand.pieces {
            Some(other) => area::areas_overlap(&world_self, other),
            None => area::area_contains(&world_self, cand.x, cand.y),
        };
        if hit {
            found.insert(cand.id.clone());
        }
    }
    found.into_iter().collect()
}

/// A member's own world-area pieces, or `None` (a point) when it declares no
/// area. A member may declare several areas; any of them that overlaps counts,
/// so return `Some` when it has at least one area.
fn member_world_pieces(eid: &str, x: f64, y: f64) -> Option<Vec<area::Poly>> {
    let guard = entity_areas().lock().unwrap();
    let Some(areas) = guard.get(eid) else {
        return None;
    };
    if areas.is_empty() {
        return None;
    }
    // Union the member's areas into a single piece set so a hit in any of them
    // is detected by one `areas_overlap` call.
    let mut all: Vec<area::Poly> = Vec::new();
    for poly in areas.values() {
        if let Some(a) = area::make_area(poly) {
            all.extend(a.translated(x, y));
        }
    }
    if all.is_empty() { None } else { Some(all) }
}

/// The container ids an entity is a member of (for diagnostics).
pub fn entity_containers(entity_id: &str, containers: &[String]) -> Vec<String> {
    let mut out = Vec::new();
    for row in containers.iter() {
        if let Some((cid, members)) = parse_container_row(row) {
            if members.iter().any(|(eid, _, _)| eid == entity_id) {
                out.push(cid);
            }
        }
    }
    out
}

/// Precompute `getEntitiesInsideArea(name)` for every named area, as a JSON
/// object mapping entity id -> area id -> the ids inside that area. Written to
/// `globalThis.__insideArea` so the JS entity wrappers can read it without a
/// per-call FFI round trip. The name is required: the wrapper looks up
/// `__insideArea[entityId][areaId]`.
pub fn inside_area_map_json(containers: &[String]) -> String {
    let entities = all_areas();
    let mut map = serde_json::Map::new();
    for (eid, areas) in entities.iter() {
        let mut area_map = serde_json::Map::new();
        for area_id in areas.keys() {
            let ids = entities_inside_area(eid, area_id, containers);
            area_map.insert(area_id.clone(), serde_json::Value::Array(
                ids.into_iter().map(serde_json::Value::String).collect()
            ));
        }
        map.insert(eid.clone(), serde_json::Value::Object(area_map));
    }
    serde_json::to_string(&serde_json::Value::Object(map)).unwrap_or_else(|_| "{}".into())
}

/// The world pieces of one named area of an entity at (x, y), if it exists.
pub fn world_pieces(entity_id: &str, area_id: &str, x: f64, y: f64) -> Option<Vec<Poly>> {
    let local = entity_area(entity_id, area_id)?;
    let a = area::make_area(&local)?;
    Some(a.translated(x, y))
}

#[cfg(test)]
mod tests {
    use super::*;
    use crate::state::test_lock;

    fn row(id: &str, members: &[(&str, f64, f64)]) -> String {
        let mut c = serde_json::Map::new();
        c.insert("id".into(), serde_json::json!(id));
        c.insert("entities".into(), serde_json::json!(
            members.iter().map(|(e, _, _)| *e).collect::<Vec<_>>()
        ));
        let mut gx = serde_json::Map::new();
        let mut gy = serde_json::Map::new();
        for (e, x, y) in members {
            gx.insert((**e).to_string(), serde_json::json!(*x));
            gy.insert((**e).to_string(), serde_json::json!(*y));
        }
        c.insert("getX".into(), serde_json::Value::Object(gx));
        c.insert("getY".into(), serde_json::Value::Object(gy));
        serde_json::to_string(&serde_json::Value::Object(c)).unwrap()
    }

    #[test]
    fn point_members_inside_a_rect_are_found() {
        let _g = test_lock();
        set_entity_area("room", "", vec![
            (0.0, 0.0), (100.0, 0.0), (100.0, 100.0), (0.0, 100.0),
        ]);
        let containers = vec![row("c1", &[
            ("room", 10.0, 10.0),
            ("npc-a", 30.0, 40.0),
            ("npc-b", 200.0, 200.0),
        ])];
        let got = entities_inside_area("room", "", &containers);
        assert_eq!(got, vec!["npc-a".to_string()]);
        clear_entity_areas();
    }

    #[test]
    fn polygon_member_overlaps_the_area() {
        let _g = test_lock();
        set_entity_area("room", "", vec![
            (0.0, 0.0), (100.0, 0.0), (100.0, 100.0), (0.0, 100.0),
        ]);
        set_entity_area("crate", "", vec![
            (0.0, 0.0), (20.0, 0.0), (20.0, 20.0), (0.0, 20.0),
        ]);
        let containers = vec![row("c1", &[
            ("room", 10.0, 10.0),
            ("crate", 40.0, 40.0), // 40..60 x 40..60: fully inside
            ("far", 150.0, 150.0),
        ])];
        let got = entities_inside_area("room", "", &containers);
        assert_eq!(got, vec!["crate".to_string()]);
        clear_entity_areas();
    }

    #[test]
    fn self_is_excluded_and_order_is_id_ascending() {
        let _g = test_lock();
        set_entity_area("room", "", vec![
            (0.0, 0.0), (100.0, 0.0), (100.0, 100.0), (0.0, 100.0),
        ]);
        let containers = vec![row("c1", &[
            ("room", 10.0, 10.0),
            ("zeta", 20.0, 20.0),
            ("alpha", 30.0, 30.0),
        ])];
        let got = entities_inside_area("room", "", &containers);
        assert_eq!(got, vec!["alpha".to_string(), "zeta".to_string()]);
        clear_entity_areas();
    }

    #[test]
    fn no_area_means_no_presence_and_empty_result() {
        let _g = test_lock();
        let containers = vec![row("c1", &[
            ("room", 10.0, 10.0),
            ("npc-a", 30.0, 40.0),
        ])];
        assert_eq!(entities_inside_area("room", "", &containers), Vec::<String>::new());
        // An area-less *member* is a point; a point outside the area is not found.
        set_entity_area("room", "", vec![
            (0.0, 0.0), (10.0, 0.0), (10.0, 10.0), (0.0, 10.0),
        ]);
        let got = entities_inside_area("room", "", &containers);
        assert_eq!(got, Vec::<String>::new());
        clear_entity_areas();
    }

    #[test]
    fn point_in_aabb_but_outside_the_polygon_is_not_reported_inside() {
        let _g = test_lock();
        // "room" is a concave L (the cliff shape): a 200x128 box with the
        // top-right pocket (x 96..200, y 64..128) cut out.
        set_entity_area("room", "", vec![
            (0.0, 0.0), (200.0, 0.0), (200.0, 64.0), (96.0, 64.0), (96.0, 128.0), (0.0, 128.0),
        ]);
        let containers = vec![row("c1", &[
            ("room", 10.0, 10.0),
            // npc-a at world (150, 96): inside the room's AABB (10..210 x 10..138)
            // but in the pocket, which is OUTSIDE the polygon -> not inside.
            ("npc-a", 150.0, 96.0),
            // npc-b at world (40, 96): inside both the AABB and the polygon body
            // -> inside.
            ("npc-b", 40.0, 96.0),
        ])];
        let got = entities_inside_area("room", "", &containers);
        assert_eq!(got, vec!["npc-b".to_string()]);
        clear_entity_areas();
    }

    #[test]
    fn union_across_multiple_containers_is_deduped() {
        let _g = test_lock();
        set_entity_area("hall", "", vec![
            (0.0, 0.0), (50.0, 0.0), (50.0, 50.0), (0.0, 50.0),
        ]);
        // hall is a member of two containers; npc-x sits in both, once each.
        let containers = vec![
            row("c1", &[("hall", 5.0, 5.0), ("npc-x", 10.0, 10.0)]),
            row("c2", &[("hall", 5.0, 5.0), ("npc-x", 10.0, 10.0), ("npc-y", 99.0, 99.0)]),
        ];
        let got = entities_inside_area("hall", "", &containers);
        // npc-x appears in both containers (deduped); npc-y is far away.
        assert_eq!(got, vec!["npc-x".to_string()]);
        clear_entity_areas();
    }

    #[test]
    fn entity_not_in_any_container_has_empty_result() {
        let _g = test_lock();
        set_entity_area("room", "", vec![
            (0.0, 0.0), (100.0, 0.0), (100.0, 100.0), (0.0, 100.0),
        ]);
        let containers = vec![row("c1", &[("other", 30.0, 40.0)])];
        assert_eq!(entities_inside_area("room", "", &containers), Vec::<String>::new());
        clear_entity_areas();
    }

    #[test]
    fn named_area_is_resolved_before_the_query() {
        let _g = test_lock();
        // "room" declares two named areas: a small "home" box and a big "zone"
        // box. The named query resolves the requested area first.
        set_entity_area("room", "home", vec![
            (0.0, 0.0), (20.0, 0.0), (20.0, 20.0), (0.0, 20.0),
        ]);
        set_entity_area("room", "zone", vec![
            (0.0, 0.0), (100.0, 0.0), (100.0, 100.0), (0.0, 100.0),
        ]);
        let containers = vec![row("c1", &[
            ("room", 10.0, 10.0),
            ("npc-near", 15.0, 15.0),  // inside both home and zone
            ("npc-far", 80.0, 80.0),   // inside zone only
        ])];
        // The "home" area admits only the near npc ...
        assert_eq!(
            entities_inside_area("room", "home", &containers),
            vec!["npc-near".to_string()]
        );
        // ... while the "zone" area admits both.
        assert_eq!(
            entities_inside_area("room", "zone", &containers),
            vec!["npc-far".to_string(), "npc-near".to_string()]
        );
        // An unknown area name yields empty (resolved first, not found).
        assert_eq!(
            entities_inside_area("room", "nope", &containers),
            Vec::<String>::new()
        );
        clear_entity_areas();
    }

    #[test]
    fn each_named_area_is_independent() {
        let _g = test_lock();
        set_entity_area("room", "home", vec![
            (0.0, 0.0), (20.0, 0.0), (20.0, 20.0), (0.0, 20.0),
        ]);
        set_entity_area("room", "zone", vec![
            (0.0, 0.0), (100.0, 0.0), (100.0, 100.0), (0.0, 100.0),
        ]);
        let containers = vec![row("c1", &[
            ("room", 10.0, 10.0),
            ("npc-near", 15.0, 15.0),  // inside both home and zone
            ("npc-far", 80.0, 80.0),   // inside zone only
        ])];
        // The name is required: querying "home" returns only the near npc.
        assert_eq!(
            entities_inside_area("room", "home", &containers),
            vec!["npc-near".to_string()]
        );
        // Querying "zone" returns both (the far one is in zone but not home).
        assert_eq!(
            entities_inside_area("room", "zone", &containers),
            vec!["npc-far".to_string(), "npc-near".to_string()]
        );
        // There is no implicit union: querying the default (unnamed) area
        // yields empty when the entity has no unnamed area.
        assert_eq!(
            entities_inside_area("room", "", &containers),
            Vec::<String>::new()
        );
        clear_entity_areas();
    }

    #[test]
    fn area_ids_are_listed_and_cleared_per_area() {
        let _g = test_lock();
        set_entity_area("room", "b", vec![(0.0, 0.0), (5.0, 0.0), (5.0, 5.0), (0.0, 5.0)]);
        set_entity_area("room", "a", vec![(0.0, 0.0), (9.0, 0.0), (9.0, 9.0), (0.0, 9.0)]);
        assert_eq!(entity_area_ids("room"), vec!["a".to_string(), "b".to_string()]);
        // Clearing one area leaves the others intact.
        set_entity_area("room", "a", vec![]);
        assert_eq!(entity_area_ids("room"), vec!["b".to_string()]);
        // Clearing the last one drops the entity from the map entirely.
        set_entity_area("room", "b", vec![]);
        assert_eq!(entity_area_ids("room"), Vec::<String>::new());
        clear_entity_areas();
    }
}
