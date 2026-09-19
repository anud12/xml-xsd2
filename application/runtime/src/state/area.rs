//! Continuous polygonal areas attached to entities.
//!
//! An area is a polygon in entity-local units (y increases downward),
//! implicitly closed. It is decomposed once, at declare time, into convex
//! pieces so every per-query intersection test is convex-vs-convex (SAT).
//! An AABB is cached per area for broad-phase rejection. An entity with no
//! area has no presence in the simulation.

/// One convex piece of a decomposed area, world-translated.
#[derive(Debug, Clone, PartialEq)]
pub struct Poly {
    pub pts: Vec<(f64, f64)>,
    pub aabb: Aabb,
}

#[derive(Debug, Clone, Copy, PartialEq)]
pub struct Aabb {
    pub min_x: f64,
    pub min_y: f64,
    pub max_x: f64,
    pub max_y: f64,
}

impl Aabb {
    pub fn contains_point(&self, x: f64, y: f64) -> bool {
        x >= self.min_x && x <= self.max_x && y >= self.min_y && y <= self.max_y
    }

    pub fn overlaps(&self, o: &Aabb) -> bool {
        self.min_x <= o.max_x && o.min_x <= self.max_x
            && self.min_y <= o.max_y && o.min_y <= self.max_y
    }
}

/// The declared area of one entity: the raw local polygon (kept for
/// introspection) and its convex decomposition, both in local units.
#[derive(Debug, Clone, PartialEq)]
pub struct Area {
    pub local: Vec<(f64, f64)>,
    pub pieces: Vec<Poly>,
}

impl Area {
    pub fn aabb(&self) -> Aabb {
        self.pieces[0].aabb
    }

    /// Translate the convex pieces to world space by (dx, dy).
    pub fn translated(&self, dx: f64, dy: f64) -> Vec<Poly> {
        self.pieces
            .iter()
            .map(|p| {
                let pts: Vec<(f64, f64)> = p.pts.iter().map(|(x, y)| (x + dx, y + dy)).collect();
                Poly {
                    aabb: aabb_of(&pts),
                    pts,
                }
            })
            .collect()
    }
}

fn aabb_of(pts: &[(f64, f64)]) -> Aabb {
    let min_x = pts.iter().map(|p| p.0).fold(f64::INFINITY, f64::min);
    let max_x = pts.iter().map(|p| p.0).fold(f64::NEG_INFINITY, f64::max);
    let min_y = pts.iter().map(|p| p.1).fold(f64::INFINITY, f64::min);
    let max_y = pts.iter().map(|p| p.1).fold(f64::NEG_INFINITY, f64::max);
    Aabb { min_x, min_y, max_x, max_y }
}

/// Build an Area from a local polygon: normalize (dedupe consecutive
/// duplicates, enforce CCW winding so area is positive) and decompose.
/// Returns None for degenerate input (fewer than 3 distinct points or
/// zero area).
pub fn make_area(poly: &[(f64, f64)]) -> Option<Area> {
    let pts = normalize(poly);
    if pts.len() < 3 {
        return None;
    }
    let signed = signed_area(&pts);
    if signed.abs() < 1e-12 {
        return None;
    }
    let pts = if signed > 0.0 { pts } else { let mut r = pts.clone(); r.reverse(); r };
    let pieces = if is_convex(&pts) {
        vec![Poly { aabb: aabb_of(&pts), pts: pts.clone() }]
    } else {
        ear_cut(&pts)
    };
    Some(Area { local: pts, pieces })
}

/// Signed area (positive for CCW winding).
fn signed_area(pts: &[(f64, f64)]) -> f64 {
    let mut s = 0.0;
    for i in 0..pts.len() {
        let (x1, y1) = pts[i];
        let (x2, y2) = pts[(i + 1) % pts.len()];
        s += x1 * y2 - x2 * y1;
    }
    s * 0.5
}

/// The convex test assumes CCW winding: an edge (a->b) supports the polygon
/// when the next vertex lies left of the directed edge (cross > 0).
fn is_convex(pts: &[(f64, f64)]) -> bool {
    let n = pts.len();
    if n < 3 {
        return false;
    }
    for i in 0..n {
        let a = pts[i];
        let b = pts[(i + 1) % n];
        let c = pts[(i + 2) % n];
        if cross(b.0 - a.0, b.1 - a.1, c.0 - b.0, c.1 - b.1) <= 1e-12 {
            return false;
        }
    }
    true
}

