//! UI state store + id-diff over the persistent sim context.
//!
//! The .ui layer (application/ui/ui/*.js) lives in the module's persistent
//! QuickJS context (crate::js_executor::sim_ctx): the module entry declares
//! nodes once through the live `globalThis.host.ui` factories, and each tick
//! re-expands container lists against the current runtime containers,
//! snapshots the result, and reconciles it into the id-keyed node store
//! (`UI_NODES`) that renderers fetch. This module owns the Rust side of that
//! seam:
//!
//! - the id-keyed UI node store (`UI_NODES`) that renderers fetch;
//! - the id-diff producing a `UiDelta` (add/update/remove).

#![allow(dead_code)]
pub mod abi;

use std::collections::HashMap;
use std::sync::atomic::{AtomicBool, Ordering};
use std::sync::{Mutex, Once};

use anyhow::Result;
use serde::{Deserialize, Serialize};

// ---------------------------------------------------------------------------
// Node / delta model
// ---------------------------------------------------------------------------

/// A UI node. The delta format carries the full node state so a renderer can
/// rebuild any node from a single add/update message.
#[derive(Debug, Clone, PartialEq, Serialize, Deserialize)]
#[serde(tag = "kind")]
pub enum UiNode {
    #[serde(rename = "division")]
    Division {
        id: String,
        #[serde(default)]
        options: serde_json::Value,
        #[serde(default)]
        children: Vec<String>,
    },
    #[serde(rename = "text")]
    Text {
        id: String,
        #[serde(default)]
        value: String,
        #[serde(default)]
        children: Vec<String>,
    },
    #[serde(rename = "field")]
    Field {
        id: String,
        #[serde(default)]
        binding: serde_json::Value,
        #[serde(default)]
        value: String,
        #[serde(default)]
        children: Vec<String>,
    },
    #[serde(rename = "window")]
    Window {
        id: String,
        #[serde(default)]
        options: serde_json::Value,
        #[serde(default)]
        children: Vec<String>,
    },
    #[serde(rename = "screen")]
    Screen {
        id: String,
        #[serde(default)]
        options: serde_json::Value,
        #[serde(default)]
        children: Vec<String>,
    },
    #[serde(rename = "image")]
    Image {
        id: String,
        #[serde(default)]
        src: String,
        #[serde(default)]
        children: Vec<String>,
    },
    #[serde(rename = "progress")]
    Progress {
        id: String,
        /// Resolved 0..1 fill fraction (bound nodes are re-resolved per tick).
        #[serde(default)]
        value: f64,
        /// Optional entity number binding `{ entity, map, name, fallback }`.
        #[serde(default)]
        binding: serde_json::Value,
        /// Panel options plus `fill`/`track`/`orientation`/`min`/`max`.
        #[serde(default)]
        options: serde_json::Value,
        #[serde(default)]
        children: Vec<String>,
    },
}

impl UiNode {
    pub fn id(&self) -> &str {
        match self {
            UiNode::Division { id, .. } => id,
            UiNode::Text { id, .. } => id,
            UiNode::Field { id, .. } => id,
            UiNode::Window { id, .. } => id,
            UiNode::Screen { id, .. } => id,
            UiNode::Image { id, .. } => id,
            UiNode::Progress { id, .. } => id,
        }
    }
}

#[derive(Debug, Clone, PartialEq, Serialize, Deserialize)]
pub struct UiDelta {
    pub ops: Vec<UiDeltaOp>,
}

#[derive(Debug, Clone, PartialEq, Serialize, Deserialize)]
#[serde(tag = "op")]
pub enum UiDeltaOp {
    #[serde(rename = "add")]
    Add { node: UiNode },
    #[serde(rename = "update")]
    Update { node: UiNode },
    #[serde(rename = "remove")]
    Remove { id: String },
}

// ---------------------------------------------------------------------------
// Globals
// ---------------------------------------------------------------------------

static INIT: Once = Once::new();
static mut UI_NODES: Option<&'static Mutex<Vec<UiNode>>> = None;
static mut UI_DIRTY: Option<&'static AtomicBool> = None;
static mut UI_DELTA: Option<&'static Mutex<Option<UiDelta>>> = None;
static mut PREV_STORE_IDS: Option<&'static Mutex<Vec<String>>> = None;
static mut UI_MODULE_OWNERS:
    Option<&'static Mutex<HashMap<String, Vec<String>>>> = None;
static mut UI_ANIMATIONS:
    Option<&'static Mutex<HashMap<String, serde_json::Value>>> = None;

fn init() {
    INIT.call_once(|| unsafe {
        UI_NODES = Some(Box::leak(Box::new(Mutex::new(Vec::new()))));
        UI_DIRTY = Some(Box::leak(Box::new(AtomicBool::new(false))));
        UI_DELTA = Some(Box::leak(Box::new(Mutex::new(None))));
        PREV_STORE_IDS = Some(Box::leak(Box::new(Mutex::new(Vec::new()))));
        UI_MODULE_OWNERS = Some(Box::leak(Box::new(Mutex::new(HashMap::new()))));
        UI_ANIMATIONS = Some(Box::leak(Box::new(Mutex::new(HashMap::new()))));
    });
}

pub fn ui_nodes() -> &'static Mutex<Vec<UiNode>> {
    init();
    unsafe { UI_NODES.expect("ui nodes initialized") }
}
pub fn ui_dirty() -> &'static AtomicBool {
    init();
    unsafe { UI_DIRTY.expect("ui dirty initialized") }
}
pub fn ui_delta() -> &'static Mutex<Option<UiDelta>> {
    init();
    unsafe { UI_DELTA.expect("ui delta initialized") }
}
fn prev_store_ids() -> &'static Mutex<Vec<String>> {
    init();
    unsafe { PREV_STORE_IDS.expect("prev store ids initialized") }
}
pub fn module_owners() -> &'static Mutex<HashMap<String, Vec<String>>> {
    init();
    unsafe { UI_MODULE_OWNERS.expect("module owners initialized") }
}
pub fn animations() -> &'static Mutex<HashMap<String, serde_json::Value>> {
    init();
    unsafe { UI_ANIMATIONS.expect("ui animations initialized") }
}
/// Clear all UI state (called from `runtime_clear_state`).
pub fn clear() {
    init();
    ui_nodes().lock().unwrap().clear();
    prev_store_ids().lock().unwrap().clear();
    module_owners().lock().unwrap().clear();
    animations().lock().unwrap().clear();
    *ui_delta().lock().unwrap() = None;
    ui_dirty().store(false, Ordering::SeqCst);
}

// ---------------------------------------------------------------------------
// Tick (runs in the persistent sim context)
// ---------------------------------------------------------------------------

fn eval_string(ctx: &rquickjs::Context, script: &str) -> Result<String> {
    crate::js_executor::sim_ctx::sim_with(ctx, |c| c.eval::<String, _>(script.to_string()))
        .map_err(|e| anyhow::anyhow!("ui eval failed: {:?}", e))
}

/// Re-expand the module's container lists against the current runtime
/// containers, snapshot the .ui layer from the persistent sim context, and
/// reconcile the result into the node store, emitting an id-keyed delta when
/// it changed.
pub fn tick() {
    if let Err(e) = tick_inner() {
        runtime_log!("ui: tick failed: {:?}", e);
    }
}

fn tick_inner() -> Result<()> {
    let Some(ctx) = crate::js_executor::sim_ctx::ctx() else {
        return Ok(());
    };
    // The static nodes were declared once at install time; only the
    // container-list materialization is per-tick. Expansion is destructive
    // (markers are replaced by item ids), so reset first — the reset drops
    // the last tick's items and restores each list's marker. The entity
    // lookup (`__uiEntitiesFor`) is stable from install; only the container
    // list it reads is refreshed here.
    let containers_json = container_entities_json()?;
    let sector_grids_json = crate::state::fetch_sector_grids_json();
    let script = format!(
        "__uiHost.resetContainers();\n\
         globalThis.__uiContainerList = {};\n\
         globalThis.__uiSectorGrids = {};\n\
         __uiHost.expandContainers(globalThis.__uiEntitiesFor);\n\
         __uiHost.expandContainerViews(globalThis.__uiEntitiesFor);\n\
         __uiHost.expandSectorGrids();\n\
         JSON.stringify(__uiHost.snapshot())",
        containers_json, sector_grids_json
    );
    let snapshot_json = eval_string(&ctx, &script)?;
    let mut snapshot: Vec<UiNode> = serde_json::from_str(&snapshot_json)?;
    resolve_field_values(&mut snapshot);
    resolve_progress_values(&mut snapshot);
    resolve_container_view_positions(&mut snapshot);

    // Keep the animation-definition store in sync for the fetch FFI (the
    // module's setAnimation calls filled __registeredAnimations at
    // install time).
    match eval_string(
        &ctx,
        "JSON.stringify(globalThis.__registeredAnimations || {})",
    ) {
        Ok(anim_json) => {
            if let Ok(map) = serde_json::from_str::<
                HashMap<String, serde_json::Value>
            >(&anim_json)
            {
                *animations().lock().unwrap() = map;
            }
        }
        Err(_) => {}
    }

    apply_diff(&snapshot)
}

