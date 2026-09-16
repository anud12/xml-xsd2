//! Sector/portal spatial model for the Build-style (Duke Nukem 3D) world view,
//! adapted to top-down 2D.
//!
//! A sector is a container with its own local interior (sizeX x sizeY cells).
//! It occupies a set of footprint squares on a shared grid (a polyomino placed
//! at `at`). Sectors have no global position — they are positioned only
//! relative to whichever sector you look from. The grid records which squares
//! are taken so a newly placed sector discovers every neighbour it lands
//! against and links to all of them. Two sectors join with a portal only where
//! they have facing openings of at least one shared cell; the portal span is
//! centered in each opening with a lower-index bias.

use serde::{Deserialize, Serialize};

/// A grid square.
pub type Square = (i32, i32);

// serde helpers: serialize a Square as a two-element [x, y] array so it
// round-trips through the FFI JSON boundary.
fn sq_ser<S: serde::Serializer>(v: &Square, s: S) -> Result<S::Ok, S::Error> {
    serde::Serialize::serialize(&[v.0, v.1], s)
}
fn sq_deser<'de, D: serde::Deserializer<'de>>(d: D) -> Result<Square, D::Error> {
    let arr: [i32; 2] = serde::Deserialize::deserialize(d)?;
    Ok((arr[0], arr[1]))
}

/// The four footprint sides, in clockwise order starting north.
#[derive(Debug, Clone, Copy, PartialEq, Eq, PartialOrd, Ord, Serialize, Deserialize)]
pub enum Side {
    N,
    E,
    S,
    W,
}

impl Side {
    /// The square adjacent to `cell` on this side.
    pub fn neighbour(&self, cell: Square) -> Square {
        match self {
            Side::N => (cell.0, cell.1 - 1),
            Side::E => (cell.0 + 1, cell.1),
            Side::S => (cell.0, cell.1 + 1),
            Side::W => (cell.0 - 1, cell.1),
        }
    }

    /// The side facing back at us (180 degrees around).
    pub fn opposite(&self) -> Side {
        match self {
            Side::N => Side::S,
            Side::E => Side::W,
            Side::S => Side::N,
            Side::W => Side::E,
        }
    }

    pub fn as_str(&self) -> &'static str {
        match self {
            Side::N => "N",
            Side::E => "E",
            Side::S => "S",
            Side::W => "W",
        }
    }

    pub fn parse(s: &str) -> Option<Side> {
        match s {
            "N" => Some(Side::N),
            "E" => Some(Side::E),
            "S" => Some(Side::S),
            "W" => Some(Side::W),
            _ => None,
        }
    }

    /// All four sides, N then clockwise.
    pub fn all() -> [Side; 4] {
        [Side::N, Side::E, Side::S, Side::W]
    }
}

/// An opening a sector declares on one of its boundary edges. `start` and
/// `length` index interior cells along the edge (y for N/S, x for E/W).
#[derive(Debug, Clone, Copy, PartialEq, Eq, Serialize, Deserialize)]
pub struct Opening {
    #[serde(serialize_with = "sq_ser", deserialize_with = "sq_deser")]
    pub cell: Square,
    pub side: Side,
    pub start: i32,
    pub length: i32,
}

/// A boundary edge is a {footprint square, side} whose adjacent square is NOT
/// part of the same sector's footprint. Interior edges (the adjacent square
/// belongs to the footprint) are not addressable and never carry openings.
#[derive(Debug, Clone, Copy, PartialEq, Eq, Serialize, Deserialize)]
pub struct BoundaryEdge {
    #[serde(serialize_with = "sq_ser", deserialize_with = "sq_deser")]
    pub square: Square,
    pub side: Side,
}

/// The sector side of a portal: which footprint square + side the portal sits
/// on, and the matched span (start/length in interior cells along that side).
#[derive(Debug, Clone, Copy, PartialEq, Eq, Serialize, Deserialize)]
pub struct PortalSide {
    #[serde(serialize_with = "sq_ser", deserialize_with = "sq_deser")]
    pub cell: Square,
    pub side: Side,
    pub span: i32,
    pub length: i32,
}

/// A portal joining two sectors along facing boundary edges.
#[derive(Debug, Clone, PartialEq, Eq, Serialize, Deserialize)]
pub struct Portal {
    pub id: String,
    pub a: PortalSide,
    pub b: PortalSide,
}

