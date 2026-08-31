//! Portal crossing + room adjacency for the .ui world view (Phase 2c).
//!
//! A unit (entity) whose local position exits a room through a portal's
//! edge range is moved to the linked room at the mapped position.

use std::collections::HashMap;

use crate::state::{self, Portal, PortalSide, Room};

/// Max perpendicular distance (local units) from the edge line at which an
/// entity counts as "on" the edge for crossing detection.
const EDGE_TOLERANCE: f64 = 4.0;
/// Distance (local units) a freshly-placed entity is nudged INTO the target
/// room's interior along the edge's interior normal, to prevent it from
/// immediately re-crossing back through the same portal.
const NUDGE_INTO_INTERIOR: f64 = 1.0;

/// A portal direction usable for crossing: the entity is currently in
/// `cur_room`, walking across `edge` (local index) whose portal range is
/// `(t0, t1)`; the destination is the other side of the portal.
struct Crossing {
    portal_id: String,
    cur_room: String,
    edge: usize,
    t0: f64,
    t1: f64,
    target: PortalSide, // the target-side half of the portal
}

/// All portal directions: both from->to and to->from for every portal.
fn crossing_directions(portals: &[Portal]) -> Vec<Crossing> {
    let mut out = Vec::new();
    for p in portals {
        out.push(Crossing {
            portal_id: p.id.clone(),
            cur_room: p.from.room.clone(),
            edge: p.from.edge,
            t0: p.from.range.0,
            t1: p.from.range.1,
            target: p.to.clone(),
        });
        out.push(Crossing {
            portal_id: p.id.clone(),
            cur_room: p.to.room.clone(),
            edge: p.to.edge,
            t0: p.to.range.0,
            t1: p.to.range.1,
            target: p.from.clone(),
        });
    }
    out
}

/// For each room, the sign of `cross(b - a, p - a)` for a point known to be
/// inside the room (its centroid). For consistently-wound polygons this is
/// the "interior" sign: a point whose cross sign DIFFERS from it sits on
/// the exterior side of that edge (exited through it).
fn interior_signs(rooms: &[Room]) -> HashMap<String, i8> {
    let mut out = HashMap::new();
    for room in rooms {
        let n = room.points.len();
        if n < 3 {
            continue;
        }
        let cx: f64 = room.points.iter().map(|p| p.0).sum::<f64>() / n as f64;
        let cy: f64 = room.points.iter().map(|p| p.1).sum::<f64>() / n as f64;
        let (ax, ay) = room.points[0];
        let (bx, by) = room.points[1];
        let cross = (by - ay) * (cx - ax) - (bx - ax) * (cy - ay);
        let sign = if cross >= 0.0 { 1 } else { -1 };
        out.insert(room.id.clone(), sign);
    }
    out
}

