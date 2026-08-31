//! Applies declared rooms/portals (raw JSON from the JS capture globals)
//! into `crate::state`. Tolerant of the JS shape: origin/points as
//! `{x,y}` objects, range as `{t0,t1}`.

use crate::js_host_api::Declarations;

fn pt(v: &serde_json::Value) -> Option<(f64, f64)> {
    match v {
        serde_json::Value::Object(o) => {
            let x = o.get("x")?.as_f64()?;
            let y = o.get("y")?.as_f64()?;
            Some((x, y))
        }
        serde_json::Value::Array(a) if a.len() == 2 => {
            Some((a[0].as_f64()?, a[1].as_f64()?))
        }
        _ => None,
    }
}

fn range(v: &serde_json::Value) -> Option<(f64, f64)> {
    match v {
        serde_json::Value::Object(o) => {
            let t0 = o.get("t0")?.as_f64()?;
            let t1 = o.get("t1")?.as_f64()?;
            Some((t0, t1))
        }
        serde_json::Value::Array(a) if a.len() == 2 => {
            Some((a[0].as_f64()?, a[1].as_f64()?))
        }
        _ => None,
    }
}

fn parse_side(v: &serde_json::Value) -> Option<crate::state::PortalSide> {
    Some(crate::state::PortalSide {
        room: v.get("room")?.as_str()?.to_string(),
        edge: v.get("edge")?.as_u64()? as usize,
        range: range(v.get("range")?)?,
    })
}

fn parse_room(v: &serde_json::Value) -> Option<crate::state::Room> {
    Some(crate::state::Room {
        id: v.get("id")?.as_str()?.to_string(),
        terrain: v.get("terrain")?.as_str()?.to_string(),
        origin: pt(v.get("origin")?)?,
        rotation: v.get("rotation").and_then(|r| r.as_f64()).unwrap_or(0.0),
        points: v
            .get("points")?
            .as_array()?
            .iter()
            .map(pt)
            .collect::<Option<Vec<_>>>()?,
    })
}

fn parse_portal(v: &serde_json::Value) -> Option<crate::state::Portal> {
    Some(crate::state::Portal {
        id: v.get("id")?.as_str()?.to_string(),
        from: parse_side(v.get("from")?)?,
        to: parse_side(v.get("to")?)?,
    })
}

pub fn apply_rooms(dec: &Declarations) {
    let mut rooms: Vec<crate::state::Room> = Vec::new();
    for v in dec.rooms.iter() {
        match parse_room(v) {
            Some(room) => rooms.push(room),
            None => runtime_log!(
                "room: skipping malformed room declaration: {:?}", v),
        }
    }
    crate::state::set_rooms(rooms);
}

pub fn apply_portals(dec: &Declarations) {
    let mut portals: Vec<crate::state::Portal> = Vec::new();
    for v in dec.portals.iter() {
        match parse_portal(v) {
            Some(portal) => portals.push(portal),
            None => runtime_log!(
                "portal: skipping malformed portal declaration: {:?}", v),
        }
    }
    crate::state::set_portals(portals);
}