/// Decompose a simple CCW polygon into convex pieces via ear clipping.
fn ear_cut(pts_in: &[(f64, f64)]) -> Vec<Poly> {
    let mut pts: Vec<(f64, f64)> = pts_in.to_vec();
    let mut remaining: Vec<usize> = (0..pts.len()).collect();
    let mut pieces: Vec<Vec<(f64, f64)>> = Vec::new();
    let mut guard = 0;
    while remaining.len() > 3 && guard < 100000 {
        guard += 1;
        let mut cut = false;
        let len = remaining.len();
        for i in 0..len {
            if is_ear(&pts, &remaining, i) {
                let a = pts[remaining[(i + len - 1) % len]];
                let b = pts[remaining[i]];
                let c = pts[remaining[(i + 1) % len]];
                pieces.push(vec![a, b, c]);
                remaining.remove(i);
                cut = true;
                break;
            }
        }
        if !cut {
            // Degenerate leftover (collinear spikes, etc.): emit as one piece.
            let poly: Vec<(f64, f64)> =
                remaining.iter().map(|&i| pts[i]).collect();
            if poly.len() >= 3 && signed_area(&poly).abs() >= 1e-12 {
                pieces.push(poly);
            }
            break;
        }
    }
    if remaining.len() == 3 {
        let poly: Vec<(f64, f64)> =
            remaining.iter().map(|&i| pts[i]).collect();
        pieces.push(poly);
    }
    pieces
        .into_iter()
        .map(|p| Poly { aabb: aabb_of(&p), pts: p })
        .collect()
}

/// A vertex (previous, current, next) is an ear when it is convex and no
/// other vertex lies strictly inside the triangle it would cut.
fn is_ear(pts: &[(f64, f64)], rem: &[usize], i: usize) -> bool {
    let len = rem.len();
    let a = pts[rem[(i + len - 1) % len]];
    let b = pts[rem[i]];
    let c = pts[rem[(i + 1) % len]];
    if cross(b.0 - a.0, b.1 - a.1, c.0 - b.0, c.1 - b.1) <= 1e-12 {
        return false;
    }
    for &j in rem.iter() {
        let vi = rem[(i + 1) % len];
        let vj = rem[(i + len - 1) % len];
        if j == vi || j == vj {
            continue;
        }
        let p = pts[j];
        if point_in_triangle(p, a, b, c) {
            return false;
        }
    }
    true
}

fn cross(ax: f64, ay: f64, bx: f64, by: f64) -> f64 {
    ax * by - ay * bx
}

/// Strict interior test for a CCW triangle.
fn point_in_triangle(p: (f64, f64), a: (f64, f64), b: (f64, f64), c: (f64, f64)) -> bool {
    let e1 = cross(b.0 - a.0, b.1 - a.1, p.0 - a.0, p.1 - a.1);
    let e2 = cross(c.0 - b.0, c.1 - b.1, p.0 - b.0, p.1 - b.1);
    let e3 = cross(a.0 - c.0, a.1 - c.1, p.0 - c.0, p.1 - c.1);
    e1 > 1e-12 && e2 > 1e-12 && e3 > 1e-12
}

/// SAT overlap of two convex CCW polygons.
pub fn polys_overlap(a: &Poly, b: &Poly) -> bool {
    for i in 0..a.pts.len() {
        let pa = a.pts[i];
        let pb = a.pts[(i + 1) % a.pts.len()];
        let normal = (-(pb.1 - pa.1), pb.0 - pa.0);
        if separated(&a.pts, &b.pts, normal) {
            return false;
        }
    }
    for i in 0..b.pts.len() {
        let pa = b.pts[i];
        let pb = b.pts[(i + 1) % b.pts.len()];
        let normal = (-(pb.1 - pa.1), pb.0 - pa.0);
        if separated(&a.pts, &b.pts, normal) {
            return false;
        }
    }
    true
}

/// Two convex polygons are separated when their projections along a normal do
/// not overlap. Touching along a single point or edge (equal extremes) is NOT
/// overlap: we want interior intersection, so the comparison is strict and a
/// tiny tolerance absorbs float noise on shared boundaries.
fn separated(
    a: &[(f64, f64)],
    b: &[(f64, f64)],
    normal: (f64, f64),
) -> bool {
    let (amin, amax) = project(a, normal);
    let (bmin, bmax) = project(b, normal);
    amax < bmin + 1e-9 || bmax < amin + 1e-9
}

