//! UI state store + persistent QuickJS host for the .ui layer.
//!
//! The canonical .ui layer (application/ui/ui/*.js) is engine-agnostic: it
//! talks to the engine only through `globalThis.__uiTransport` and a small
//! set of injected globals. This module owns the Rust side of that seam:
//!
//! - a persistent QuickJS context (engine of record) that loads the .ui
//!   layer once and evaluates the declared UI nodes in it;
//! - the id-keyed UI node store (`UI_NODES`) that renderers fetch;
//! - the id-diff producing a `UiDelta` (add/update/remove).

#![allow(dead_code)]
use std::collections::HashMap;
use std::sync::atomic::{AtomicBool, Ordering};
use std::sync::{Mutex, Once};

use anyhow::Result;
use serde::{Deserialize, Serialize};

use crate::js_runtime::{create_context, create_runtime};

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
static mut DECLARED_NODES:
    Option<&'static Mutex<HashMap<String, Vec<UiNode>>>> = None;
static mut UI_DIRTY: Option<&'static AtomicBool> = None;
static mut UI_DELTA: Option<&'static Mutex<Option<UiDelta>>> = None;
static mut UI_HOST: Option<&'static UiHost> = None;
static mut PREV_STORE_IDS: Option<&'static Mutex<Vec<String>>> = None;
static mut UI_MODULE_OWNERS:
    Option<&'static Mutex<HashMap<String, Vec<String>>>> = None;
static mut UI_ANIMATIONS:
    Option<&'static Mutex<HashMap<String, serde_json::Value>>> = None;
static mut UI_MODULE_SOURCES:
    Option<&'static Mutex<HashMap<String, String>>> = None;

fn init() {
    INIT.call_once(|| unsafe {
        UI_NODES = Some(Box::leak(Box::new(Mutex::new(Vec::new()))));
        DECLARED_NODES = Some(Box::leak(Box::new(Mutex::new(HashMap::new()))));
        UI_DIRTY = Some(Box::leak(Box::new(AtomicBool::new(false))));
        UI_DELTA = Some(Box::leak(Box::new(Mutex::new(None))));
        PREV_STORE_IDS = Some(Box::leak(Box::new(Mutex::new(Vec::new()))));
        UI_MODULE_OWNERS = Some(Box::leak(Box::new(Mutex::new(HashMap::new()))));
        UI_ANIMATIONS = Some(Box::leak(Box::new(Mutex::new(HashMap::new()))));
        UI_MODULE_SOURCES = Some(Box::leak(Box::new(Mutex::new(HashMap::new()))));
    });
}

pub fn ui_nodes() -> &'static Mutex<Vec<UiNode>> {
    init();
    unsafe { UI_NODES.expect("ui nodes initialized") }
}
pub fn declared_nodes() -> &'static Mutex<HashMap<String, Vec<UiNode>>> {
    init();
    unsafe { DECLARED_NODES.expect("declared ui nodes initialized") }
}
pub fn all_declared_nodes() -> Vec<UiNode> {
    let declared = declared_nodes().lock().unwrap();
    let mut out: Vec<UiNode> = Vec::new();
    for nodes in declared.values() {
        out.extend(nodes.iter().cloned());
    }
    out
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
/// Module source per module id, kept so the persistent host can re-run the
/// .ui layer (container-list render lambdas can't be serialized).
pub fn module_sources() -> &'static Mutex<HashMap<String, String>> {
    init();
    unsafe { UI_MODULE_SOURCES.expect("ui module sources initialized") }
}
/// Store a module's source so the persistent host can re-run its .ui layer.
pub fn set_module_source(module_id: &str, source: &str) {
    init();
    module_sources().lock().unwrap()
        .insert(module_id.to_string(), source.to_string());
}