/// The current runtime containers as a JS array literal of
/// `{ id, entities: [...] }` objects, for the in-context entity lookup.
fn container_entities_json() -> Result<String> {
    let containers = crate::state::last_containers().lock().unwrap().clone();
    let mut out: Vec<serde_json::Value> = Vec::new();
    for json_str in containers.iter() {
        if let Ok(v) = serde_json::from_str::<serde_json::Value>(json_str) {
            if let Some(id) = v.get("id").and_then(|x| x.as_str()) {
                let entities = v.get("entities")
                    .and_then(|e| e.as_array())
                    .map(|arr| arr.iter().map(|e| e.clone()).collect::<Vec<_>>())
                    .unwrap_or_default();
                out.push(serde_json::json!({ "id": id, "entities": entities }));
            }
        }
    }
    Ok(serde_json::to_string(&out)?)
}

/// Re-resolve every field node's `value` from the current entity store so
/// the id-diff sees live changes as updates (the node id is stable).
fn resolve_field_values(snapshot: &mut [UiNode]) {
    let text_data = crate::state::last_entity_data().lock().unwrap();
    let num_data = crate::state::last_entity_number_data().lock().unwrap();
    for node in snapshot.iter_mut() {
        if let UiNode::Field { binding, value, .. } = node {
            let (entity, map, name, fallback) = (
                binding.get("entity").and_then(|v| v.as_str()),
                binding.get("map").and_then(|v| v.as_str()),
                binding.get("name").and_then(|v| v.as_str()),
                binding.get("fallback").and_then(|v| v.as_str()).unwrap_or(""),
            );
            if let (Some(entity), Some(map), Some(name)) = (entity, map, name) {
                let mut resolved: Option<String> = None;
                match map {
                    "text" => {
                        if let Some(s) = text_data.get(entity)
                            .and_then(|m| m.get(name))
                        {
                            resolved = Some(s.clone());
                        }
                    }
                    "number" => {
                        if let Some(n) = num_data.get(entity)
                            .and_then(|m| m.get(name))
                        {
                            resolved = Some(n.to_string());
                        }
                    }
                    _ => {}
                }
                match resolved {
                    Some(v) if !v.is_empty() => *value = v,
                    _ => *value = fallback.to_string(),
                }
            } else {
                *value = fallback.to_string();
            }
        }
    }
}

/// Re-resolve every progress node's `value` from the current entity store so
/// the id-diff sees live changes as updates. A bound node reads its entity's
/// number field (`name`) and stores the fill quantized into `steps` (default
/// 100): `round(((value - min) / (max - min)).clamp(0, 1) * steps)` — an
/// integer 0..steps. Each of `name`/`min`/`max` is a field key re-read from
/// the entity's number data every tick; when a key is absent on the entity the
/// binding's captured `.orElse` default (`fallback`/`minFallback`/`maxFallback`)
/// is used. An omitted `min` falls back to the static default 0.
fn resolve_progress_values(snapshot: &mut [UiNode]) {
    let num_data = crate::state::last_entity_number_data().lock().unwrap();
    for node in snapshot.iter_mut() {
        let UiNode::Progress { value, binding, options, .. } = node else {
            continue;
        };
        let Some(b) = binding.as_object() else {
            continue;
        };
        let (entity, name) = (
            b.get("entity").and_then(|v| v.as_str()),
            b.get("name").and_then(|v| v.as_str()),
        );
        let Some(entity) = entity else {
            continue; // unbound: keep the declared value
        };
        // Each bound field is re-read from the entity's number data; a missing
        // key falls back to the captured .orElse default (min omits to a
        // static 0).
        let num = |key: Option<&str>, default: f64| -> Option<f64> {
            match key {
                Some(k) => num_data.get(entity).and_then(|m| m.get(k)).copied().or(Some(default)),
                None => Some(default),
            }
        };
        let name_default = b.get("fallback").and_then(|v| v.as_f64()).unwrap_or(0.0);
        let value_raw = num(name, name_default);
        let min_default = b.get("minFallback").and_then(|v| v.as_f64()).unwrap_or(0.0);
        let min = num(b.get("minName").and_then(|v| v.as_str()), min_default);
        let max_default = b.get("maxFallback").and_then(|v| v.as_f64()).unwrap_or(1.0);
        let max = num(b.get("maxName").and_then(|v| v.as_str()), max_default);
        let (value_raw, min, max) = match (value_raw, min, max) {
            (Some(v), Some(mi), Some(ma)) => (v, mi, ma),
            _ => continue,
        };
        let span = max - min;
        let fraction = if span > 0.0 { ((value_raw - min) / span).clamp(0.0, 1.0) } else { 0.0 };
        // Quantize the fraction into an integer number of steps (default 100);
        // the renderer divides by steps to recover the fill fraction.
        let steps = options.get("steps").and_then(|v| v.as_f64()).unwrap_or(100.0).max(1.0);
        *value = (fraction * steps).round();
    }
}

/// A per-area outline style parsed from a container-view's `area` option
/// entry. Every field is required: the named area to draw plus its outline
/// line color, body fill color, and outline thickness.
struct AreaStyle {
    name: String,
    color: [f64; 4],
    body_color: [f64; 4],
    thickness: f64,
}

/// Read an `rgba` JSON array `[r, g, b, a]` into a `[f64; 4]`, defaulting any
/// missing/non-numeric component to 1.0.
fn rgba(v: Option<&serde_json::Value>) -> [f64; 4] {
    let mut out = [1.0f64; 4];
    if let Some(arr) = v.and_then(|x| x.as_array()) {
        for (i, c) in arr.iter().take(4).enumerate() {
            if let Some(n) = c.as_f64() {
                out[i] = n;
            }
        }
    }
    out
}