fn project(pts: &[(f64, f64)], normal: (f64, f64)) -> (f64, f64) {
    let mut min = f64::INFINITY;
    let mut max = f64::NEG_INFINITY;
    for (x, y) in pts {
        let d = x * normal.0 + y * normal.1;
        if d < min { min = d; }
        if d > max { max = d; }
    }
    (min, max)
}

/// Two areas (world-translated) overlap: AABB broad phase, then SAT per
/// convex-pair.
pub fn areas_overlap(a: &[Poly], b: &[Poly]) -> bool {
    for pa in a {
        for pb in b {
            if pa.aabb.overlaps(&pb.aabb) && polys_overlap(pa, pb) {
                return true;
            }
        }
    }
    false
}

/// A world point is inside the area.
pub fn area_contains(a: &[Poly], x: f64, y: f64) -> bool {
    for p in a {
        if !p.aabb.contains_point(x, y) {
            continue;
        }
        if point_in_poly(p, x, y) {
            return true;
        }
    }
    false
}

/// Point in a CCW convex polygon (on-edge counts as inside).
fn point_in_poly(poly: &Poly, x: f64, y: f64) -> bool {
    let n = poly.pts.len();
    for i in 0..n {
        let a = poly.pts[i];
        let b = poly.pts[(i + 1) % n];
        if cross(b.0 - a.0, b.1 - a.1, x - a.0, y - a.1) < -1e-12 {
            return false;
        }
    }
    true
}

/// Dedupe consecutive duplicate vertices (closing vertex included or not)
/// and drop points collinear along a straight run.
fn normalize(poly: &[(f64, f64)]) -> Vec<(f64, f64)> {
    let mut out: Vec<(f64, f64)> = Vec::new();
    for p in poly {
        if out.last() != Some(p) {
            out.push(*p);
        }
    }
    while out.len() > 1 && out.first().copied() == out.last().copied() {
        out.pop();
    }
    // Drop collinear middle points (a, b, c with b on segment ac).
    let mut cleaned: Vec<(f64, f64)> = Vec::new();
    let mut i = 0;
    while i < out.len() {
        let a = out[i];
        let b = out[(i + 1) % out.len()];
        let c = out[(i + 2) % out.len()];
        let col =
            (cross(b.0 - a.0, b.1 - a.1, c.0 - b.0, c.1 - b.1)).abs() < 1e-12
                && dot(b.0 - a.0, b.1 - a.1, c.0 - a.0, c.1 - a.1) > 0.0;
        if col {
            // b is a pass-through; skip it.
        } else {
            cleaned.push(b);
        }
        i += 1;
    }
    if cleaned.is_empty() {
        out
    } else {
        cleaned
    }
}

fn dot(ax: f64, ay: f64, bx: f64, by: f64) -> f64 {
    ax * bx + ay * by
}

#[cfg(test)]
mod tests {
    use super::*;

    fn rect(x: f64, y: f64, w: f64, h: f64) -> Vec<(f64, f64)> {
        vec![
            (x, y),
            (x + w, y),
            (x + w, y + h),
            (x, y + h),
        ]
    }

    // The plan's cliff: an L shape (concave), local units.
    const CLIFF: &[(f64, f64)] = &[
        (0.0, 0.0),
        (200.0, 0.0),
        (200.0, 64.0),
        (96.0, 64.0),
        (96.0, 128.0),
        (0.0, 128.0),
    ];

    #[test]
    fn convex_rect_is_a_single_piece() {
        let a = make_area(&rect(0.0, 0.0, 10.0, 5.0)).unwrap();
        assert_eq!(a.pieces.len(), 1);
        assert_eq!(a.pieces[0].pts.len(), 4);
        assert_eq!(a.aabb(), Aabb { min_x: 0.0, min_y: 0.0, max_x: 10.0, max_y: 5.0 });
    }

    #[test]
    fn concave_l_decomposes_into_convex_pieces() {
        let a = make_area(CLIFF).unwrap();
        assert!(a.pieces.len() >= 2, "concave polygon needs more than one piece");
        for p in &a.pieces {
            assert!(is_convex(&p.pts), "piece must be convex");
        }
        // The decomposition conserves area (|signed area| per piece sums to the whole).
        let whole = signed_area(&a.local).abs();
        let sum: f64 = a.pieces.iter().map(|p| signed_area(&p.pts).abs()).sum();
        assert!((whole - sum).abs() < 1e-9, "area conserved: {} vs {}", whole, sum);
    }