/// Moves entities that have exited a room through a portal's edge range to
/// the linked room at the mapped position. Returns `"entityId:portalId"`
/// entries for logging. Each entity moves at most once per tick (the first
/// matching portal wins) to avoid oscillation.
///
/// Returns `Ok(moves)` on success or `Err(msg)` if any global state is
/// poisoned, so the caller can log and continue.
pub fn process_portal_crossings() -> Result<Vec<String>, String> {
    let rooms = state::rooms()
        .lock()
        .map_err(|e| format!("rooms poisoned: {}", e))?
        .clone();
    let portals = state::portals()
        .lock()
        .map_err(|e| format!("portals poisoned: {}", e))?
        .clone();
    let directions = crossing_directions(&portals);
    let interior = interior_signs(&rooms);

    // Entities currently sitting on a target edge after a teleport are not
    // moved again this tick.
    let mut moved: Vec<String> = Vec::new();
    let mut moved_entities: Vec<String> = Vec::new();

    let entity_ids: Vec<String> = state::last_entity_number_data()
        .lock()
        .map_err(|e| format!("entity number data poisoned: {}", e))?
        .keys()
        .cloned()
        .collect();

    for id in entity_ids {
        if moved_entities.contains(&id) {
            continue;
        }
        let (room_id, px, py) = {
            let text = state::last_entity_data()
                .lock()
                .map_err(|e| format!("entity data poisoned: {}", e))?;
            let numbers = state::last_entity_number_data()
                .lock()
                .map_err(|e| format!("entity number data poisoned: {}", e))?;
            let text_map = match text.get(&id) {
                Some(m) => m,
                None => continue,
            };
            let room = match text_map.get("room") {
                Some(r) => r.clone(),
                None => continue,
            };
            let num_map = match numbers.get(&id) {
                Some(m) => m,
                None => continue,
            };
            let (x, y) = match (num_map.get("x"), num_map.get("y")) {
                (Some(x), Some(y)) => (*x, *y),
                _ => continue,
            };
            (room, x, y)
        };

        let room = match rooms.iter().find(|r| r.id == room_id) {
            Some(r) => r,
            None => continue,
        };
        let n = room.points.len();
        if n < 3 {
            continue;
        }
        let interior_sign = match interior.get(&room.id).copied() {
            Some(s) => s,
            None => continue,
        };
        for dir in directions.iter() {
            if dir.cur_room != room.id {
                continue;
            }
            let e = dir.edge % n;
            let (ax, ay) = room.points[e];
            let (bx, by) = room.points[(e + 1) % n];
            let dx = bx - ax;
            let dy = by - ay;
            let len2 = dx * dx + dy * dy;
            if len2 <= 0.0 {
                continue;
            }
            let t = ((px - ax) * dx + (py - ay) * dy) / len2;
            if !(dir.t0..=dir.t1).contains(&t) {
                continue;
            }
            let cross = dy * (px - ax) - dx * (py - ay);
            // A point on the interior side of a consistently-wound edge has
            // the SAME cross sign as the room's interior sign (the centroid's
            // sign). Opposite sign = point is on the exterior side.
            let side = match cross.partial_cmp(&0.0) {
                Some(std::cmp::Ordering::Greater) => 1i8,
                Some(std::cmp::Ordering::Less) => -1,
                _ => continue, // point is exactly on the edge line
            };
            if side == interior_sign {
                // still on (or inside) the interior side of this edge
                continue;
            }
            let perp = cross / len2.sqrt();
            if perp.abs() > EDGE_TOLERANCE {
                continue;
            }
            // Crossing: map t across the portal range onto the target edge.
            let t_span = dir.t1 - dir.t0;
            let u = if t_span.abs() > 1e-9 {
                (t - dir.t0) / t_span
            } else {
                0.5
            };
            let target_room = match rooms.iter().find(|r| r.id == dir.target.room) {
                Some(r) => r,
                None => continue,
            };
            let tn = target_room.points.len();
            if tn < 3 || dir.target.edge >= tn {
                continue;
            }
            let e2 = dir.target.edge % tn;
            let (ax2, ay2) = target_room.points[e2];
            let (bx2, by2) = target_room.points[(e2 + 1) % tn];
            let u_span = dir.target.range.1 - dir.target.range.0;
            let tprime = (dir.target.range.0 + u * u_span).clamp(
                dir.target.range.0,
                dir.target.range.0 + u_span,
            );
            // Nudge 1.0 unit into the target room's interior along the
            // edge's interior normal so the entity does not sit on the edge
            // and immediately re-cross back through the same portal.
            let ddx2 = bx2 - ax2;
            let ddy2 = by2 - ay2;
            let len2_2 = ddx2 * ddx2 + ddy2 * ddy2;
            let (nx, ny) = if len2_2 > 0.0 {
                let mut nx = ax2 + tprime * (ddx2);
                let mut ny = ay2 + tprime * (ddy2);
                // Interior normal for a consistently-wound room: the
                // inwards-perpendicular (-dy, dx) (or its negation) chosen
                // so it points to the centroid.
                let cx2: f64 =
                    target_room.points.iter().map(|p| p.0).sum::<f64>()
                        / target_room.points.len() as f64;
                let cy2: f64 =
                    target_room.points.iter().map(|p| p.1).sum::<f64>()
                        / target_room.points.len() as f64;
                let cand_x = ax2 - ddy2 / len2_2.sqrt();
                let cand_y = ay2 + ddx2 / len2_2.sqrt();
                let to_center_x = cx2 - ax2;
                let to_center_y = cy2 - ay2;
                let dot = (cand_x - ax2) * to_center_x
                    + (cand_y - ay2) * to_center_y;
                let s = if dot >= 0.0 { 1.0 } else { -1.0 };
                nx += s * (-ddy2 / len2_2.sqrt()) * NUDGE_INTO_INTERIOR;
                ny += s * (ddx2 / len2_2.sqrt()) * NUDGE_INTO_INTERIOR;
                (nx, ny)
            } else {
                (ax2 + tprime * ddx2, ay2 + tprime * ddy2)
            };

            state::last_entity_number_data()
                .lock()
                .map_err(|e| format!("entity number data poisoned: {}", e))?
                .entry(id.clone())
                .or_insert_with(HashMap::new)
                .insert("x".into(), nx);
            state::last_entity_number_data()
                .lock()
                .map_err(|e| format!("entity number data poisoned: {}", e))?
                .entry(id.clone())
                .or_insert_with(HashMap::new)
                .insert("y".into(), ny);
            state::last_entity_data()
                .lock()
                .map_err(|e| format!("entity data poisoned: {}", e))?
                .entry(id.clone())
                .or_insert_with(HashMap::new)
                .insert("room".into(), target_room.id.clone());

            moved_entities.push(id.clone());
            moved.push(format!("{}:{}", id, dir.portal_id));
            break;
        }
    }
    Ok(moved)
}