/// Re-resolve every container-view item's geometry (x/y/width/height) from
/// its entity's container position, so the id-diff sees live moves as updates
/// and the C# `ApplyWindow` re-positions the panel. A view node carries
/// `options.container`, `options.viewWidth`, `options.viewHeight`; each item
/// child carries `options.entity` (stamped by the JS expansion). The cell
/// size is `view / container.size`, and the item sits at
/// `(getX * cellW, getY * cellH)` with span `(getSpanX * cellW, getSpanY * cellH)`.
fn resolve_container_view_positions(snapshot: &mut [UiNode]) {
    // Build a container-geometry map: id -> (sizeX, sizeY, per-entity
    // getX/getY/getSpanX/getSpanY). The latest row for an id wins (mirrors
    // the JS `__uiEntitiesFor` lookup).
    let containers = crate::state::last_containers().lock().unwrap().clone();
    // A view (Vec, not a map) so the immutable borrow ends before the
    // mutable item writes below. The latest row for an id wins.
    let mut geom: Vec<
        (String, Option<f64>, Option<f64>, bool, serde_json::Map<String, serde_json::Value>),
    > = Vec::new();
    for json_str in containers.iter() {
        let Ok(v) = serde_json::from_str::<serde_json::Value>(json_str) else {
            continue;
        };
        let Some(id) = v.get("id").and_then(|x| x.as_str()) else {
            continue;
        };
        let size_x = v.get("sizeX").and_then(|s| s.get("value")).and_then(|n| n.as_f64());
        let size_y = v.get("sizeY").and_then(|s| s.get("value")).and_then(|n| n.as_f64());
        // `alignment` (optional) selects how an entity's marker is placed over
        // its span: "center" centers the marker on the span midpoint, while
        // the default "top-left" pins the marker's top-left corner to (x, y).
        let center = v.get("alignment").and_then(|a| a.as_str()) == Some("center");
        let prev = geom.iter().find(|g| g.0 == id).map(|g| g.4.clone());
        let per_entity = merge_position_maps(&prev, &v);
        geom.retain(|g| g.0 != id);
        geom.push((id.to_string(), size_x, size_y, center, per_entity));
    }

    // First pass (immutable): collect each view's (container id, cell size,
    // center flag, item child ids). The cell size is precomputed so the
    // second pass owns all the data it needs and never borrows `geom` or the
    // snapshot.
    let mut views: Vec<
        (String, f64, f64, bool, Vec<String>, Vec<AreaStyle>),
    > = Vec::new();
    for node in snapshot.iter() {
        let UiNode::Window { options, children, .. } = node else {
            continue;
        };
        let Some(opts_obj) = options.as_object() else {
            continue;
        };
        let Some(cid) = opts_obj.get("container").and_then(|c| c.as_str()) else {
            continue;
        };
        let Some(view_w) = opts_obj.get("viewWidth").and_then(|n| n.as_f64()) else {
            continue;
        };
        let Some(view_h) = opts_obj.get("viewHeight").and_then(|n| n.as_f64()) else {
            continue;
        };
        // The container must declare its extent; a view without bounds cannot
        // derive a cell size.
        let (sx, sy, center) = match geom.iter().find(|g| g.0 == cid) {
            Some((_, Some(a), Some(b), c, _)) => (*a, *b, *c),
            _ => continue,
        };
        // `area` (optional) selects which of each entity's named areas the
        // outline renders and the outline style for each. It is an array of
        // entries, every field required:
        //   { name, color: [r,g,b,a], bodyColor: [r,g,b,a], thickness: number }
        // When omitted, the entity's first declared area (id ascending) is
        // drawn with the renderer's default red/green/thickness.
        let mut area_styles: Vec<AreaStyle> = Vec::new();
        if let Some(arr) = opts_obj.get("area").and_then(|a| a.as_array()) {
            for entry in arr {
                let Some(obj) = entry.as_object() else {
                    continue;
                };
                let Some(name) = obj.get("name").and_then(|v| v.as_str()) else {
                    continue;
                };
                let color = rgba(obj.get("color"));
                let body_color = rgba(obj.get("bodyColor"));
                let thickness = obj.get("thickness").and_then(|v| v.as_f64()).unwrap_or(0.0);
                area_styles.push(AreaStyle {
                    name: name.to_string(),
                    color,
                    body_color,
                    thickness,
                });
            }
        }
        views.push((
            cid.to_string(),
            view_w / sx,
            view_h / sy,
            center,
            children.clone(),
            area_styles,
        ));
    }

    // Second pass (mutable): index snapshot positions by id (owned keys),
    // then write each item's cell geometry via `get_mut(position)`.
    let mut index: std::collections::HashMap<String, usize> = std::collections::HashMap::new();
    for (i, node) in snapshot.iter().enumerate() {
        index.insert(node.id().to_string(), i);
    }

    for (cid, cell_w, cell_h, center, children, area_styles) in views {
        let Some(per_entity) = geom
            .iter()
            .find(|g| g.0 == cid)
            .map(|g| g.4.clone())
        else {
            continue;
        };
        for child_id in children {
            let Some(pos) = index.get(child_id.as_str()).copied() else {
                continue;
            };
            let Some(item) = snapshot.get_mut(pos) else {
                continue;
            };
            let UiNode::Window { options: item_opts, .. } = item else {
                continue;
            };
            let Some(item_obj) = item_opts.as_object_mut() else {
                continue;
            };
            let Some(entity) = item_obj.get("entity").and_then(|e| e.as_str())
                .map(|s| s.to_string())
            else {
                continue;
            };
            let Some(ent_pos) = per_entity.get(entity.as_str()).and_then(|p| p.as_object()) else {
                continue;
            };
            let get = |key: &str| ent_pos.get(key).and_then(|v| v.as_f64()).unwrap_or(0.0);
            let span_x = get("spanX").max(1.0) * cell_w;
            let span_y = get("spanY").max(1.0) * cell_h;
            // With `alignment: "center"`, the marker is centered on the span
            // midpoint: the declared (x, y) is the top-left, so shift the
            // marker by half its span. Otherwise (the "top-left" default), the
            // marker pins its top-left corner to (x, y).
            let (ox, oy) = if center {
                (span_x / 2.0, span_y / 2.0)
            } else {
                (0.0, 0.0)
            };
            let x = get("x") * cell_w - ox;
            let y = get("y") * cell_h - oy;
            // Position/size/area live in `options.panel` (the canonical node
            // shape); `entity` stays top-level metadata on the item.
            if !item_obj.contains_key("panel") {
                item_obj.insert("panel".into(), serde_json::json!({}));
            }
            let panel = item_obj.get_mut("panel").unwrap().as_object_mut().unwrap();
            panel.insert("x".into(), num_json(x));
            panel.insert("y".into(), num_json(y));
            panel.insert("width".into(), num_json(span_x));
            panel.insert("height".into(), num_json(span_y));
            // Stamp the entity's area polygons into the item's `panel` options
            // so the C# renderer can draw them over the sprite. Each polygon is
            // in view-local logical units (same space as x/y/width/height): the
            // entity's world position is (getX, getY), so view-local is
            // (getX * cell_w, getY * cell_h) relative to the view origin. The
            // view's `area` selects which named areas to draw and the outline
            // style for each (first declared area with defaults when omitted).
            stamp_area_outline(
                panel,
                &entity,
                get("x"),
                get("y"),
                cell_w,
                cell_h,
                ox,
                oy,
                &area_styles,
            );
        }
    }
}

/// Stamp the entity's declared areas (translated to view-local logical units)
/// into the item's `options.panel.areaOutline`. `area_styles` selects which named
/// areas to draw and the outline style for each (every field required on each
/// entry). When `area_styles` is empty (the view omitted `area`), the
/// entity's first declared area (id ascending) is drawn with the renderer's
/// default red/green/thickness. The result is a list of polygons, each with
/// its own `points`, `color`, `bodyColor` and `thickness`:
///   { polygons: [ { points, color, bodyColor, thickness }, ... ] }
/// An entity with no matching area gets a 1x1 pixel/rectangle fallback polygon
/// at its view-local position. `cell_w`/`cell_h` are the per-unit view scale;
/// `ox`/`oy` are the alignment offsets already applied to x/y so each polygon
/// tracks the marker.
fn stamp_area_outline(
    panel: &mut serde_json::Map<String, serde_json::Value>,
    entity: &str,
    ent_x: f64,
    ent_y: f64,
    cell_w: f64,
    cell_h: f64,
    ox: f64,
    oy: f64,
    area_styles: &[AreaStyle],
) {
    // The entity's view-local top-left (matches the x/y stamp above).
    let vx = ent_x * cell_w - ox;
    let vy = ent_y * cell_h - oy;

    // The per-area styles to render (owned names so the borrow of
    // `area_styles` ends before the mutable `panel` writes below). When the
    // view omitted `area`, draw the entity's first declared area (id
    // ascending) with default styling.
    let styled: Vec<(String, [f64; 4], [f64; 4], f64)> = if area_styles.is_empty() {
        let first = crate::state::area_query::entity_area_ids(entity).into_iter().next();
        match first {
            Some(name) => vec![(name, [1.0, 0.0, 0.0, 1.0], [0.0, 1.0, 0.0, 1.0], 2.0)],
            None => Vec::new(),
        }
    } else {
        area_styles
            .iter()
            .map(|s| (s.name.clone(), s.color, s.body_color, s.thickness))
            .collect()
    };

    let fallback_points = || {
        // No area: a 1x1 pixel/rectangle fallback at the view-local origin.
        vec![
            serde_json::json!([vx, vy]),
            serde_json::json!([vx + 1.0, vy]),
            serde_json::json!([vx + 1.0, vy + 1.0]),
            serde_json::json!([vx, vy + 1.0]),
        ]
    };

    let mut polys: Vec<serde_json::Value> = Vec::new();
    for (name, color, body_color, thickness) in &styled {
        let points = match crate::state::area_query::entity_area(entity, name) {
            Some(poly) => poly
                .into_iter()
                .map(|(px, py)| {
                    serde_json::json!([
                        (px * cell_w + vx) as f64,
                        (py * cell_h + vy) as f64,
                    ])
                })
                .collect(),
            None => fallback_points(),
        };
        polys.push(serde_json::json!({
            "points": points,
            "color": [color[0], color[1], color[2], color[3]],
            "bodyColor": [body_color[0], body_color[1], body_color[2], body_color[3]],
            "thickness": thickness,
        }));
    }

    if polys.is_empty() {
        // The entity declares no areas at all: a single 1x1 fallback polygon.
        polys.push(serde_json::json!({
            "points": fallback_points(),
            "color": [1.0, 0.0, 0.0, 1.0],
            "bodyColor": [0.0, 1.0, 0.0, 1.0],
            "thickness": 2.0,
        }));
    }

    let outline = serde_json::json!({ "polygons": polys });
    panel.insert("areaOutline".into(), outline);
}

/// Merge a container row's per-entity position maps (getX/getY/getSpanX/
/// getSpanY, each `entityId -> number`) into one `entityId -> { x, y, spanX,
/// spanY }` map, layered over any earlier row for the same container id.
fn merge_position_maps(
    prev: &Option<serde_json::Map<String, serde_json::Value>>,
    v: &serde_json::Value,
) -> serde_json::Map<String, serde_json::Value> {
    let mut out = match prev {
        Some(m) => m.clone(),
        None => serde_json::Map::new(),
    };
    let v_obj = match v.as_object() {
        Some(o) => o,
        None => return out,
    };
    let copy_map = |key: &str| -> std::collections::HashMap<String, f64> {
        let mut m = std::collections::HashMap::new();
        if let Some(obj) = v_obj.get(key).and_then(|x| x.as_object()) {
            for (k, val) in obj {
                if let Some(n) = val.as_f64() {
                    m.insert(k.clone(), n);
                }
            }
        }
        m
    };
    let get_x = copy_map("getX");
    let get_y = copy_map("getY");
    let span_x = copy_map("getSpanX");
    let span_y = copy_map("getSpanY");
    let mut ids: std::collections::HashSet<&str> = std::collections::HashSet::new();
    for m in [&get_x, &get_y, &span_x, &span_y] {
        for k in m.keys() {
            ids.insert(k.as_str());
        }
    }
    for id in ids {
        let entry = out.entry(id.to_string()).or_insert_with(|| {
            serde_json::json!({ "x": 0.0, "y": 0.0, "spanX": 1.0, "spanY": 1.0 })
        });
        let Some(obj) = entry.as_object_mut() else {
            continue;
        };
        if let Some(n) = get_x.get(id) {
            obj.insert("x".into(), num_json(*n));
        }
        if let Some(n) = get_y.get(id) {
            obj.insert("y".into(), num_json(*n));
        }
        if let Some(n) = span_x.get(id) {
            obj.insert("spanX".into(), num_json(*n));
        }
        if let Some(n) = span_y.get(id) {
            obj.insert("spanY".into(), num_json(*n));
        }
    }
    out
}