/// A grid square that belongs to some sector.
#[derive(Debug, Clone, PartialEq, Eq, Serialize, Deserialize)]
pub struct SectorCell {
    pub x: i32,
    pub y: i32,
    pub container: String,
    /// The boundary edges of the footprint that this square owns.
    #[serde(default, skip_serializing_if = "Vec::is_empty")]
    pub boundary_edges: Vec<BoundaryEdge>,
    /// Openings declared on this square's boundary edges.
    #[serde(default, skip_serializing_if = "Vec::is_empty")]
    pub openings: Vec<Opening>,
}

/// The full computed state of one sector grid.
#[derive(Debug, Clone, PartialEq, Eq, Serialize, Deserialize)]
pub struct SectorGridState {
    pub id: String,
    #[serde(default, skip_serializing_if = "Vec::is_empty")]
    pub cells: Vec<SectorCell>,
    #[serde(default, skip_serializing_if = "Vec::is_empty")]
    pub portals: Vec<Portal>,
}

/// A sector declaration captured from the JS layer. Footprint offsets are
/// normalized to min x/y = 0; `at` places the whole shape on the grid.
#[derive(Debug, Clone)]
pub struct SectorDeclaration {
    pub grid: String,
    pub container: String,
    pub at: Square,
    pub footprint: Vec<Square>,
    pub openings: Vec<Opening>,
}

/// Normalizes a footprint so its min x/y are 0, then translates by `at`.
pub fn normalize_footprint(footprint: &[(i32, i32)], at: Square) -> Vec<Square> {
    if footprint.is_empty() {
        return Vec::new();
    }
    let min_x = footprint.iter().map(|c| c.0).min().unwrap_or(0);
    let min_y = footprint.iter().map(|c| c.1).min().unwrap_or(0);
    footprint
        .iter()
        .map(|(x, y)| (at.0 + (x - min_x), at.1 + (y - min_y)))
        .collect()
}

/// The boundary edges of a footprint: for every footprint square, every side
/// whose adjacent square is not itself in the footprint.
pub fn boundary_edges(footprint: &[Square]) -> Vec<BoundaryEdge> {
    let set: std::collections::HashSet<Square> = footprint.iter().cloned().collect();
    let mut edges = Vec::new();
    for &sq in footprint {
        for side in Side::all() {
            if !set.contains(&side.neighbour(sq)) {
                edges.push(BoundaryEdge { square: sq, side });
            }
        }
    }
    edges
}

/// Centers a portal span of `portal_len` inside an opening of `open_len`
/// (starting at `open_start`), with a lower-index bias: the odd excess cell
/// lands on the high side. Returns the portal's (start, length).
///
/// `portal_len = min(openA, openB)`; each side is centered independently in its
/// own cell space. `floor()` is the lower-index bias.
pub fn center_span(open_start: i32, open_len: i32, portal_len: i32) -> (i32, i32) {
    let offset = ((open_len - portal_len) / 2).max(0);
    (open_start + offset, portal_len)
}