/// Clear all UI state (called from `runtime_clear_state`).
pub fn clear() {
    init();
    ui_nodes().lock().unwrap().clear();
    declared_nodes().lock().unwrap().clear();
    prev_store_ids().lock().unwrap().clear();
    module_owners().lock().unwrap().clear();
    animations().lock().unwrap().clear();
    module_sources().lock().unwrap().clear();
    *ui_delta().lock().unwrap() = None;
    ui_dirty().store(false, Ordering::SeqCst);
}

/// Reset the node store and diff baseline (called from `runtime_clear_state`
/// before a new archive is processed, so a module never sees the previous
/// archive's leftover nodes).
pub fn reset_store() {
    init();
    ui_nodes().lock().unwrap().clear();
    prev_store_ids().lock().unwrap().clear();
    *ui_delta().lock().unwrap() = None;
    ui_dirty().store(false, Ordering::SeqCst);
}

/// Store the nodes a module declared (called from the declarations
/// pipeline). Re-declaring replaces that module's previous nodes.
pub fn set_declared(module_id: &str, nodes: Vec<UiNode>) {
    runtime_log!(
        "ui: module \"{}\" declared {} node(s)",
        module_id, nodes.len());
    let mut ids: Vec<String> = Vec::new();
    for n in nodes.iter() { ids.push(n.id().to_string()); }
    declared_nodes().lock().unwrap().insert(module_id.to_string(), nodes);
    module_owners().lock().unwrap().insert(module_id.to_string(), ids);
}

// ---------------------------------------------------------------------------
// Persistent QuickJS host
// ---------------------------------------------------------------------------

pub struct UiHost {
    pub rt: rquickjs::Runtime,
    pub ctx: rquickjs::Context,
}

fn ui_layer_bundle() -> String {
    [
        crate::js_host_api::script_ui::ui_transport_shim(),
        include_str!("../../../ui/ui/host.js"),
    ]
    .join("\n")
}

fn ensure_host() -> Result<&'static UiHost> {
    let existing = unsafe { UI_HOST };
    if let Some(h) = existing { return Ok(h); }
    let rt = create_runtime()?;
    let ctx = create_context(&rt)?;
    // The host lives for the process lifetime; the Runtime must outlive the
    // Context, so both are leaked together.
    let host = Box::leak(Box::new(UiHost { rt, ctx }));
    unsafe { UI_HOST = Some(host); }
    let bundle = ui_layer_bundle();
    host.ctx.with(|c| c.eval::<(), _>(bundle.clone()))
        .map_err(|e| anyhow::anyhow!("ui host eval failed: {:?}", e))?;
    runtime_log!("ui: persistent QuickJS host initialized");
    Ok(host)
}

fn eval_string(ctx: &rquickjs::Context, script: &str) -> Result<String> {
    ctx.with(|c| c.eval::<String, _>(script.to_string()))
        .map_err(|e| anyhow::anyhow!("ui host eval failed: {:?}", e))
}

/// Evaluate the UI DAG in the persistent QuickJS host and reconcile the
/// result into the node store, emitting an id-keyed delta when it changed.
pub fn tick() {
    if let Err(e) = tick_inner() {
        runtime_log!("ui: tick failed: {:?}", e);
    }
}

