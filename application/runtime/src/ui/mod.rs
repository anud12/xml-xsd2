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
    #[serde(rename = "image")]
    Image {
        id: String,
        #[serde(default)]
        src: String,
        #[serde(default)]
        children: Vec<String>,
    },
    #[serde(rename = "canvas")]
    Canvas {
        id: String,
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
            UiNode::Image { id, .. } => id,
            UiNode::Canvas { id, .. } => id,
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
    let script = format!(
        "__uiHost.resetContainers();\n\
         globalThis.__uiContainerList = {};\n\
         __uiHost.expandContainers(globalThis.__uiEntitiesFor);\n\
         __uiHost.expandContainerViews(globalThis.__uiEntitiesFor);\n\
         JSON.stringify(__uiHost.snapshot())",
        containers_json
    );
    let snapshot_json = eval_string(&ctx, &script)?;
    let mut snapshot: Vec<UiNode> = serde_json::from_str(&snapshot_json)?;
    resolve_field_values(&mut snapshot);
    resolve_container_view_positions(&mut snapshot);

    // Keep the animation-definition store in sync for the fetch FFI (the
    // module's registerAnimation calls filled __registeredAnimations at
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
        (String, Option<f64>, Option<f64>, serde_json::Map<String, serde_json::Value>),
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
        let prev = geom.iter().find(|g| g.0 == id).map(|g| g.3.clone());
        let per_entity = merge_position_maps(&prev, &v);
        geom.retain(|g| g.0 != id);
        geom.push((id.to_string(), size_x, size_y, per_entity));
    }

    // First pass (immutable): collect each view's (container id, cell size,
    // item child ids). The cell size is precomputed so the second pass owns
    // all the data it needs and never borrows `geom` or the snapshot.
    let mut views: Vec<(String, f64, f64, Vec<String>)> = Vec::new();
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
        let (sx, sy) = match geom.iter().find(|g| g.0 == cid) {
            Some((_, Some(a), Some(b), _)) => (*a, *b),
            _ => continue,
        };
        views.push((
            cid.to_string(),
            view_w / sx,
            view_h / sy,
            children.clone(),
        ));
    }

    // Second pass (mutable): index snapshot positions by id (owned keys),
    // then write each item's cell geometry via `get_mut(position)`.
    let mut index: std::collections::HashMap<String, usize> = std::collections::HashMap::new();
    for (i, node) in snapshot.iter().enumerate() {
        index.insert(node.id().to_string(), i);
    }

    for (cid, cell_w, cell_h, children) in views {
        let Some(per_entity) = geom
            .iter()
            .find(|g| g.0 == cid)
            .map(|g| g.3.clone())
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
            let Some(entity) = item_obj.get("entity").and_then(|e| e.as_str()) else {
                continue;
            };
            let Some(ent_pos) = per_entity.get(entity).and_then(|p| p.as_object()) else {
                continue;
            };
            let get = |key: &str| ent_pos.get(key).and_then(|v| v.as_f64()).unwrap_or(0.0);
            let x = get("x") * cell_w;
            let y = get("y") * cell_h;
            let span_x = get("spanX").max(1.0) * cell_w;
            let span_y = get("spanY").max(1.0) * cell_h;
            item_obj.insert("x".into(), num_json(x));
            item_obj.insert("y".into(), num_json(y));
            item_obj.insert("width".into(), num_json(span_x));
            item_obj.insert("height".into(), num_json(span_y));
        }
    }
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
  hostApi.ui.div('spine-div', {}, [
    hostApi.ui.text('spine-text', 'spine')
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
        assert_eq!(nodes.len(), 2);
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
        assert_eq!(adds.len(), 2);
        assert!(adds.contains(&"spine-div"));
        assert!(adds.contains(&"spine-text"));
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
  hostApi.ui.field('hp-field', {
    entity: 'ent-a',
    map: 'number',
    name: 'hp',
    fallback: 'n/a'
  });
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
                r#"{"kind":"division","id":"d","options":{"layout":"column"},"children":["t"]}"#),
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
            r#"{"kind":"window","id":"win-a","options":{"x":10,"y":-20,"anchor":"top-left","align":"top-left","width":200,"height":100},"children":["t"]}"#);
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
                    assert_eq!(options["x"], 10);
                    assert_eq!(options["y"], -20);
                    assert_eq!(options["anchor"], "top-left");
                    assert_eq!(options["align"], "top-left");
                    assert_eq!(options["width"], 200);
                    assert_eq!(options["height"], 100);
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
        assert_eq!(win_el["options"]["anchor"], "top-left");
        assert_eq!(win_el["options"]["align"], "top-left");
        assert_eq!(win_el["options"]["x"], 10);
        assert_eq!(win_el["options"]["y"], -20);
        assert_eq!(win_el["options"]["width"], 200);
        assert_eq!(win_el["options"]["height"], 100);
        assert_eq!(win_el["children"][0], "t");
    }

    #[test]
    fn div_options_carry_onclick_and_onhover() {
        let _g = lock();
        ui_nodes().lock().unwrap().clear();
        prev_store_ids().lock().unwrap().clear();
        ui_delta().lock().unwrap().take();
        let div = node_json(
            r#"{"kind":"division","id":"btn","options":{"layout":"column","onClick":"do-thing","onHover":{"background":"hover.png","emitAction":"btn-hover"}},"children":[]}"#);
        apply_diff(&[div]).unwrap();

        let json = fetch_ui_state_json();
        let v: serde_json::Value = serde_json::from_str(&json).unwrap();
        let el = v["nodes"].as_array().unwrap().iter()
            .find(|n| n["id"] == "btn").unwrap();
        assert_eq!(el["options"]["onClick"], "do-thing");
        assert_eq!(el["options"]["onHover"]["background"], "hover.png");
        assert_eq!(el["options"]["onHover"]["emitAction"], "btn-hover");
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
  hostApi.ui.window('list-panel', { width: 300, height: 300 }, [
    hostApi.ui.entityList('items', { container: 'items' },
      (entity) => [
        hostApi.ui.window(entity.id, { width: 100, height: 50 }, [
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

    #[test]
    fn store_round_trips_through_binary_slab() {
        let _g = lock();
        ui_nodes().lock().unwrap().clear();
        prev_store_ids().lock().unwrap().clear();
        ui_delta().lock().unwrap().take();
        apply_diff(&[
            node_json(
                r#"{"kind":"window","id":"win-a","options":{"x":10,"y":-20,"anchor":"top-left","width":200,"height":100},"children":["t"]}"#),
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

    #[test]
    fn canvas_node_with_world_options_round_trips_store_and_delta() {
        let _g = lock();
        ui_nodes().lock().unwrap().clear();
        prev_store_ids().lock().unwrap().clear();
        ui_delta().lock().unwrap().take();
        let canvas = node_json(
            r#"{"kind":"canvas","id":"world-canvas","options":{"world":{"map":"cave","room":"cave-1"},"camera":{"room":"cave-1","x":0,"y":0,"zoom":1}},"children":["hud"]}"#);
        assert!(matches!(&canvas, UiNode::Canvas { id, .. } if id == "world-canvas"));
        apply_diff(&[canvas.clone()]).unwrap();

        // the add op carries the full options
        let delta = ui_delta().lock().unwrap().take().unwrap();
        let add = delta.ops.iter().find(|o| matches!(o, UiDeltaOp::Add { node }
            if node.id() == "world-canvas")).expect("add op for canvas");
        match add {
            UiDeltaOp::Add { node } => match node {
                UiNode::Canvas { id, options, children } => {
                    assert_eq!(id, "world-canvas");
                    assert_eq!(options["world"]["room"], "cave-1");
                    assert_eq!(options["camera"]["zoom"], 1);
                    assert_eq!(children, &vec!["hud".to_string()]);
                }
                other => panic!("expected canvas node, got {:?}", other),
            },
            other => panic!("expected add, got {:?}", other),
        }

        // the store round-trips the node with options intact
        let json = fetch_ui_state_json();
        let v: serde_json::Value = serde_json::from_str(&json).unwrap();
        let canvas_el = v["nodes"].as_array().unwrap().iter()
            .find(|n| n["id"] == "world-canvas").unwrap();
        assert_eq!(canvas_el["kind"], "canvas");
        assert_eq!(canvas_el["options"]["world"]["map"], "cave");
        assert_eq!(canvas_el["options"]["world"]["room"], "cave-1");
        assert_eq!(canvas_el["children"][0], "hud");
    }
}