/// Computes the full state of a grid from its sector declarations: places each
/// footprint, records boundary edges + openings per square, discovers
/// neighbours across adjacent squares, and forms portals from facing openings.
pub fn compute_grid(id: &str, decls: &[SectorDeclaration]) -> SectorGridState {
    // Per-square owner (container id).
    let mut owner: std::collections::HashMap<Square, String> = std::collections::HashMap::new();
    // Per-square boundary edges + openings, keyed by (container, square, side).
    let mut edges: Vec<(String, Square, Side)> = Vec::new();
    let mut openings: Vec<(String, Square, Side, Opening)> = Vec::new();

    for d in decls {
        let min_x = d.footprint.iter().map(|c| c.0).min().unwrap_or(0);
        let min_y = d.footprint.iter().map(|c| c.1).min().unwrap_or(0);
        // Local (relative-to-footprint) cell -> grid cell: the opening's
        // declared `cell` is relative to the sector's own footprint origin,
        // so translate by (at - footprint_min) to land it on the same grid
        // coordinate the footprint square occupies.
        let to_grid = |c: Square| -> Square {
            (d.at.0 + (c.0 - min_x), d.at.1 + (c.1 - min_y))
        };
        let placed = normalize_footprint(&d.footprint, d.at);
        let set: std::collections::HashSet<Square> = placed.iter().cloned().collect();
        for &sq in &placed {
            owner.insert(sq, d.container.clone());
        }
        for &sq in &placed {
            for side in Side::all() {
                if !set.contains(&side.neighbour(sq)) {
                    edges.push((d.container.clone(), sq, side));
                }
            }
        }
        for o in &d.openings {
            let grid_cell = to_grid(o.cell);
            let opening = Opening { cell: grid_cell, ..*o };
            openings.push((d.container.clone(), grid_cell, o.side, opening));
        }
    }

    // Index edges and openings by (container, square). An opening is filed
    // under the square it declares (`o.cell`), not the cell it happens to
    // render on, so a footprint with several squares can carry openings on
    // different squares.
    let mut edges_by_sq: std::collections::HashMap<(String, Square), Vec<Side>> =
        std::collections::HashMap::new();
    let mut openings_by_sq: std::collections::HashMap<(String, Square), Vec<Opening>> =
        std::collections::HashMap::new();
    for &(ref container, ref sq, side) in &edges {
        edges_by_sq.entry((container.clone(), *sq)).or_default().push(side);
    }
    for &(ref container, ref osq, _, ref opening) in &openings {
        openings_by_sq
            .entry((container.clone(), *osq))
            .or_default()
            .push(opening.clone());
    }

    // Build one cell per occupied square that owns a boundary edge.
    let mut cells: Vec<SectorCell> = Vec::new();
    let keys: Vec<(String, Square)> =
        edges_by_sq.keys().map(|(c, s)| (c.clone(), *s)).collect();
    for (ref container, ref sq) in &keys {
        let mut boundary_edges: Vec<BoundaryEdge> = edges_by_sq
            .get(&((*container).clone(), *sq))
            .map(|sides| {
                sides
                    .iter()
                    .map(|side| BoundaryEdge { square: *sq, side: *side })
                    .collect::<Vec<_>>()
            })
            .unwrap_or_default();
        boundary_edges.sort_by(|a, b| a.side.cmp(&b.side));
        let mut cell_openings: Vec<Opening> = openings_by_sq
            .get(&((*container).clone(), *sq))
            .cloned()
            .unwrap_or_default();
        cell_openings.sort_by(|a, b| a.side.cmp(&b.side).then(a.start.cmp(&b.start)));
        cells.push(SectorCell {
            x: sq.0,
            y: sq.1,
            container: container.clone(),
            boundary_edges,
            openings: cell_openings,
        });
    }
    cells.sort_by(|a, b| a.x.cmp(&b.x).then(a.y.cmp(&b.y)));

    // Portal matching: for each boundary edge of A, if the adjacent square is
    // owned by B != A, look for a matching opening on both sides. Facing edges
    // pair by (square, side) and (adjacent square, opposite side).
    let mut portals: Vec<Portal> = Vec::new();
    let mut next_portal = 0usize;
    let opening_at = |container: &str, sq: Square, side: Side| -> Option<&Opening> {
        openings
            .iter()
            .find(|(c, osq, oside, _)| {
                c == container && *osq == sq && *oside == side
            })
            .map(|(_, _, _, opening)| opening)
    };

    for &(ref a_container, ref a_sq, a_side) in &edges {
        let neighbor_sq = a_side.neighbour(*a_sq);
        let b_container = match owner.get(&neighbor_sq) {
            Some(c) if c != a_container => c.clone(),
            _ => continue,
        };
        // Each facing pair is visited twice (once from each side); emit the
        // portal only from the lower-named container so it is counted once.
        if a_container >= &b_container {
            continue;
        }
        let b_side = a_side.opposite();
        let (Some(a_open), Some(b_open)) =
            (opening_at(a_container, *a_sq, a_side), opening_at(&b_container, neighbor_sq, b_side))
        else {
            continue;
        };
        let portal_len = (a_open.length.min(b_open.length)).max(1);
        let (a_span, a_len) = center_span(a_open.start, a_open.length, portal_len);
        let (b_span, b_len) = center_span(b_open.start, b_open.length, portal_len);
        next_portal += 1;
        portals.push(Portal {
            id: format!("p{}", next_portal),
            a: PortalSide { cell: *a_sq, side: a_side, span: a_span, length: a_len },
            b: PortalSide { cell: neighbor_sq, side: b_side, span: b_span, length: b_len },
        });
    }
    // Deterministic portal order: by (a cell, a side, b cell, b side).
    portals.sort_by(|p, q| {
        (p.a.cell, p.a.side, p.b.cell, p.b.side).cmp(&(q.a.cell, q.a.side, q.b.cell, q.b.side))
    });

    SectorGridState {
        id: id.to_string(),
        cells,
        portals,
    }
}

