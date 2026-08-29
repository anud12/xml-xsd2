//! Room/Portal spatial data model (Layer A) for the RTS world view.
//!
//! Points are LOCAL to a room; `origin` is the world position of the room
//! center and `rotation` is radians about the origin. A local point maps to
//! world as `origin + R(rotation) * point`.

#[derive(Debug, Clone, PartialEq, serde::Serialize, serde::Deserialize)]
pub struct Room {
    pub id: String,
    pub terrain: String,
    pub origin: (f64, f64),
    pub rotation: f64,
    pub points: Vec<(f64, f64)>,
}

#[derive(Debug, Clone, PartialEq, serde::Serialize, serde::Deserialize)]
pub struct PortalSide {
    pub room: String,
    pub edge: usize,
    pub range: (f64, f64),
}

#[derive(Debug, Clone, PartialEq, serde::Serialize, serde::Deserialize)]
pub struct Portal {
    pub id: String,
    pub from: PortalSide,
    pub to: PortalSide,
}

impl Room {
    /// Maps a local point to world space: `origin + R(rotation) * point`.
    pub fn to_world(&self, point: (f64, f64)) -> (f64, f64) {
        let (cos, sin) = (self.rotation.cos(), self.rotation.sin());
        (
            self.origin.0 + cos * point.0 - sin * point.1,
            self.origin.1 + sin * point.0 + cos * point.1,
        )
    }
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

    #[test]
    fn rooms_and_portals_round_trip_through_state_and_json() {
        let _g = lock();
        state::clear_state();
        let room = Room {
            id: "cave-1".into(),
            terrain: "stone".into(),
            origin: (10.0, -4.5),
            rotation: std::f64::consts::FRAC_PI_4,
            points: vec![(1.0, 2.0), (3.0, 4.0), (5.0, 6.0)],
        };
        let portal = Portal {
            id: "p-1".into(),
            from: PortalSide {
                room: "cave-1".into(),
                edge: 2,
                range: (0.2, 0.8),
            },
            to: PortalSide {
                room: "hall-2".into(),
                edge: 0,
                range: (0.0, 1.0),
            },
        };
        state::set_rooms(vec![room.clone()]);
        state::set_portals(vec![portal.clone()]);

        let by_id = state::fetch_room_by_id("cave-1").expect("room by id");
        assert_eq!(by_id, room);
        assert!(state::fetch_room_by_id("nope").is_none());

        let json = state::fetch_rooms_json();
        let v: serde_json::Value = serde_json::from_str(&json).unwrap();
        let parsed_room: Room =
            serde_json::from_value(v["rooms"][0].clone()).unwrap();
        let parsed_portal: Portal =
            serde_json::from_value(v["portals"][0].clone()).unwrap();
        assert_eq!(parsed_room, room);
        assert_eq!(parsed_portal, portal);
        assert_eq!(parsed_room.terrain, "stone");
        assert_eq!(parsed_portal.from.edge, 2);
        assert!((parsed_room.origin.0 - 10.0).abs() < 1e-12);

        // rotation maps local points as origin + R(rotation) * point
        let (wx, wy) = room.to_world((1.0, 0.0));
        assert!((wx - (10.0 + room.rotation.cos())).abs() < 1e-12);
        assert!((wy - (room.origin.1 + room.rotation.sin())).abs() < 1e-12);

        state::clear_state();
        assert!(state::rooms().lock().unwrap().is_empty());
        assert!(state::portals().lock().unwrap().is_empty());
    }
}