    #[test]
    fn overlapping_areas_detected() {
        let a = make_area(&rect(0.0, 0.0, 10.0, 10.0)).unwrap();
        let b = make_area(&rect(5.0, 5.0, 10.0, 10.0)).unwrap();
        assert!(areas_overlap(&a.translated(0.0, 0.0), &b.translated(0.0, 0.0)));
    }

    #[test]
    fn disjoint_areas_rejected_by_aabb() {
        let a = make_area(&rect(0.0, 0.0, 10.0, 10.0)).unwrap();
        let b = make_area(&rect(50.0, 50.0, 10.0, 10.0)).unwrap();
        assert!(!areas_overlap(&a.translated(0.0, 0.0), &b.translated(0.0, 0.0)));
    }

    #[test]
    fn concave_pocket_only_overlaps_when_reaching_it() {
        let l = make_area(CLIFF).unwrap();
        // A rectangle sitting in the pocket (x 96..200, y 64..128): touches
        // the L's inner corner only along an edge, no interior overlap.
        let pocket = make_area(&rect(96.0, 64.0, 104.0, 64.0)).unwrap();
        assert!(!areas_overlap(&l.translated(0.0, 0.0), &pocket.translated(0.0, 0.0)));
        // Nudge it into the body: real overlap now.
        let nudge = make_area(&rect(90.0, 60.0, 100.0, 60.0)).unwrap();
        assert!(areas_overlap(&l.translated(0.0, 0.0), &nudge.translated(0.0, 0.0)));
    }

    #[test]
    fn point_in_aab_but_outside_the_concave_polygon_is_not_inside() {
        // The cliff's AABB is (0,0)..(200,128). Its top-right quarter
        // (x 96..200, y 64..128) is a pocket cut out of the polygon: a point
        // there is inside the bounding box but outside every convex piece.
        let a = make_area(CLIFF).unwrap();
        let t = a.translated(0.0, 0.0);

        // The broad-phase box covering the whole area (union of the pieces'
        // AABBs) admits the point ...
        let (mut min_x, mut min_y) = (f64::INFINITY, f64::INFINITY);
        let (mut max_x, mut max_y) = (f64::NEG_INFINITY, f64::NEG_INFINITY);
        for p in &t {
            min_x = min_x.min(p.aabb.min_x);
            min_y = min_y.min(p.aabb.min_y);
            max_x = max_x.max(p.aabb.max_x);
            max_y = max_y.max(p.aabb.max_y);
        }
        let whole = Aabb { min_x, min_y, max_x, max_y };

        // ... and a point deep in the pocket is inside that AABB ...
        let (px, py) = (150.0, 96.0);
        assert!(whole.contains_point(px, py), "point should be inside the area AABB");
        // ... but is outside the polygon itself (no convex piece contains it).
        assert!(!area_contains(&t, px, py), "pocket point must not be inside the polygon");

        // A point in the solid body of the L is inside both.
        assert!(area_contains(&t, 40.0, 96.0));
    }

    #[test]
    fn translated_pieces_track_the_entity_position() {
        let a = make_area(&rect(0.0, 0.0, 10.0, 10.0)).unwrap();
        let t = a.translated(320.0, 128.0);
        assert_eq!(t[0].aabb, Aabb { min_x: 320.0, min_y: 128.0, max_x: 330.0, max_y: 138.0 });
        assert!(area_contains(&t, 325.0, 133.0));
        assert!(!area_contains(&t, 319.0, 127.0));
    }

    #[test]
    fn degenerate_input_is_rejected() {
        assert!(make_area(&[(0.0, 0.0), (1.0, 1.0)]).is_none());
        assert!(make_area(&[(0.0, 0.0), (1.0, 0.0), (2.0, 0.0)]).is_none());
        assert!(make_area(&[(0.0, 0.0), (0.0, 0.0), (1.0, 0.0), (1.0, 1.0)]).is_some());
    }

    #[test]
    fn cw_winding_is_normalized_to_ccw() {
        let cw = vec![
            (0.0, 0.0),
            (0.0, 10.0),
            (10.0, 10.0),
            (10.0, 0.0),
        ];
        let a = make_area(&cw).unwrap();
        assert!(signed_area(&a.local) > 0.0);
        assert_eq!(a.pieces.len(), 1);
    }
}