/// The process-global sector grids, keyed by grid id.
pub fn sector_grids() -> &'static std::sync::Mutex<std::collections::HashMap<String, SectorGridState>> {
    super::persisted_flag();
    unsafe { super::SECTOR_GRIDS.expect("sector grids initialized") }
}

/// Replace the stored state for a grid.
pub fn set_sector_grid(state: SectorGridState) {
    sector_grids().lock().unwrap().insert(state.id.clone(), state);
}

/// The stored state for a grid, if any.
pub fn fetch_sector_grid(id: &str) -> Option<SectorGridState> {
    sector_grids().lock().unwrap().get(id).cloned()
}

/// All sector grids as JSON: `{"<gridId>": SectorGridState, ...}`.
pub fn fetch_sector_grids_json() -> String {
    let grids = sector_grids().lock().unwrap().clone();
    serde_json::json!(grids).to_string()
}

#[cfg(test)]
mod tests {
    use super::*;
    use crate::state::test_lock;

    fn sq(x: i32, y: i32) -> Square {
        (x, y)
    }

    #[test]
    fn normalize_places_footprint_at_anchor() {
        // L footprint normalized to min 0, placed at (3, 2).
        let fp = vec![sq(0, 0), sq(1, 0), sq(0, 1)];
        let placed = normalize_footprint(&fp, (3, 2));
        let expect = [sq(3, 2), sq(4, 2), sq(3, 3)];
        let mut got = placed;
        got.sort();
        let mut want = expect.to_vec();
        want.sort();
        assert_eq!(got, want);
    }

    #[test]
    fn l_footprint_has_eight_boundary_edges() {
        // L: (0,0) (1,0) (0,1). The four interior edges — (0,0)E,S, (1,0)W,
        // (0,1)N — are not boundary edges.
        let fp = vec![sq(0, 0), sq(1, 0), sq(0, 1)];
        let edges = boundary_edges(&fp);
        assert_eq!(edges.len(), 8);
        let has = |s: Square, side: Side| edges.iter().any(|e| e.square == s && e.side == side);
        // (0,0) N W
        assert!(has(sq(0, 0), Side::N));
        assert!(has(sq(0, 0), Side::W));
        // (1,0) N E S
        assert!(has(sq(1, 0), Side::N));
        assert!(has(sq(1, 0), Side::E));
        assert!(has(sq(1, 0), Side::S));
        // (0,1) E S W
        assert!(has(sq(0, 1), Side::E));
        assert!(has(sq(0, 1), Side::S));
        assert!(has(sq(0, 1), Side::W));
        // interior edges must be absent
        assert!(!has(sq(0, 0), Side::E));
        assert!(!has(sq(0, 0), Side::S));
        assert!(!has(sq(1, 0), Side::W));
        assert!(!has(sq(0, 1), Side::N));
    }

    #[test]
    fn center_span_matches_spec_examples() {
        // lenA 4, lenB 3 -> L 3, oA 0. A portal start 0 length 4 -> span (0,3).
        assert_eq!(center_span(0, 4, 3), (0, 3));
        // lenA 10, lenB 3 -> L 3, oA 3. A portal start 0 length 10 -> span (3,3).
        assert_eq!(center_span(0, 10, 3), (3, 3));
        // Equal lengths: no offset.
        assert_eq!(center_span(7, 4, 4), (7, 4));
        // Odd excess lands on the high side (lower-index bias).
        assert_eq!(center_span(0, 5, 4), (0, 4));
        assert_eq!(center_span(0, 5, 2), (1, 2));
    }

    fn one_square(container: &str, at: Square) -> SectorDeclaration {
        SectorDeclaration {
            grid: "g".into(),
            container: container.into(),
            at,
            footprint: vec![sq(0, 0)],
            openings: Vec::new(),
        }
    }

    #[test]
    fn adjacent_squares_link_without_openings() {
        let a = one_square("a", (0, 0));
        let b = one_square("b", (1, 0));
        let state = compute_grid("g", &[a, b]);
        // Two cells, each with a boundary edge facing the other; no openings -> no portal.
        assert_eq!(state.cells.len(), 2);
        assert!(state.portals.is_empty());
    }