fn num_json(n: f64) -> serde_json::Value {
    serde_json::Number::from_f64(n)
        .map(serde_json::Value::Number)
        .unwrap_or(serde_json::Value::Null)
}

fn apply_diff(snapshot: &[UiNode]) -> Result<()> {
    let mut store = ui_nodes().lock().unwrap();
    let mut ops: Vec<UiDeltaOp> = Vec::new();
    let mut seen = std::collections::HashSet::new();
    for node in snapshot.iter() {
        seen.insert(node.id().to_string());
        match store.iter_mut().find(|n| n.id() == node.id()) {
            Some(existing) => {
                if existing != node {
                    *existing = node.clone();
                    ops.push(UiDeltaOp::Update { node: node.clone() });
                }
            }
            None => {
                store.push(node.clone());
                ops.push(UiDeltaOp::Add { node: node.clone() });
            }
        }
    }
    store.retain(|n| seen.contains(n.id()));
    let removed: Vec<String> = prev_store_ids().lock().unwrap().iter()
        .filter(|id| !seen.contains(id.as_str()))
        .cloned()
        .collect();
    for id in removed {
        ops.push(UiDeltaOp::Remove { id });
    }
    *prev_store_ids().lock().unwrap() =
        store.iter().map(|n| n.id().to_string()).collect();

    if !ops.is_empty() {
        *ui_delta().lock().unwrap() = Some(UiDelta { ops });
        ui_dirty().store(true, Ordering::SeqCst);
    }
    Ok(())
}

// ---------------------------------------------------------------------------
// Fetch API (consumed by the FFI seam)
// ---------------------------------------------------------------------------

/// Full UI tree as JSON: `{"nodes":[...],"moduleOwners":{...},
/// "animations":{...}}`. Animations are the module-registered animation
/// definitions (name -> {frames:[{sprite}], ...}) that object backgrounds
/// reference by name.
pub fn fetch_ui_state_json() -> String {
    let nodes = ui_nodes().lock().unwrap().clone();
    let owners = module_owners().lock().unwrap().clone();
    let animations = animations().lock().unwrap().clone();
    serde_json::json!({ "nodes": nodes, "moduleOwners": owners,
        "animations": animations })
        .to_string()
}

/// Pending delta as JSON, if any (does not clear the dirty flag).
pub fn fetch_ui_delta_json() -> Option<String> {
    ui_delta().lock().unwrap()
        .as_ref()
        .map(|d| serde_json::to_string(d).unwrap_or_default())
}

#[cfg(test)]
mod tests {
    use super::*;

    // All UI state is process-global; serialize via the shared state lock.
    fn lock() -> std::sync::MutexGuard<'static, ()> {
        crate::state::test_lock()
    }

    fn node_json(json: &str) -> UiNode {
        serde_json::from_str(json).unwrap()
    }

    /// Install `source` as the module entry in the persistent sim context
    /// (the live UI declaration path).
    fn install_module(source: &str) {
        let mut files = HashMap::new();
        files.insert("index.js".to_string(), source.to_string());
        crate::js_executor::sim_ctx::install(&files).unwrap();
    }

    const SPINE_MODULE: &str = r#"
export default (hostApi) => {
  hostApi.ui.window('spine-win', {}, [
    hostApi.ui.div('spine-div', {}, [
      hostApi.ui.text('spine-text', 'spine')
    ])
  ]);
};
"#;

    #[test]
    fn spine_div_and_text_flow_into_ui_state() {
        let _g = lock();
        crate::state::clear_state();
        install_module(SPINE_MODULE);
        tick();

        let json = fetch_ui_state_json();
        let v: serde_json::Value = serde_json::from_str(&json).unwrap();
        let nodes = v["nodes"].as_array().unwrap();
        assert_eq!(nodes.len(), 3);
        let win = nodes.iter().find(|n| n["id"] == "spine-win").unwrap();
        assert_eq!(win["kind"], "window");
        assert_eq!(win["children"][0], "spine-div");
        let div = nodes.iter().find(|n| n["id"] == "spine-div").unwrap();
        assert_eq!(div["kind"], "division");
        assert_eq!(div["children"][0], "spine-text");
        let text = nodes.iter().find(|n| n["id"] == "spine-text").unwrap();
        assert_eq!(text["kind"], "text");
        assert_eq!(text["value"], "spine");

        let delta: serde_json::Value = serde_json::from_str(
            &fetch_ui_delta_json().unwrap()).unwrap();
        let adds: Vec<&str> = delta["ops"].as_array().unwrap().iter()
            .filter(|o| o["op"] == "add")
            .map(|o| o["node"]["id"].as_str().unwrap())
            .collect();
        assert_eq!(adds.len(), 3);
        assert!(adds.contains(&"spine-win"));
        assert!(adds.contains(&"spine-div"));
        assert!(adds.contains(&"spine-text"));
    }

    const STANDALONE_MODULE: &str = r#"
export default (hostApi) => {
  // Standalone nodes: no window reaches them, so they are a no-op (never
  // added to the view), including their subtrees.
  hostApi.ui.text('orphan-text', 'orphan');
  hostApi.ui.div('orphan-div', {}, [
    hostApi.ui.text('orphan-nested', 'x')
  ]);
  // A window and its content make it to the view.
  hostApi.ui.window('real-win', {}, [
    hostApi.ui.text('win-text', 'in')
  ]);
};
"#;

    #[test]
    fn standalone_nodes_are_not_added_to_the_view() {
        let _g = lock();
        crate::state::clear_state();
        install_module(STANDALONE_MODULE);
        tick();

        let v: serde_json::Value =
            serde_json::from_str(&fetch_ui_state_json()).unwrap();
        let ids: Vec<&str> = v["nodes"].as_array().unwrap().iter()
            .map(|n| n["id"].as_str().unwrap()).collect();
        assert!(!ids.contains(&"orphan-text"),
            "standalone text is a no-op: {:?}", ids);
        assert!(!ids.contains(&"orphan-div"),
            "standalone division is a no-op: {:?}", ids);
        assert!(!ids.contains(&"orphan-nested"),
            "subtree of a standalone node is a no-op: {:?}", ids);
        assert!(ids.contains(&"real-win"), "window missing: {:?}", ids);
        assert!(ids.contains(&"win-text"),
            "window content missing: {:?}", ids);
    }

    const SCREEN_MODULE: &str = r#"