fn tick_inner() -> Result<()> {
    let host = ensure_host()?;
    let declared = all_declared_nodes();
    if declared.is_empty() { return Ok(()); }

    // Start from a clean .ui context each tick so a previous tick's (or a
    // previous test's) module registry can't leak into this snapshot. The
    // bundle re-eval re-installs __uiHost with empty node + render registries;
    // then the container-list modules are re-run so their render lambdas are
    // live (the extraction context is gone; lambdas can't be serialized).
    let bundle = ui_layer_bundle();
    host.ctx.with(|c| c.eval::<(), _>(bundle.to_string()))
        .map_err(|e| anyhow::anyhow!("ui host re-eval failed: {:?}", e))?;

    let declared_ids: Vec<String> = declared_nodes()
        .lock().unwrap().keys().cloned().collect();
    // Only re-run modules that actually declare a container list: re-running
    // a module whose source has `import` statements or external references
    // would fail in the persistent host. Container-less modules keep their
    // declared static nodes (no render lambdas needed).
    for module_id in declared_ids {
        let Some(src) = module_sources().lock().unwrap().get(&module_id).cloned()
        else { continue; };
        if !contains_container_list(&src) { continue; }
        let script = ui_layer_rerun_script(&src);
        if let Err(e) = host.ctx.with(|c| c.eval::<(), _>(script.to_string())) {
            runtime_log!("ui: module \"{}\" re-run failed: {:?}", module_id, e);
        }
    }

    let seed: String = serde_json::to_string(&declared)?;
    // Install the entity lookup the .ui layer uses to expand container lists.
    // Each list node's `options.container` names a runtime container; the
    // lookup returns that container's entity ids. Unknown containers render
    // zero items (the list node stays in the tree with empty children).
    // TODO: collect and surface module UI errors (currently log-only).
    let containers_json = container_entities_json()?;
    let script = format!(
        "globalThis.__uiContainerList = {};\n\
         __uiHost.loadSnapshot({});\n\
         globalThis.__uiEntitiesFor = function (name) {{\n\
           var node = null;\n\
           var snap = __uiHost.snapshot();\n\
           for (var i = 0; i < snap.length; i++) {{ if (snap[i].id === name) {{ node = snap[i]; break; }} }}\n\
           var cid = node && node.options && node.options.container;\n\
           if (typeof cid !== 'string') return [];\n\
           var map = {{}};\n\
           for (var j = 0; j < globalThis.__uiContainerList.length; j++) {{\n\
             map[globalThis.__uiContainerList[j].id] = globalThis.__uiContainerList[j].entities || [];\n\
           }}\n\
           return map[cid] || [];\n\
         }};\n\
         __uiHost.expandContainers(__uiEntitiesFor);",
        containers_json, seed
    );
    host.ctx.with(|c| c.eval::<(), _>(script))
    .map_err(|e| anyhow::anyhow!("ui seed failed: {:?}", e))?;

    let snapshot_json =
        eval_string(&host.ctx, "JSON.stringify(__uiHost.snapshot())")?;
    let mut snapshot: Vec<UiNode> = serde_json::from_str(&snapshot_json)?;
    resolve_field_values(&mut snapshot);

    apply_diff(&snapshot)
}



/// True when the module source declares a container list. Only such modules
/// are re-run in the persistent host: container-less modules keep their
/// declared static nodes (no render lambdas needed) and re-running a module
/// whose source has imports or external references would fail there.
fn contains_container_list(source: &str) -> bool {
    source.contains("ui.container(") || source.contains("ui.container (")
}