    #[test]
    fn facing_openings_form_a_portal() {
        let a = SectorDeclaration {
            grid: "g".into(),
            container: "a".into(),
            at: (0, 0),
            footprint: vec![sq(0, 0)],
            openings: vec![Opening { cell: sq(0, 0), side: Side::E, start: 0, length: 4 }],
        };
        let b = SectorDeclaration {
            grid: "g".into(),
            container: "b".into(),
            at: (1, 0),
            footprint: vec![sq(0, 0)],
            openings: vec![Opening { cell: sq(0, 0), side: Side::W, start: 0, length: 4 }],
        };
        let state = compute_grid("g", &[a, b]);
        assert_eq!(state.portals.len(), 1);
        let p = &state.portals[0];
        // A's opening is on (0,0) E; B's on (1,0) W (the adjacent square).
        assert_eq!(p.a.cell, (0, 0));
        assert_eq!(p.a.side, Side::E);
        assert_eq!(p.b.cell, (1, 0));
        assert_eq!(p.b.side, Side::W);
        // Equal lengths -> full span, no centering.
        assert_eq!(p.a.span, 0);
        assert_eq!(p.a.length, 4);
        assert_eq!(p.b.span, 0);
        assert_eq!(p.b.length, 4);
    }

    #[test]
    fn mismatched_lengths_center_the_portal() {
        let a = SectorDeclaration {
            grid: "g".into(),
            container: "a".into(),
            at: (0, 0),
            footprint: vec![sq(0, 0)],
            openings: vec![Opening { cell: sq(0, 0), side: Side::E, start: 0, length: 10 }],
        };
        let b = SectorDeclaration {
            grid: "g".into(),
            container: "b".into(),
            at: (1, 0),
            footprint: vec![sq(0, 0)],
            openings: vec![Opening { cell: sq(0, 0), side: Side::W, start: 0, length: 3 }],
        };
        let state = compute_grid("g", &[a, b]);
        assert_eq!(state.portals.len(), 1);
        let p = &state.portals[0];
        // L = 3; A centered: oA = floor((10-3)/2) = 3 -> span (3,3). B full -> (0,3).
        assert_eq!(p.a.span, 3);
        assert_eq!(p.a.length, 3);
        assert_eq!(p.b.span, 0);
        assert_eq!(p.b.length, 3);
    }

    #[test]
    fn concave_pocket_yields_two_cycle() {
        // A is the L: (0,0)(1,0)(0,1). B fills the pocket at (1,1).
        // Both (1,0)S and (0,1)E of A face B's (1,1); B's N and W face back.
        // Openings on all four facing edges -> TWO portals between A and B.
        let a = SectorDeclaration {
            grid: "g".into(),
            container: "a".into(),
            at: (0, 0),
            footprint: vec![sq(0, 0), sq(1, 0), sq(0, 1)],
            openings: vec![
                Opening { cell: sq(1, 0), side: Side::S, start: 0, length: 4 },
                Opening { cell: sq(0, 1), side: Side::E, start: 0, length: 4 },
            ],
        };
        let b = SectorDeclaration {
            grid: "g".into(),
            container: "b".into(),
            at: (1, 1),
            footprint: vec![sq(0, 0)],
            openings: vec![
                Opening { cell: sq(0, 0), side: Side::N, start: 0, length: 4 },
                Opening { cell: sq(0, 0), side: Side::W, start: 0, length: 4 },
            ],
        };
        let state = compute_grid("g", &[a, b]);
        // A's pocket edges (1,0)S and (0,1)E; B's N and W. Four facing pairs,
        // but only the two that have openings on BOTH sides form portals.
        assert_eq!(state.portals.len(), 2);
        let keys: Vec<(Square, Side, Square, Side)> = state
            .portals
            .iter()
            .map(|p| (p.a.cell, p.a.side, p.b.cell, p.b.side))
            .collect();
        assert!(keys.contains(&((1, 0), Side::S, (1, 1), Side::N)));
        assert!(keys.contains(&((0, 1), Side::E, (1, 1), Side::W)));
    }

    #[test]
    fn grid_state_round_trips_through_json() {
        let _g = test_lock();
        let a = SectorDeclaration {
            grid: "cave".into(),
            container: "room-a".into(),
            at: (0, 0),
            footprint: vec![sq(0, 0)],
            openings: vec![Opening { cell: sq(0, 0), side: Side::N, start: 13, length: 4 }],
        };
        let state = compute_grid("cave", &[a]);
        let json = serde_json::to_string(&state).unwrap();
        let parsed: SectorGridState = serde_json::from_str(&json).unwrap();
        assert_eq!(parsed, state);
        assert_eq!(parsed.id, "cave");
        assert_eq!(parsed.cells.len(), 1);
        assert_eq!(parsed.cells[0].openings.len(), 1);
        assert_eq!(parsed.cells[0].openings[0].start, 13);
    }
}