export default (hostApi) => {
  hostApi.ui.setScreen({ screen: { background: 'art/screen.png' } }, [
    hostApi.ui.text('screen-text', 'game')
  ]);
};
"#;

    #[test]
    fn screen_node_flows_into_ui_state_as_top_level_surface() {
        let _g = lock();
        crate::state::clear_state();
        install_module(SCREEN_MODULE);
        tick();

        let v: serde_json::Value =
            serde_json::from_str(&fetch_ui_state_json()).unwrap();
        let nodes = v["nodes"].as_array().unwrap();
        let screen = nodes
            .iter()
            .find(|n| n["kind"] == "screen")
            .expect("screen node missing");
        assert!(screen["id"].as_str().unwrap().starts_with("__ui-screen-"));
        assert_eq!(screen["options"]["screen"]["background"], "art/screen.png");
        assert!(screen["children"].as_array().unwrap().iter().any(|c| c == "screen-text"));
        // The screen's children reach the view (the screen is a top-level
        // surface, like a window).
        let text = nodes
            .iter()
            .find(|n| n["id"] == "screen-text")
            .expect("screen child missing");
        assert_eq!(text["value"], "game");
        let delta: serde_json::Value = serde_json::from_str(
            &fetch_ui_delta_json().unwrap()).unwrap();
        assert!(delta["ops"].as_array().unwrap().iter().any(|o|
            o["op"] == "add" && o["node"]["kind"] == "screen"));
    }

    #[test]
    fn one_iteration_keeps_spine_nodes_visible() {
        let _g = lock();
        crate::state::clear_state();
        install_module(SPINE_MODULE);
        crate::ffi_mod::runtime_run_iteration(1);
        tick();

        let v: serde_json::Value = serde_json::from_str(&fetch_ui_state_json()).unwrap();
        let ids: Vec<&str> = v["nodes"].as_array().unwrap().iter()
            .map(|n| n["id"].as_str().unwrap()).collect();
        assert!(ids.contains(&"spine-div"));
        assert!(ids.contains(&"spine-text"));
    }

    #[test]
    fn diff_produces_add_update_remove() {
        let _g = lock();
        ui_nodes().lock().unwrap().clear();
        prev_store_ids().lock().unwrap().clear();
        ui_delta().lock().unwrap().take();
        let a = node_json(
            r#"{"kind":"division","id":"a","options":{},"children":[]}"#);
        let b1 = node_json(
            r#"{"kind":"text","id":"b","value":"one","children":[]}"#);
        apply_diff(&[a.clone(), b1.clone()]).unwrap();
        let delta = ui_delta().lock().unwrap().take().unwrap();
        assert_eq!(delta.ops.len(), 2);
        assert!(matches!(delta.ops[0], UiDeltaOp::Add { .. }));
        assert!(matches!(delta.ops[1], UiDeltaOp::Add { .. }));

        // update b, remove a
        let b2 = node_json(
            r#"{"kind":"text","id":"b","value":"two","children":[]}"#);
        apply_diff(&[b2.clone()]).unwrap();
        let delta = ui_delta().lock().unwrap().take().unwrap();
        assert_eq!(delta.ops.len(), 2);
        assert!(matches!(&delta.ops[0], UiDeltaOp::Update { .. }));
        assert!(matches!(&delta.ops[1], UiDeltaOp::Remove { id }
            if id == "a"));

        // unchanged -> no delta
        apply_diff(&[b2]).unwrap();
        assert!(ui_delta().lock().unwrap().is_none());
    }

    fn set_entity_number(entity: &str, name: &str, value: f64) {
        let mut data = crate::state::last_entity_number_data().lock().unwrap();
        data.entry(entity.to_string())
            .or_insert_with(HashMap::new)
            .insert(name.to_string(), value);
    }

    const FIELD_MODULE: &str = r#"
export default (hostApi) => {
  hostApi.ui.window('field-win', {}, [
    hostApi.ui.field('hp-field', {
      entity: 'ent-a',
      map: 'number',
      name: 'hp',
      fallback: 'n/a'
    })
  ]);
};
"#;

    #[test]
    fn field_binds_entity_value_and_updates_live() {
        let _g = lock();
        crate::state::clear_state();
        install_module(FIELD_MODULE);
        set_entity_number("ent-a", "hp", 7.0);
        tick();

        let v: serde_json::Value = serde_json::from_str(&fetch_ui_state_json()).unwrap();
        let nodes = v["nodes"].as_array().unwrap();
        let field = nodes.iter().find(|n| n["id"] == "hp-field").unwrap();
        assert_eq!(field["kind"], "field");
        assert_eq!(field["value"], "7");
        assert_eq!(field["binding"]["entity"], "ent-a");
        assert_eq!(field["binding"]["map"], "number");
        assert_eq!(field["binding"]["name"], "hp");
        let delta: serde_json::Value = serde_json::from_str(
            &fetch_ui_delta_json().unwrap()).unwrap();
        assert!(delta["ops"].as_array().unwrap().iter().any(|o|
            o["op"] == "add" && o["node"]["id"] == "hp-field"));

        // mutate the entity value; the next tick must produce an update, not remove+add
        ui_delta().lock().unwrap().take();
        ui_dirty().store(false, Ordering::SeqCst);
        set_entity_number("ent-a", "hp", 42.0);
        tick();

        let v: serde_json::Value = serde_json::from_str(&fetch_ui_state_json()).unwrap();
        let field = v["nodes"].as_array().unwrap().iter()
            .find(|n| n["id"] == "hp-field").unwrap();
        assert_eq!(field["value"], "42");
        let delta: serde_json::Value = serde_json::from_str(
            &fetch_ui_delta_json().unwrap()).unwrap();
        let ops = delta["ops"].as_array().unwrap();
        assert!(ops.iter().any(|o| o["op"] == "update"
            && o["node"]["id"] == "hp-field"
            && o["node"]["value"] == "42"));
        assert!(!ops.iter().any(|o| o["op"] == "remove"));
    }

    #[test]
    fn fetch_ui_state_round_trips_nodes() {
        let _g = lock();
        ui_nodes().lock().unwrap().clear();
        prev_store_ids().lock().unwrap().clear();
        apply_diff(&[
            node_json(
                r#"{"kind":"division","id":"d","options":{"panel":{"layout":"column"}},"children":["t"]}"#),
            node_json(
                r#"{"kind":"text","id":"t","value":"spine","children":[]}"#),
        ]).unwrap();
        let json = fetch_ui_state_json();
        let v: serde_json::Value = serde_json::from_str(&json).unwrap();
        let nodes = v["nodes"].as_array().unwrap();
        assert_eq!(nodes.len(), 2);
        assert_eq!(nodes[0]["id"], "d");
        assert_eq!(nodes[1]["value"], "spine");
    }

    #[test]
    fn window_node_with_options_round_trips_store_and_delta() {
        let _g = lock();
        ui_nodes().lock().unwrap().clear();
        prev_store_ids().lock().unwrap().clear();
        ui_delta().lock().unwrap().take();
        let win = node_json(
            r#"{"kind":"window","id":"win-a","options":{"panel":{"x":10,"y":-20,"anchor":"top-left","align":"top-left","width":200,"height":100}},"children":["t"]}"#);
        assert!(matches!(&win, UiNode::Window { id, .. } if id == "win-a"));
        let text = node_json(
            r#"{"kind":"text","id":"t","value":"hi","children":[]}"#);
        apply_diff(&[win.clone(), text.clone()]).unwrap();

        // the add op carries the full options
        let delta = ui_delta().lock().unwrap().take().unwrap();
        let add = delta.ops.iter().find(|o| matches!(o, UiDeltaOp::Add { node }
            if node.id() == "win-a")).expect("add op for window");
        match add {
            UiDeltaOp::Add { node } => match node {
                UiNode::Window { id, options, children } => {
                    assert_eq!(id, "win-a");
                    assert_eq!(options["panel"]["x"], 10);
                    assert_eq!(options["panel"]["y"], -20);
                    assert_eq!(options["panel"]["anchor"], "top-left");
                    assert_eq!(options["panel"]["align"], "top-left");
                    assert_eq!(options["panel"]["width"], 200);
                    assert_eq!(options["panel"]["height"], 100);
                    assert_eq!(children, &vec!["t".to_string()]);
                }
                other => panic!("expected window node, got {:?}", other),
            },
            other => panic!("expected add, got {:?}", other),
        }

        // the store round-trips the node with options intact
        let json = fetch_ui_state_json();
        let v: serde_json::Value = serde_json::from_str(&json).unwrap();
        let win_el = v["nodes"].as_array().unwrap().iter()
            .find(|n| n["id"] == "win-a").unwrap();
        assert_eq!(win_el["kind"], "window");
        assert_eq!(win_el["options"]["panel"]["anchor"], "top-left");
        assert_eq!(win_el["options"]["panel"]["align"], "top-left");
        assert_eq!(win_el["options"]["panel"]["x"], 10);
        assert_eq!(win_el["options"]["panel"]["y"], -20);
        assert_eq!(win_el["options"]["panel"]["width"], 200);
        assert_eq!(win_el["options"]["panel"]["height"], 100);
        assert_eq!(win_el["children"][0], "t");
    }

    #[test]
    fn div_options_carry_onclick_and_onhover() {
        let _g = lock();
        ui_nodes().lock().unwrap().clear();
        prev_store_ids().lock().unwrap().clear();
        ui_delta().lock().unwrap().take();
        let div = node_json(
            r#"{"kind":"division","id":"btn","options":{"panel":{"layout":"column","onClick":"do-thing","onHover":{"background":"hover.png","emitAction":"btn-hover"}}},"children":[]}"#);
        apply_diff(&[div]).unwrap();

        let json = fetch_ui_state_json();
        let v: serde_json::Value = serde_json::from_str(&json).unwrap();
        let el = v["nodes"].as_array().unwrap().iter()
            .find(|n| n["id"] == "btn").unwrap();
        assert_eq!(el["options"]["panel"]["onClick"], "do-thing");
        assert_eq!(el["options"]["panel"]["onHover"]["background"], "hover.png");
        assert_eq!(el["options"]["panel"]["onHover"]["emitAction"], "btn-hover");
    }

    #[test]
    fn image_node_round_trips_store_and_delta_with_src() {
        let _g = lock();
        ui_nodes().lock().unwrap().clear();
        prev_store_ids().lock().unwrap().clear();
        ui_delta().lock().unwrap().take();
        let img = node_json(
            r#"{"kind":"image","id":"img-a","src":"art/hover.png","children":[]}"#);
        assert!(matches!(&img, UiNode::Image { id, src, .. }
            if id == "img-a" && src == "art/hover.png"));
        apply_diff(&[img.clone()]).unwrap();

        // the add op carries the full node with src
        let delta = ui_delta().lock().unwrap().take().unwrap();
        let add = delta.ops.iter().find(|o| matches!(o, UiDeltaOp::Add { node }
            if node.id() == "img-a")).expect("add op for image");
        match add {
            UiDeltaOp::Add { node } => match node {
                UiNode::Image { id, src, .. } => {
                    assert_eq!(id, "img-a");
                    assert_eq!(src, "art/hover.png");
                }
                other => panic!("expected image node, got {:?}", other),
            },
            other => panic!("expected add, got {:?}", other),
        }

        // the store round-trips the node with src intact
        let json = fetch_ui_state_json();
        let v: serde_json::Value = serde_json::from_str(&json).unwrap();
        let img_el = v["nodes"].as_array().unwrap().iter()
            .find(|n| n["id"] == "img-a").unwrap();
        assert_eq!(img_el["kind"], "image");
        assert_eq!(img_el["src"], "art/hover.png");
        assert_eq!(img_el["children"].as_array().unwrap().len(), 0);
    }

    #[test]
    fn progress_node_round_trips_store_and_delta() {
        let _g = lock();
        ui_nodes().lock().unwrap().clear();
        prev_store_ids().lock().unwrap().clear();
        ui_delta().lock().unwrap().take();
        let prog = node_json(
            r#"{"kind":"progress","id":"prog-a","value":0.75,"binding":{},"options":{"fill":"art/fill.png","track":"art/track.png","orientation":"vertical","max":4},"children":[]}"#);
        assert!(matches!(&prog, UiNode::Progress { id, value, .. }
            if id == "prog-a" && *value == 0.75));
        apply_diff(&[prog.clone()]).unwrap();

        // the add op carries the full node with options
        let delta = ui_delta().lock().unwrap().take().unwrap();
        let add = delta.ops.iter().find(|o| matches!(o, UiDeltaOp::Add { node }
            if node.id() == "prog-a")).expect("add op for progress");
        match add {
            UiDeltaOp::Add { node } => match node {
                UiNode::Progress { id, value, options, .. } => {
                    assert_eq!(id, "prog-a");
                    assert_eq!(*value, 0.75);
                    assert_eq!(options["fill"], "art/fill.png");
                    assert_eq!(options["track"], "art/track.png");
                    assert_eq!(options["orientation"], "vertical");
                    assert_eq!(options["max"], 4);
                }
                other => panic!("expected progress node, got {:?}", other),
            },
            other => panic!("expected add, got {:?}", other),
        }

        // the store round-trips the node with options intact
        let json = fetch_ui_state_json();
        let v: serde_json::Value = serde_json::from_str(&json).unwrap();
        let prog_el = v["nodes"].as_array().unwrap().iter()
            .find(|n| n["id"] == "prog-a").unwrap();
        assert_eq!(prog_el["kind"], "progress");
        assert_eq!(prog_el["value"], 0.75);
        assert_eq!(prog_el["options"]["fill"], "art/fill.png");
        assert_eq!(prog_el["options"]["orientation"], "vertical");
        assert_eq!(prog_el["options"]["max"], 4);
    }

    const PROGRESS_MODULE: &str = r#"
export default (hostApi) => {
  hostApi.ui.window('prog-win', {}, [
    hostApi.ui.progress('hp-bar', {
      progress: {
        fill: 'art/fill.png',
        entity: 'ent-a',
        value: (entity) => entity.number_map.get('hp').orElse(hostApi.runtime.number.of(0)),
        max: (entity) => entity.number_map.get('max').orElse(hostApi.runtime.number.of(100))
      }
    })
  ]);
};
"#;

    #[test]
    fn progress_binds_entity_value_and_updates_live() {
        let _g = lock();
        crate::state::clear_state();
        install_module(PROGRESS_MODULE);
        // hp = 70 over [0, 100] -> (70 - 0) / 100 = 0.7 -> round(0.7 * 100) = 70
        set_entity_number("ent-a", "hp", 70.0);
        set_entity_number("ent-a", "max", 100.0);
        tick();

        let v: serde_json::Value = serde_json::from_str(&fetch_ui_state_json()).unwrap();
        let nodes = v["nodes"].as_array().unwrap();
        let prog = nodes.iter().find(|n| n["id"] == "hp-bar").unwrap();
        assert_eq!(prog["kind"], "progress");
        assert_eq!(prog["value"], 70.0);
        assert_eq!(prog["binding"]["entity"], "ent-a");
        assert_eq!(prog["binding"]["name"], "hp");
        // the .orElse(0) default is captured as the raw-value fallback
        assert_eq!(prog["binding"]["fallback"], 0.0);
        assert_eq!(prog["options"]["fill"], "art/fill.png");
        let delta: serde_json::Value = serde_json::from_str(
            &fetch_ui_delta_json().unwrap()).unwrap();
        assert!(delta["ops"].as_array().unwrap().iter().any(|o|
            o["op"] == "add" && o["node"]["id"] == "hp-bar"));

        // out-of-range clamps to steps (100); the next tick emits an update
        ui_delta().lock().unwrap().take();
        ui_dirty().store(false, Ordering::SeqCst);
        set_entity_number("ent-a", "hp", 200.0);
        tick();

        let v: serde_json::Value = serde_json::from_str(&fetch_ui_state_json()).unwrap();
        let prog = v["nodes"].as_array().unwrap().iter()
            .find(|n| n["id"] == "hp-bar").unwrap();
        assert_eq!(prog["value"], 100.0);
        let delta: serde_json::Value = serde_json::from_str(
            &fetch_ui_delta_json().unwrap()).unwrap();
        let ops = delta["ops"].as_array().unwrap();
        assert!(ops.iter().any(|o| o["op"] == "update"
            && o["node"]["id"] == "hp-bar"
            && o["node"]["value"] == 100.0));
        assert!(!ops.iter().any(|o| o["op"] == "remove"));
    }

    #[test]
    fn progress_broken_binding_falls_back_to_orElse_default() {
        let _g = lock();
        crate::state::clear_state();
        install_module(PROGRESS_MODULE);
        // no entity value yet: value_raw falls back to the .orElse default (0),
        // max to its .orElse default (100) -> (0 - 0) / 100 = 0 -> 0 steps
        tick();

        let v: serde_json::Value = serde_json::from_str(&fetch_ui_state_json()).unwrap();
        let prog = v["nodes"].as_array().unwrap().iter()
            .find(|n| n["id"] == "hp-bar").unwrap();
        assert_eq!(prog["value"], 0.0);

        // then the value arrives: hp = 30 over [0, 100] -> 0.3 -> 30 steps
        set_entity_number("ent-a", "hp", 30.0);
        set_entity_number("ent-a", "max", 100.0);
        tick();
        let v: serde_json::Value = serde_json::from_str(&fetch_ui_state_json()).unwrap();
        let prog = v["nodes"].as_array().unwrap().iter()
            .find(|n| n["id"] == "hp-bar").unwrap();
        assert_eq!(prog["value"], 30.0);
    }

    const PROGRESS_CONSTANT_MODULE: &str = r#"
export default (hostApi) => {
  hostApi.runtime.setEntity(hostApi.runtime.string.of('node-1'), {
    numberMap: {}
  });
  hostApi.ui.window('const-win', {}, [
    hostApi.ui.progress('const-bar', {
      panel: { x: 400, y: 400, width: 300, height: 100 },
      progress: {
        fill: 'art/fill.png',
        entity: hostApi.runtime.string.of('node-1'),
        value: (entity) => hostApi.runtime.number.of(3),
        max: (entity) => hostApi.runtime.number.of(10),
        min: (entity) => hostApi.runtime.number.of(0)
      }
    })
  ]);
};
"#;

    #[test]
    fn progress_constant_lambdas_bind_a_static_range() {
        let _g = lock();
        crate::state::clear_state();
        install_module(PROGRESS_CONSTANT_MODULE);
        tick();

        let v: serde_json::Value = serde_json::from_str(&fetch_ui_state_json()).unwrap();
        let prog = v["nodes"].as_array().unwrap().iter()
            .find(|n| n["id"] == "const-bar").unwrap();
        assert_eq!(prog["kind"], "progress");
        // constant 3 over the constant [0, 10] range -> 0.3 -> 30 steps
        assert_eq!(prog["value"], 30.0);
        assert_eq!(prog["binding"]["entity"], "node-1");
        // constant fields carry their value as the fallback and omit the
        // per-tick keys
        assert!(prog["binding"].get("name").is_none());
        assert_eq!(prog["binding"]["fallback"], 3.0);
        assert!(prog["binding"].get("maxName").is_none());
        assert_eq!(prog["binding"]["maxFallback"], 10.0);
        assert!(prog["binding"].get("minName").is_none());
        assert_eq!(prog["binding"]["minFallback"], 0.0);
        // later ticks keep the value (no entity field to re-read)
        ui_delta().lock().unwrap().take();
        ui_dirty().store(false, Ordering::SeqCst);
        set_entity_number("node-1", "anything", 99.0);
        tick();
        let v: serde_json::Value = serde_json::from_str(&fetch_ui_state_json()).unwrap();
        let prog = v["nodes"].as_array().unwrap().iter()
            .find(|n| n["id"] == "const-bar").unwrap();
        assert_eq!(prog["value"], 30.0);
    }

    const PROGRESS_MIN_MODULE: &str = r#"
export default (hostApi) => {
  hostApi.ui.window('temp-win', {}, [
    hostApi.ui.progress('temp-bar', {
      progress: {
        fill: 'art/fill.png',
        entity: 'ent-a',
        value: (entity) => entity.number_map.get('temp').orElse(hostApi.runtime.number.of(0)),
        min: (entity) => entity.number_map.get('min').orElse(hostApi.runtime.number.of(0)),
        max: (entity) => entity.number_map.get('max').orElse(hostApi.runtime.number.of(1))
      }
    })
  ]);
};
"#;

    #[test]
    fn progress_normalizes_entity_value_across_min_max_range() {
        let _g = lock();
        crate::state::clear_state();
        install_module(PROGRESS_MIN_MODULE);
        // temp = 10 over the entity-bound range min=-10..max=30 (span 40)
        // -> (10 - (-10)) / 40 = 0.5 -> round(0.5 * 100) = 50
        set_entity_number("ent-a", "temp", 10.0);
        set_entity_number("ent-a", "min", -10.0);
        set_entity_number("ent-a", "max", 30.0);
        tick();

        let v: serde_json::Value = serde_json::from_str(&fetch_ui_state_json()).unwrap();
        let prog = v["nodes"].as_array().unwrap().iter()
            .find(|n| n["id"] == "temp-bar").unwrap();
        assert_eq!(prog["value"], 50.0);
        // the entity-bound min/max keys live in the binding
        assert_eq!(prog["binding"]["minName"], "min");
        assert_eq!(prog["binding"]["maxName"], "max");

        // below min clamps to 0; above max clamps to steps (100)
        ui_delta().lock().unwrap().take();
        ui_dirty().store(false, Ordering::SeqCst);
        set_entity_number("ent-a", "temp", -100.0);
        tick();
        let v: serde_json::Value = serde_json::from_str(&fetch_ui_state_json()).unwrap();
        let prog = v["nodes"].as_array().unwrap().iter()
            .find(|n| n["id"] == "temp-bar").unwrap();
        assert_eq!(prog["value"], 0.0);

        set_entity_number("ent-a", "temp", 100.0);
        tick();
        let v: serde_json::Value = serde_json::from_str(&fetch_ui_state_json()).unwrap();
        let prog = v["nodes"].as_array().unwrap().iter()
            .find(|n| n["id"] == "temp-bar").unwrap();
        assert_eq!(prog["value"], 100.0);

        // changing the bounds live re-resolves: min=0, max=10, temp=5
        // -> (5 - 0) / 10 = 0.5 -> 50
        set_entity_number("ent-a", "min", 0.0);
        set_entity_number("ent-a", "max", 10.0);
        set_entity_number("ent-a", "temp", 5.0);
        tick();
        let v: serde_json::Value = serde_json::from_str(&fetch_ui_state_json()).unwrap();
        let prog = v["nodes"].as_array().unwrap().iter()
            .find(|n| n["id"] == "temp-bar").unwrap();
        assert_eq!(prog["value"], 50.0);
    }

    const CONTAINER_LIST_MODULE: &str = r#"
export default (hostApi) => {
  hostApi.runtime.setEntity(hostApi.runtime.string.of('item-a'), {
    numberMap: { 'value': hostApi.runtime.number.of(1) }
  });
  hostApi.runtime.setEntity(hostApi.runtime.string.of('item-b'), {
    numberMap: { 'value': hostApi.runtime.number.of(2) }
  });
  hostApi.runtime.setContainer('items', {
    entities: ['item-a', 'item-b']
  });
  hostApi.ui.window('list-panel', { panel: { width: 300, height: 300 } }, [
    hostApi.ui.entityList('items', { container: 'items' },
      (entity) => [
        hostApi.ui.window(entity.id, { panel: { width: 100, height: 50 } }, [
          hostApi.ui.field(entity.id + ':value', {
            entity: entity.id,
            map: 'number',
            name: 'value',
            fallback: '0'
          })
        ])
      ])
  ]);
};
"#;

    fn seed_container_state() {
        set_entity_number("item-a", "value", 1.0);
        set_entity_number("item-b", "value", 2.0);
        crate::state::set_last_containers(vec![
            r#"{"id":"items","entities":["item-a","item-b"]}"#.to_string()]);
    }

    fn seed_grid1_container() {
        // grid-1: a 100x100 container with room at (10,10) and crate at (40,40).
        crate::state::set_last_containers(vec![
            r#"{"id":"grid-1","entities":["room","crate"],"sizeX":{"value":100},"sizeY":{"value":100},"getX":{"room":10,"crate":40},"getY":{"room":10,"crate":40}}"#.to_string()]);
    }

    #[test]
    fn container_list_expands_one_item_per_entity() {
        let _g = lock();
        crate::state::clear_state();

        seed_container_state();
        install_module(CONTAINER_LIST_MODULE);
        tick();

        let v: serde_json::Value = serde_json::from_str(&fetch_ui_state_json()).unwrap();
        let nodes = v["nodes"].as_array().unwrap();
        let ids: Vec<&str> = nodes.iter().map(|n| n["id"].as_str().unwrap()).collect();
        // The list node + one item window + one field per entity.
        assert!(ids.contains(&"items"), "list node missing: {:?}", ids);
        assert!(ids.contains(&"item-a"), "item-a window missing: {:?}", ids);
        assert!(ids.contains(&"item-b"), "item-b window missing: {:?}", ids);
        assert!(ids.contains(&"item-a:value"), "item-a field missing: {:?}", ids);
        assert!(ids.contains(&"item-b:value"), "item-b field missing: {:?}", ids);

        // The list node's children are the materialized item windows.
        let list = nodes.iter().find(|n| n["id"] == "items").unwrap();
        let children = list["children"].as_array().unwrap();
        assert_eq!(children.len(), 2);
        assert_eq!(children[0], "item-a");
        assert_eq!(children[1], "item-b");

        // Field values resolved from the entity store.
        let fa = nodes.iter().find(|n| n["id"] == "item-a:value").unwrap();
        assert_eq!(fa["value"], "1");
        let fb = nodes.iter().find(|n| n["id"] == "item-b:value").unwrap();
        assert_eq!(fb["value"], "2");
    }

    #[test]
    fn container_list_reconciles_entity_additions_and_removals() {
        let _g = lock();
        crate::state::clear_state();

        seed_container_state();
        install_module(CONTAINER_LIST_MODULE);
        tick();
        ui_delta().lock().unwrap().take();

        // Add a third entity to the container; re-tick must materialize it.
        let mut containers = crate::state::last_containers().lock().unwrap().clone();
        containers.push(
            r#"{"id":"items","entities":["item-a","item-b","item-c"]}"#.to_string());
        *crate::state::last_containers().lock().unwrap() = containers;
        set_entity_number("item-c", "value", 3.0);
        tick();

        let v: serde_json::Value = serde_json::from_str(&fetch_ui_state_json()).unwrap();
        let ids: Vec<&str> = v["nodes"].as_array().unwrap().iter()
            .map(|n| n["id"].as_str().unwrap()).collect();
        assert!(ids.contains(&"item-c"), "item-c not materialized: {:?}", ids);
        let list = v["nodes"].as_array().unwrap().iter()
            .find(|n| n["id"] == "items").unwrap();
        let children = list["children"].as_array().unwrap();
        assert_eq!(children.len(), 3);

        // Remove item-b; re-tick must drop its nodes.
        let mut containers = crate::state::last_containers().lock().unwrap().clone();
        *containers.iter_mut().find(|c| c.contains("\"item-c\""))
            .expect("item-c container row") =
            r#"{"id":"items","entities":["item-a","item-c"]}"#.to_string();
        *crate::state::last_containers().lock().unwrap() = containers;
        tick();

        let v: serde_json::Value = serde_json::from_str(&fetch_ui_state_json()).unwrap();
        let ids: Vec<&str> = v["nodes"].as_array().unwrap().iter()
            .map(|n| n["id"].as_str().unwrap()).collect();
        assert!(!ids.contains(&"item-b"), "item-b not removed: {:?}", ids);
        assert!(!ids.contains(&"item-b:value"), "item-b field not removed: {:?}", ids);
        assert!(ids.contains(&"item-a") && ids.contains(&"item-c"));
    }

    #[test]
    fn container_list_unknown_container_renders_zero_items() {
        let _g = lock();
        crate::state::clear_state();
        // A list whose target container was never registered.
        let module = CONTAINER_LIST_MODULE.replace(
            "{ container: 'items' }", "{ container: 'missing' }");
        install_module(&module);
        tick();

        let v: serde_json::Value = serde_json::from_str(&fetch_ui_state_json()).unwrap();
        let ids: Vec<&str> = v["nodes"].as_array().unwrap().iter()
            .map(|n| n["id"].as_str().unwrap()).collect();
        // The list node exists but no items were materialized.
        assert!(ids.contains(&"items"), "list node missing: {:?}", ids);
        assert!(!ids.iter().any(|id| id.starts_with("item-")),
            "expected zero items, got {:?}", ids);
    }

    const CONTAINER_VIEW_AREA_MODULE: &str = r#"
export default (hostApi) => {
  hostApi.runtime.setEntity(hostApi.runtime.string.of('room'), {
    numberMap: { column: hostApi.runtime.number.of(10), row: hostApi.runtime.number.of(10) },
    areaMap: { floor: { polygon: [[0, 0], [100, 0], [100, 100], [0, 100]] } }
  });
  hostApi.runtime.setEntity(hostApi.runtime.string.of('crate'), {
    numberMap: { column: hostApi.runtime.number.of(40), row: hostApi.runtime.number.of(40) }
  });
  hostApi.runtime.setContainer(hostApi.runtime.string.of('grid-1'), {
    entities: ['room', 'crate'],
    getX: (entity) => entity.number_map.get('column').orElse(hostApi.runtime.number.of(0)),
    getY: (entity) => entity.number_map.get('row').orElse(hostApi.runtime.number.of(0)),
    sizeX: { value: hostApi.runtime.number.of(100), outOfBounds: 'clamp' },
    sizeY: { value: hostApi.runtime.number.of(100), outOfBounds: 'clamp' }
  });
  hostApi.ui.containerView('world', {
    container: 'grid-1',
    panel: { width: 700, height: 500 },
    area: [
      { name: 'floor', color: [1, 0, 0, 1], bodyColor: [0, 1, 0, 1], thickness: 3 }
    ]
  }, (entity) => hostApi.ui.window(entity.id, {}, []));
};
"#;

    #[test]
    fn container_view_item_gets_area_outline_stamped() {
        let _g = lock();
        crate::state::clear_state();
        seed_grid1_container();
        // Seed room's declared area directly (the JS module's setEntity area
        // goes through process_module; here we seed the Rust state store).
        crate::state::set_entity_area("room", "floor", vec![
            (0.0, 0.0), (100.0, 0.0), (100.0, 100.0), (0.0, 100.0),
        ]);
        install_module(CONTAINER_VIEW_AREA_MODULE);
        tick();

        let v: serde_json::Value = serde_json::from_str(&fetch_ui_state_json()).unwrap();
        let nodes = v["nodes"].as_array().unwrap();
        // room declares a 100x100 local area; view is 700x500 over a 100x100
        // container, so cell_w = 7, cell_h = 5. room's world pos is (10, 10),
        // so view-local top-left is (70, 50). The area spans 100x100 local ->
        // 700x500 view units, so the outline spans (70,50)..(770,550).
        let room = nodes.iter().find(|n| n["id"] == "room").unwrap_or_else(|| {
            let dbg_ids: Vec<&str> = nodes.iter().map(|n| n["id"].as_str().unwrap()).collect();
            panic!("room node missing; ids: {dbg_ids:?}");
        });
        let outline = room["options"]["panel"]["areaOutline"].clone();
        assert!(outline.is_object(), "room should have areaOutline: {outline}");
        // The view's `area` selects room's "floor" area, so exactly one
        // polygon is stamped, carrying the view's per-area styling.
        let polys = outline["polygons"].as_array().unwrap();
        assert_eq!(polys.len(), 1, "room should stamp one polygon: {outline}");
        // Per-polygon styling from the view's `area` entry.
        let color = polys[0]["color"].as_array().unwrap();
        assert_eq!(
            (color[0].as_f64().unwrap(), color[1].as_f64().unwrap(), color[2].as_f64().unwrap(), color[3].as_f64().unwrap()),
            (1.0, 0.0, 0.0, 1.0),
            "room outline color: {polys:?}");
        let body = polys[0]["bodyColor"].as_array().unwrap();
        assert_eq!(
            (body[0].as_f64().unwrap(), body[1].as_f64().unwrap(), body[2].as_f64().unwrap(), body[3].as_f64().unwrap()),
            (0.0, 1.0, 0.0, 1.0),
            "room body color: {polys:?}");
        assert!((polys[0]["thickness"].as_f64().unwrap() - 3.0).abs() < 1e-9,
            "room thickness: {polys:?}");
        let pts = polys[0]["points"].as_array().unwrap();
        assert_eq!(pts.len(), 4, "room area has 4 corners: {pts:?}");
        // The four view-local corners of the 100x100 area at (70,50):
        // (70,50),(770,50),(770,550),(70,550). Collect the set of points and
        // compare against the expected set (winding order is normalized by
        // make_area, so we compare unordered).
        let corners: Vec<String> = pts.iter()
            .map(|p| {
                let a = p.as_array().unwrap();
                let x = a[0].as_f64().unwrap();
                let y = a[1].as_f64().unwrap();
                format!("{x:.0},{y:.0}")
            })
            .collect();
        let expected = vec![
            "70,50".to_string(),
            "770,50".to_string(),
            "770,550".to_string(),
            "70,550".to_string(),
        ];
        assert_eq!(corners.len(), 4, "room area has 4 corners: {corners:?}");
        for e in &expected {
            assert!(corners.contains(e), "missing corner {e}; got {corners:?}");
        }

        // crate has no declared area: a 1x1 fallback rect at its view-local
        // origin. crate's world pos is (40, 40) -> view-local (280, 200).
        let crate_node = nodes.iter().find(|n| n["id"] == "crate").unwrap();
        let c_outline = crate_node["options"]["panel"]["areaOutline"].clone();
        assert!(c_outline.is_object(), "crate should have 1x1 fallback: {c_outline}");
        // crate declares no areas: a single 1x1 fallback polygon.
        let c_polys = c_outline["polygons"].as_array().unwrap();
        assert_eq!(c_polys.len(), 1, "crate should stamp one fallback polygon: {c_outline}");
        // The view requested the "floor" style; crate declares no "floor"
        // area, so its 1x1 fallback carries that same requested style.
        let c_color = c_polys[0]["color"].as_array().unwrap();
        assert_eq!(
            (c_color[0].as_f64().unwrap(), c_color[1].as_f64().unwrap()),
            (1.0, 0.0),
            "crate fallback color: {c_polys:?}");
        assert!((c_polys[0]["thickness"].as_f64().unwrap() - 3.0).abs() < 1e-9,
            "crate fallback thickness: {c_polys:?}");
        let c_pts = c_polys[0]["points"].as_array().unwrap();
        assert_eq!(c_pts.len(), 4, "crate fallback is a 1x1 rect: {c_pts:?}");
        let c0 = c_pts[0].as_array().unwrap();
        assert!((c0[0].as_f64().unwrap() - 280.0).abs() < 1e-6, "crate x0: {}", c0[0]);
        assert!((c0[1].as_f64().unwrap() - 200.0).abs() < 1e-6, "crate y0: {}", c0[1]);
        let c1 = c_pts[1].as_array().unwrap();
        assert!((c1[0].as_f64().unwrap() - 281.0).abs() < 1e-6, "crate x1: {}", c1[0]);
        assert!((c1[1].as_f64().unwrap() - 200.0).abs() < 1e-6, "crate y1: {}", c1[1]);
    }

    #[test]
    fn store_round_trips_through_binary_slab() {
        let _g = lock();
        ui_nodes().lock().unwrap().clear();
        prev_store_ids().lock().unwrap().clear();
        ui_delta().lock().unwrap().take();
        apply_diff(&[
            node_json(
                r#"{"kind":"window","id":"win-a","options":{"panel":{"x":10,"y":-20,"anchor":"top-left","width":200,"height":100}},"children":["t"]}"#),
            node_json(
                r#"{"kind":"text","id":"t","value":"spine","children":[]}"#),
            node_json(
                r#"{"kind":"field","id":"hp","binding":{"entity":"ent-a","map":"number","name":"hp","fallback":"n/a"},"value":"7","children":[]}"#),
        ]).unwrap();
        let mut anims = HashMap::new();
        anims.insert(
            "blink".to_string(),
            serde_json::json!({"frames":[{"sprite":"a.png"},{"sprite":"b.png"}],"duration":1.5,"loop":true}),
        );
        *animations().lock().unwrap() = anims;

        // Snapshot: store -> slab -> back out the domain data.
        let nodes = ui_nodes().lock().unwrap().clone();
        let snap = crate::ui::abi::build_snapshot(
            &nodes, &animations().lock().unwrap().clone());
        unsafe {
            let s = &*snap;
            assert_eq!(s.version, crate::ui::abi::UI_ABI_VERSION);
            assert_eq!(s.node_count, 3);
            assert_eq!(s.anim_count, 1);
            let arena = s.strings;
            let cstr = |off: u32| -> String {
                std::ffi::CStr::from_ptr(
                    arena.add(off as usize) as *const i8)
                    .to_string_lossy().into_owned()
            };
            let ns = std::slice::from_raw_parts(s.nodes, 3);
            let win = ns.iter().find(|n| cstr(n.id) == "win-a").unwrap();
            assert_eq!(win.kind, crate::ui::abi::UI_KIND_WINDOW);
            assert!((win.opt.x - 10.0).abs() < f32::EPSILON);
            assert_eq!(cstr(win.opt.anchor), "top-left");
            assert!((win.opt.width - 200.0).abs() < f32::EPSILON);
            assert_eq!(win.child_count, 1);
            let text = ns.iter().find(|n| cstr(n.id) == "t").unwrap();
            assert_eq!(cstr(text.value), "spine");
            let hp = ns.iter().find(|n| cstr(n.id) == "hp").unwrap();
            assert_eq!(hp.kind, crate::ui::abi::UI_KIND_FIELD);
            assert_eq!(cstr(hp.binding.entity), "ent-a");
            assert_eq!(hp.binding.map, crate::ui::abi::UI_MAP_NUMBER);
            assert_eq!(cstr(hp.value), "7");
            let anims = std::slice::from_raw_parts(s.anims, 1);
            assert_eq!(cstr(anims[0].name), "blink");
            assert_eq!(anims[0].frame_count, 2);
            crate::ui::abi::free_snapshot(snap);
        }

        // Delta: pending ops -> slab -> ops intact, then consumed.
        let delta = crate::ui::abi::build_delta(
            &ui_delta().lock().unwrap().take().unwrap().ops);
        unsafe {
            let d = &*delta;
            assert_eq!(d.op_count, 3);
            let arena = d.strings;
            let cstr = |off: u32| -> String {
                std::ffi::CStr::from_ptr(
                    arena.add(off as usize) as *const i8)
                    .to_string_lossy().into_owned()
            };
            let ops = std::slice::from_raw_parts(d.ops, 3);
            assert!(ops.iter().all(|o| o.op == crate::ui::abi::UI_OP_ADD));
            assert!(ops.iter().any(|o| cstr(o.node.id) == "win-a"));
            crate::ui::abi::free_delta(delta);
        }

    }
}