/// Builds the script that re-runs a module's .ui layer in the persistent
/// host: a minimal hostApi (UI factories + no-op runtime calls) is provided,
/// then the module entrypoint is invoked, re-registering the static nodes and
/// refreshing the container render-lambda registry.
fn ui_layer_rerun_script(source: &str) -> String {
    // The stored source is already transformed (`var __module_default = ...`).
    // Reuse that binding rather than re-prefixing.
    let body = if source.contains("export default") {
        source.replace("export default", "var __ui_mod =")
    } else if source.contains("var __module_default") {
        source.replace("var __module_default", "var __ui_mod")
    } else {
        format!("var __ui_mod = {}", source)
    };
    format!(
        r#"
{body}
globalThis.__uiHost.clear();
(function () {{
  var hostApi = {{
    ui: {{
      getSpritePNG: function (p) {{ return p; }},
      getAnimation: function (name, dur) {{
        var n = typeof name === 'object' ? name.value : name;
        return {{ name: n, duration: (dur && dur.duration) || 1 }};
      }},
      div: __uiHost.div,
      text: __uiHost.text,
      window: __uiHost.window,
      field: __uiHost.field,
      image: __uiHost.image,
      canvas: __uiHost.canvas,
      container: __uiHost.container
    }},
    runtime: {{
      string: {{ of: function (s) {{ return s; }} }},
      number: {{ of: function (n) {{ return n; }} }},
      setEntity: function () {{}},
      setContainer: function () {{}},
      registerAction: function () {{}},
      registerEffect: function () {{}},
      registerAnimation: function () {{}},
      log: function () {{}}
    }}
  }};
  if (typeof __ui_mod === 'function') __ui_mod(hostApi);
}})();
"#
    )
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
    use std::sync::Mutex as StdMutex;

    // All UI state is process-global; serialize the tests that touch it.
    static TEST_LOCK: StdMutex<()> = StdMutex::new(());
    fn lock() -> std::sync::MutexGuard<'static, ()> {
        TEST_LOCK.lock().unwrap_or_else(|e| e.into_inner())
    }

    fn node_json(json: &str) -> UiNode {
        serde_json::from_str(json).unwrap()
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
        clear();
        let dec = crate::js_executor::extract_from_source(SPINE_MODULE)
            .expect("extraction should succeed");
        assert_eq!(dec.ui_nodes.len(), 2, "expected div + text declarations");
        crate::module::declarations::apply_declarations(&dec, "spine-module");
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
    fn redeclaration_produces_update_delta() {
        let _g = lock();
        clear();
        let dec = crate::js_executor::extract_from_source(SPINE_MODULE).unwrap();
        crate::module::declarations::apply_declarations(&dec, "spine-module");
        tick();
        ui_delta().lock().unwrap().take();
        ui_dirty().store(false, Ordering::SeqCst);

        let changed = SPINE_MODULE.replace("'spine'", "'spine-v2'");
        let dec2 = crate::js_executor::extract_from_source(&changed).unwrap();
        crate::module::declarations::apply_declarations(&dec2, "spine-module");
        tick();

        let delta: serde_json::Value = serde_json::from_str(
            &fetch_ui_delta_json().unwrap()).unwrap();
        let ops = delta["ops"].as_array().unwrap();
        assert!(ops.iter().any(|o| o["op"] == "update"
            && o["node"]["id"] == "spine-text"
            && o["node"]["value"] == "spine-v2"));
    }

    #[test]
    fn one_iteration_keeps_spine_nodes_visible() {
        let _g = lock();
        clear();
        let dec = crate::js_executor::extract_from_source(SPINE_MODULE).unwrap();
        crate::module::declarations::apply_declarations(&dec, "spine-module");
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

    const WORLD_MODULE: &str = r#"
export default (hostApi) => {
  hostApi.runtime.setRoom('cave-1', {
    terrain: 'stone',
    origin: { x: 10, y: -4.5 },
    rotation: 0.75,
    points: [{ x: 1, y: 2 }, { x: 3, y: 4 }, { x: 5, y: 6 }]
  });
  hostApi.runtime.setPortal('p-1', {
    from: { room: 'cave-1', edge: 2, range: { t0: 0.2, t1: 0.8 } },
    to: { room: 'hall-2', edge: 0, range: { t0: 0, t1: 1 } }
  });
  hostApi.ui.canvas('world-canvas', {
    world: { room: 'cave-1' },
    camera: { room: 'cave-1', x: 0, y: 0, zoom: 2 }
  }, []);
};
"#;

    #[test]
    fn world_module_populates_rooms_portals_and_canvas_node() {
        let _g = lock();
        clear();
        crate::state::clear_state();

        let dec = crate::js_executor::extract_from_source(WORLD_MODULE)
            .expect("extraction should succeed");
        assert_eq!(dec.rooms.len(), 1, "expected one room declaration");
        assert_eq!(dec.portals.len(), 1, "expected one portal declaration");
        assert_eq!(dec.ui_nodes.len(), 1, "expected canvas node declaration");

        crate::module::declarations::apply_declarations(&dec, "world-module");
        tick();

        // rooms + portals landed in state
        {
            let rooms = crate::state::rooms().lock().unwrap();
            assert_eq!(rooms.len(), 1);
            assert_eq!(rooms[0].id, "cave-1");
            assert_eq!(rooms[0].terrain, "stone");
            assert!((rooms[0].origin.0 - 10.0).abs() < 1e-12);
            assert!((rooms[0].origin.1 - (-4.5)).abs() < 1e-12);
            assert!((rooms[0].rotation - 0.75).abs() < 1e-12);
            assert_eq!(rooms[0].points.len(), 3);
            assert!((rooms[0].points[0].1 - 2.0).abs() < 1e-12);
        }
        {
            let portals = crate::state::portals().lock().unwrap();
            assert_eq!(portals.len(), 1);
            assert_eq!(portals[0].id, "p-1");
            assert_eq!(portals[0].from.room, "cave-1");
            assert_eq!(portals[0].from.edge, 2);
            assert!((portals[0].from.range.0 - 0.2).abs() < 1e-12);
            assert!((portals[0].to.range.1 - 1.0).abs() < 1e-12);
        }

        // the canvas node flowed through store + delta
        let v: serde_json::Value =
            serde_json::from_str(&fetch_ui_state_json()).unwrap();
        let canvas = v["nodes"].as_array().unwrap().iter()
            .find(|n| n["id"] == "world-canvas").unwrap();
        assert_eq!(canvas["kind"], "canvas");
        assert_eq!(canvas["options"]["world"]["room"], "cave-1");
        assert_eq!(canvas["options"]["camera"]["zoom"], 2);
        let delta: serde_json::Value = serde_json::from_str(
            &fetch_ui_delta_json().unwrap()).unwrap();
        assert!(delta["ops"].as_array().unwrap().iter().any(|o|
            o["op"] == "add" && o["node"]["id"] == "world-canvas"));

        // FFI-shaped JSON round-trips
        let w: serde_json::Value =
            serde_json::from_str(&crate::state::fetch_rooms_json()).unwrap();
        assert_eq!(w["rooms"][0]["id"], "cave-1");
        assert_eq!(w["portals"][0]["id"], "p-1");
    }

    const FIELD_MODULE: &str = r#"
export default (hostApi) => {
  hostApi.runtime.setEntity(hostApi.runtime.string.of('ent-a'), {
    numberMap: { 'hp': hostApi.runtime.number.of(7) },
    textMap: {}
  });
  hostApi.ui.field('hp-field', {
    entity: 'ent-a',
    map: 'number',
    name: 'hp',
    fallback: 'n/a'
  });
};
"#;

    fn set_entity_number(entity: &str, name: &str, value: f64) {
        let mut data = crate::state::last_entity_number_data().lock().unwrap();
        data.entry(entity.to_string())
            .or_insert_with(HashMap::new)
            .insert(name.to_string(), value);
    }

    #[test]
    fn field_binds_entity_value_and_updates_live() {
        let _g = lock();
        clear();
        crate::state::clear_state();
        set_entity_number("ent-a", "hp", 7.0);

        let dec = crate::js_executor::extract_from_source(FIELD_MODULE).unwrap();
        crate::module::declarations::apply_declarations(&dec, "field-module");
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
    hostApi.ui.container('items', { container: 'items' },
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

    #[test]
    fn container_list_expands_one_item_per_entity() {
        let _g = lock();
        clear();
        crate::state::clear_state();

        let dec = crate::js_executor::extract_from_source(CONTAINER_LIST_MODULE)
            .expect("extraction should succeed");
        assert_eq!(dec.containers.len(), 1, "expected one container declaration");
        crate::module::declarations::apply_declarations(&dec, "container-module");
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
        clear();
        crate::state::clear_state();

        let dec = crate::js_executor::extract_from_source(CONTAINER_LIST_MODULE)
            .unwrap();
        crate::module::declarations::apply_declarations(&dec, "container-module");
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
        clear();
        crate::state::clear_state();
        // A list whose target container was never registered.
        let module = CONTAINER_LIST_MODULE.replace(
            "{ container: 'items' }", "{ container: 'missing' }");
        let dec = crate::js_executor::extract_from_source(&module).unwrap();
        crate::module::declarations::apply_declarations(&dec, "container-module");
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