/// Rooms linked by at least one portal (either direction) are adjacent.
pub fn room_adjacency() -> HashMap<String, Vec<String>> {
    let portals = state::portals().lock().unwrap().clone();
    let mut out: HashMap<String, Vec<String>> = HashMap::new();
    for p in &portals {
        if p.from.room != p.to.room {
            let a = out.entry(p.from.room.clone()).or_default();
            if !a.contains(&p.to.room) {
                a.push(p.to.room.clone());
            }
            let b = out.entry(p.to.room.clone()).or_default();
            if !b.contains(&p.from.room) {
                b.push(p.from.room.clone());
            }
        }
    }
    out
}

#[cfg(test)]
mod tests {
    use super::*;
    use crate::state;

    // All process-global state is shared; serialize the tests that touch it.
    static TEST_LOCK: std::sync::Mutex<()> = std::sync::Mutex::new(());
    fn lock() -> std::sync::MutexGuard<'static, ()> {
        TEST_LOCK.lock().unwrap_or_else(|e| e.into_inner())
    }

    fn square_room(id: &str) -> Room {
        Room {
            id: id.into(),
            terrain: "test".into(),
            origin: (0.0, 0.0),
            rotation: 0.0,
            // 100x100 square centered at origin, CCW winding.
            // Edge 0 = bottom (-50,-50)->(50,-50); edge 1 = right
            // (50,-50)->(50,50); edge 2 = top; edge 3 = left
            // (-50,50)->(-50,-50).
            points: vec![
                (-50.0, -50.0),
                (50.0, -50.0),
                (50.0, 50.0),
                (-50.0, 50.0),
            ],
        }
    }

    fn seed() {
        state::clear_state();
        state::set_rooms(vec![square_room("room1"), square_room("room2")]);
        state::set_portals(vec![Portal {
            id: "p1".into(),
            from: crate::state::PortalSide {
                room: "room1".into(),
                edge: 1,
                range: (0.25, 0.75),
            },
            to: crate::state::PortalSide {
                room: "room2".into(),
                edge: 3,
                range: (0.25, 0.75),
            },
        }]);
    }

    fn put_entity(id: &str, room: &str, x: f64, y: f64) {
        let mut nums: HashMap<String, f64> = HashMap::new();
        nums.insert("x".into(), x);
        nums.insert("y".into(), y);
        let mut texts: HashMap<String, String> = HashMap::new();
        texts.insert("room".into(), room.into());
        state::last_entity_number_data()
            .lock()
            .unwrap()
            .insert(id.into(), nums);
        state::last_entity_data()
            .lock()
            .unwrap()
            .insert(id.into(), texts);
    }

    fn entity_pos(id: &str) -> (String, f64, f64) {
        let text = state::last_entity_data().lock().unwrap();
        let nums = state::last_entity_number_data().lock().unwrap();
        let room = text.get(id).unwrap().get("room").unwrap().clone();
        let (x, y) = (
            nums.get(id).unwrap().get("x").unwrap(),
            nums.get(id).unwrap().get("y").unwrap(),
        );
        (room, *x, *y)
    }

    #[test]
    fn entity_crossing_portal_is_moved_to_target_room() {
        let _g = lock();
        seed();
        // A starts just inside room1's right edge (49,0); t along edge 1 is
        // 0.5, inside the portal range 0.25..0.75. It must not move while
        // interior.
        put_entity("A", "room1", 49.0, 0.0);
        let moved = process_portal_crossings().unwrap();
        assert!(moved.is_empty(), "interior entity must not move: {:?}", moved);

        // Pushed to (51,0): just outside the right edge, still in the
        // portal's t range -> it crosses into room2.
        put_entity("A", "room1", 51.0, 0.0);
        let moved = process_portal_crossings().unwrap();
        assert_eq!(moved, vec!["A:p1".to_string()]);

        let (room, x, y) = entity_pos("A");
        assert_eq!(room, "room2");
        // Mapped to room2 edge 3 (left edge: (-50,50) -> (-50,-50)), t' = 0.5
        // -> (-50, 0), nudged 1.0 into room2 interior (rightward, +x).
        assert!((x - (-49.0)).abs() < 0.01, "x={}", x);
        assert!((y - 0.0).abs() < 0.01, "y={}", y);

        // Anti-oscillation: running again must NOT move it back.
        let moved = process_portal_crossings().unwrap();
        assert!(moved.is_empty(), "re-cross must be prevented: {:?}", moved);
        let (room, x, y) = entity_pos("A");
        assert_eq!(room, "room2");
        assert!((x - (-49.0)).abs() < 0.01);
        assert!((y - 0.0).abs() < 0.01);
    }

    #[test]
    fn entity_outside_portal_range_is_not_moved() {
        let _g = lock();
        seed();
        // Right edge of room1: t at y=49 is (49-(-50))/100 = 0.99 > 0.75.
        put_entity("B", "room1", 51.0, 49.0);
        let moved = process_portal_crossings().unwrap();
        assert!(moved.is_empty(), "{:?}", moved);
        let (room, x, y) = entity_pos("B");
        assert_eq!(room, "room1");
        assert!((x - 51.0).abs() < 1e-9);
        assert!((y - 49.0).abs() < 1e-9);
    }

    #[test]
    fn entity_far_inside_is_not_moved() {
        let _g = lock();
        seed();
        put_entity("C", "room1", 0.0, 0.0);
        let moved = process_portal_crossings().unwrap();
        assert!(moved.is_empty(), "{:?}", moved);
        let (room, x, y) = entity_pos("C");
        assert_eq!(room, "room1");
        assert!((x - 0.0).abs() < 1e-9);
        assert!((y - 0.0).abs() < 1e-9);
    }

    #[test]
    fn entity_can_cross_back_through_same_portal() {
        let _g = lock();
        seed();
        // Start A just outside room2's left edge (edge 3, t in range):
        // x = -51, y = 0 -> t = (0-50)/(-100)... edge 3 is (-50,50)->(-50,-50),
        // t = dot(p-a, b-a)/|b-a|^2 = ((-51+50)*0 + (0-50)*(-100))/10000 = 0.5.
        put_entity("A", "room2", -51.0, 0.0);
        let moved = process_portal_crossings().unwrap();
        assert_eq!(moved, vec!["A:p1".to_string()]);
        let (room, x, y) = entity_pos("A");
        assert_eq!(room, "room1");
        // room1 edge 1: (50,-50)->(50,50), t' = 0.5 -> (50, 0), nudged into
        // room1 interior (leftward, -x).
        assert!((x - 49.0).abs() < 0.01, "x={}", x);
        assert!((y - 0.0).abs() < 0.01, "y={}", y);
    }

    #[test]
    fn adjacency_lists_linked_rooms_both_ways() {
        let _g = lock();
        seed();
        let adj = room_adjacency();
        assert!(adj.get("room1").unwrap().contains(&"room2".to_string()));
        assert!(adj.get("room2").unwrap().contains(&"room1".to_string()));
    }
}
