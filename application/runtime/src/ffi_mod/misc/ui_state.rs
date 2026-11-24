//! FFI transport seam for the .ui layer.
//!
//! The C# client pulls the UI tree / id-keyed deltas through these
//! functions. State and delta cross the boundary as binary POD slabs
//! (`crate::ui::abi`), not JSON; the C# side reads the structs directly and
//! releases them through the matching `runtime_free_ui_*` functions.

use libc::c_char;
use std::ffi::{CStr, CString};

fn to_c_string(s: String) -> *mut c_char {
    match CString::new(s) {
        Ok(cs) => cs.into_raw(),
        Err(_) => std::ptr::null_mut(),
    }
}

/// Full UI tree as one binary slab (for initial paint): a `UiSnapshot`
/// header at offset 0, node/animation struct regions, then a NUL-terminated
/// string arena. Released with `runtime_free_ui_snapshot`.
#[no_mangle]
pub extern "C" fn runtime_fetch_ui_state() -> *mut crate::ui::abi::UiSnapshot {
    crate::ui::tick();
    let nodes = crate::ui::ui_nodes().lock().unwrap().clone();
    let animations = crate::ui::animations().lock().unwrap().clone();
    crate::ui::abi::build_snapshot(&nodes, &animations)
}

/// Release a slab returned by `runtime_fetch_ui_state`.
#[no_mangle]
pub extern "C" fn runtime_free_ui_snapshot(ptr: *mut crate::ui::abi::UiSnapshot) {
    unsafe { crate::ui::abi::free_snapshot(ptr) };
}

/// Pending delta as one binary slab (a `UiDelta` header at offset 0, op
/// struct regions, then a string arena), or null when clean. Fetching
/// consumes the pending delta. Released with `runtime_free_ui_delta`.
#[no_mangle]
pub extern "C" fn runtime_fetch_ui_delta() -> *mut crate::ui::abi::UiDelta {
    crate::ui::tick();
    let Some(delta) = crate::ui::ui_delta().lock().unwrap().take() else {
        return std::ptr::null_mut();
    };
    crate::ui::abi::build_delta(&delta.ops)
}

/// Release a slab returned by `runtime_fetch_ui_delta`.
#[no_mangle]
pub extern "C" fn runtime_free_ui_delta(ptr: *mut crate::ui::abi::UiDelta) {
    unsafe { crate::ui::abi::free_delta(ptr) };
}

/// Registered animation definitions as JSON (`{name: {frames:[...]}}`).
/// Consumers advance frames themselves using the elapsed time units.
#[no_mangle]
pub extern "C" fn runtime_fetch_ui_animations() -> *mut c_char {
    let map = crate::ui::animations().lock().unwrap().clone();
    to_c_string(serde_json::to_string(&map).unwrap_or_default())
}

/// Set the acting actor id on the client state the .ui layer reads through
/// `__uiTransport.readClientState()` (written into the persistent sim
/// context).
#[no_mangle]
pub extern "C" fn runtime_set_actor(actor: *const c_char) {
    if actor.is_null() { return; }
    let Some(ctx) = crate::js_executor::sim_ctx::ctx() else { return; };
    let Ok(actor) = unsafe { CStr::from_ptr(actor) }.to_str() else { return; };
    let json = serde_json::to_string(actor).unwrap_or_else(|_| "\"\"".to_string());
    let _ = crate::js_executor::sim_ctx::sim_with(ctx, |c| c.eval::<(), _>(format!(
        "globalThis.__uiClientState = globalThis.__uiClientState || \
         {{ clientId: 'local', actor: null, values: {{}} }};\n\
         globalThis.__uiClientState.actor = {};",
        json
    )));
}

/// Dispatch synchronously in the shared UI context. Calls from the UI thread
/// complete in emission order; sim_with serializes them with snapshot ticks.
#[no_mangle]
pub extern "C" fn runtime_ui_hover_event(action_kind: u32, node_id: *const c_char, window_id: *const c_char, x: f32, y: f32, left: f32, top: f32, width: f32, height: f32, has_cell: i32, col: i32, row: i32) {
    let action = match action_kind { 0 => "ui:hover-window:enter", 1 => "ui:hover-window:exit", 2 => "ui:hover-window:move", _ => return };
    if node_id.is_null() || window_id.is_null() { return; }
    let (Ok(node), Ok(window)) = (unsafe { CStr::from_ptr(node_id) }.to_str(), unsafe { CStr::from_ptr(window_id) }.to_str()) else { return; };
    let Some(ctx) = crate::js_executor::sim_ctx::ctx() else { return; };
    let args = serde_json::json!({"node": node, "window": window, "cursor": {"x": x, "y": y}, "rect": {"x": left, "y": top, "width": width, "height": height}, "cell": if has_cell != 0 { Some(serde_json::json!({"x": col, "y": row})) } else { None }});
    let script = format!("globalThis.__uiHost.hoverWindowEvent({}, {});", serde_json::to_string(action).unwrap(), args);
    if let Err(e) = crate::js_executor::sim_ctx::sim_with(ctx, |c| c.eval::<(), _>(script)) {
        runtime_log!("ui: hover event failed: {:?}", e);
    }
}

/// Synchronize pin ownership with the host under the same guard as hover events.
#[no_mangle]
pub extern "C" fn runtime_ui_hover_pinned(pinned: u32, node_id: *const c_char, window_id: *const c_char) {
    if node_id.is_null() || window_id.is_null() { return; }
    let (Ok(node), Ok(window)) = (unsafe { CStr::from_ptr(node_id) }.to_str(), unsafe { CStr::from_ptr(window_id) }.to_str()) else { return; };
    let Some(ctx) = crate::js_executor::sim_ctx::ctx() else { return; };
    let args = serde_json::json!({"node": node, "window": window, "pinned": pinned != 0});
    let script = format!("globalThis.__uiHost.hoverWindowPinned({});", args);
    if let Err(e) = crate::js_executor::sim_ctx::sim_with(ctx, |c| c.eval::<(), _>(script)) {
        runtime_log!("ui: hover pin failed: {:?}", e);
    }
}

/// Route a click on a node whose legacy `onClick` handler is a JS function
/// (captured by the sim-context pre-wire behind the `"__jsHandler"` marker):
/// invokes the handler with a click ctx (`emitAction`, `cursor`) and
/// dispatches every action the handler emits.
#[no_mangle]
pub extern "C" fn runtime_ui_js_click(id: *const c_char, col: i32, row: i32) {
    if id.is_null() { return; }
    let Some(ctx) = crate::js_executor::sim_ctx::ctx() else { return; };
    let Ok(id) = unsafe { CStr::from_ptr(id) }.to_str() else { return; };
    let id_json = serde_json::to_string(id).unwrap_or_else(|_| "\"\"".to_string());
    let script = format!(
        "(function(){{\
         globalThis.__uiClickEmissions = [];\
         var h = (globalThis.__uiClickHandlers || {{}})[{id_json}];\
         if (typeof h !== 'function') return '[]';\
         var ctx = {{\
           emitAction: function(name, args){{\
             var n = typeof name === 'object' && name !== null ? name.value : name;\
             globalThis.__uiClickEmissions.push({{ name: n, args: args || {{}} }});\
           }},\
           cursor: {{ getX: function(){{ return {col}; }}, getY: function(){{ return {row}; }} }}\
         }};\
         h(ctx);\
         return JSON.stringify(globalThis.__uiClickEmissions);\
       }})()"
    );
    let Ok(raw) = crate::js_executor::sim_ctx::sim_with(ctx, |c| c.eval::<String, _>(script)) else { return; };
    let Ok(emissions) = serde_json::from_str::<Vec<serde_json::Value>>(&raw) else { return; };
    for em in emissions {
        let Some(name) = em.get("name").and_then(|n| n.as_str()) else { continue; };
        let mut pairs: Vec<(String, serde_json::Value)> = Vec::new();
        if let Some(args) = em.get("args").and_then(|a| a.as_object()) {
            for (k, v) in args {
                pairs.push((k.clone(), v.clone()));
            }
        }
        let Some(c_name) = CString::new(name).ok() else { continue; };
        crate::ffi_mod::debug::runtime_debug_simulate_action_args(c_name.as_ptr(), &pairs);
    }
}

/// Run a UI handler script in the sim context and parse the collected
/// `globalThis.__uiClickEmissions` into `(name, [(key, f64)])` pairs ready
/// for `runtime_debug_simulate_action_args`. Shared by the click / drop /
/// drag-start / drag-end paths so all of them flow through the exact same
/// dispatch as clicks.
fn eval_js_emissions(script: &str) -> Vec<(String, Vec<(String, serde_json::Value)>)> {
    let Some(ctx) = crate::js_executor::sim_ctx::ctx() else { return Vec::new(); };
    let Ok(raw) = crate::js_executor::sim_ctx::sim_with(ctx, |c| c.eval::<String, _>(script)) else { return Vec::new(); };
    let Ok(emissions) = serde_json::from_str::<Vec<serde_json::Value>>(&raw) else { return Vec::new(); };
    let mut out = Vec::new();
    for em in emissions {
        let Some(name) = em.get("name").and_then(|n| n.as_str()) else { continue; };
        let mut pairs: Vec<(String, serde_json::Value)> = Vec::new();
        if let Some(args) = em.get("args").and_then(|a| a.as_object()) {
            for (k, v) in args {
                // Carry the value verbatim (string / number / bool / object) so
                // the action's `ctx.args` and the effect's `__actionArgs` see
                // the exact types the UI handler emitted. Coercing to f64 here
                // destroyed string args (e.g. entityId: "1" → 0).
                pairs.push((k.clone(), v.clone()));
            }
        }
        out.push((name.to_string(), pairs));
    }
    out
}

/// Build the script body that fetches a handler from `globalThis.{table}[id]`
/// (a bare function, as with the click / drop paths) and invokes it with the
/// given context literal.
fn build_handler_script(table: &str, id: &str, ctx_js: &str) -> String {
    let id_json = serde_json::to_string(id).unwrap_or_else(|_| "\"\"".to_string());
    format!(
        "(function(){{\
         globalThis.__uiClickEmissions = [];\
         var t = globalThis.{table} || {{}};\
         var h = t[{id_json}];\
         if (typeof h !== 'function') return '[]';\
         h({ctx_js});\
         return JSON.stringify(globalThis.__uiClickEmissions);\
       }})()"
    )
}

/// Build the script body that fetches a method (`onDragStart` / `onDragEnd`)
/// from `globalThis.__uiDragHandlers[id]` (an object keyed by node id) and
/// invokes it with the given context literal.
fn build_drag_handler_script(id: &str, method: &str, ctx_js: &str) -> String {
    let id_json = serde_json::to_string(id).unwrap_or_else(|_| "\"\"".to_string());
    format!(
        "(function(){{\
         globalThis.__uiClickEmissions = [];\
         var t = globalThis.__uiDragHandlers || {{}};\
         var entry = t[{id_json}];\
         var h = entry && typeof entry.{method} === 'function' ? entry.{method} : null;\
         if (!h) return '[]';\
         h({ctx_js});\
         return JSON.stringify(globalThis.__uiClickEmissions);\
       }})()"
    )
}

/// The `emitAction` JS literal shared by every UI event context. Mirrors the
/// click handler: normalizes the action reference (handle or string) and
/// pushes to the emissions collector.
const EMIT_ACTION_JS: &str =
    "emitAction: function(name, args){\
     var n = typeof name === 'object' && name !== null ? name.value : name;\
     globalThis.__uiClickEmissions.push({ name: n, args: args || {} });\
    }";

/// Convert a NUL-terminated C string to a JSON-quoted string literal, or
/// `None` when the pointer is null / not valid UTF-8.
fn cstr_json(p: *const c_char) -> Option<String> {
    if p.is_null() { return None; }
    unsafe { CStr::from_ptr(p) }.to_str().ok().map(|s| serde_json::to_string(s).unwrap_or_else(|_| "\"\"".to_string()))
}

/// Convert a NUL-terminated C string to a raw JSON object (e.g. `{}` or
/// `{"a":1}`), returning `fallback` when the pointer is null, not UTF-8, or
/// not a valid JSON value. Used for the `data` / `payload` JSON blobs that
/// drag/drop events carry across the boundary.
fn cstr_json_object(p: *const c_char, fallback: &str) -> String {
    if p.is_null() { return fallback.to_string(); }
    if let Ok(s) = unsafe { CStr::from_ptr(p) }.to_str() {
        if serde_json::from_str::<serde_json::Value>(s).is_ok() {
            return s.to_string();
        }
    }
    fallback.to_string()
}

fn dispatch_emissions(emissions: &[(String, Vec<(String, serde_json::Value)>)]) {
    for (name, pairs) in emissions {
        let Ok(c_name) = CString::new(name.as_str()) else { continue; };
        crate::ffi_mod::debug::runtime_debug_simulate_action_args(c_name.as_ptr(), pairs);
    }
}

/// Route a drop on a node whose `droppable.onDrop` is a JS function (stashed
/// in `globalThis.__uiDropHandlers` behind the `"__jsHandler"` marker). Builds
/// a `UiDropContext` (`item`, optional `cursor`, `emitAction`, `targetId`),
/// invokes the handler, dispatches every emitted action.
#[no_mangle]
pub extern "C" fn runtime_ui_js_drop(
    id: *const c_char,
    drop_type: *const c_char,
    entity_id: *const c_char,
    source_container_id: *const c_char,
    has_cursor: i32,
    cursor_x: i32,
    cursor_y: i32,
    cell_x: i32,
    cell_y: i32,
    data_json: *const c_char,
) {
    if id.is_null() { return; }
    let Ok(id) = unsafe { CStr::from_ptr(id) }.to_str() else { return; };
    let drop_type_json = cstr_json(drop_type).unwrap_or_else(|| "\"\"".to_string());
    let entity_json = cstr_json(entity_id);
    let container_json = cstr_json(source_container_id);
    let data_json = cstr_json_object(data_json, "{}");
    let cursor_js = if has_cursor != 0 {
        format!(
            "getX: function(){{ return {cursor_x}; }}, \
             getY: function(){{ return {cursor_y}; }}, \
             getCellX: function(){{ return {cell_x}; }}, \
             getCellY: function(){{ return {cell_y}; }}"
        )
    } else {
        String::new()
    };
    let item_fields = format!(
        "dragType: {drop_type_json},{}",
        entity_json
            .as_deref()
            .map(|e| format!("entityId: {e},"))
            .unwrap_or_default()
    );
    let item_fields = format!(
        "{item_fields}{}",
        container_json
            .as_deref()
            .map(|c| format!("sourceContainerId: {c},"))
            .unwrap_or_default()
    );
    let item_fields = format!(
        "{item_fields}data: {data_json}"
    );
    let id_json = serde_json::to_string(id).unwrap_or_else(|_| "\"\"".to_string());
    let cursor_field = if has_cursor != 0 {
        format!(",cursor: {{{cursor_js}}}")
    } else {
        String::new()
    };
    let ctx_js = format!(
        "{{ item: {{{item_fields}}} {cursor_field} ,\
          {EMIT_ACTION_JS},\
          targetId: {id_json}\
        }}"
    );
    let script = build_handler_script("__uiDropHandlers", id, &ctx_js);
    dispatch_emissions(&eval_js_emissions(&script));
}

/// Route a drag-start on a node whose `draggable.onDragStart` is a JS
/// function (stashed in `globalThis.__uiDragHandlers[id].onDragStart` behind
/// the `"__jsHandler"` marker). Builds a `UiDragContext` (`dragType`,
/// `item`, optional `cursor`, `emitAction`), invokes it, dispatches actions.
#[no_mangle]
pub extern "C" fn runtime_ui_js_drag_start(
    id: *const c_char,
    drag_type: *const c_char,
    payload_json: *const c_char,
    has_cursor: i32,
    cursor_x: i32,
    cursor_y: i32,
) {
    if id.is_null() { return; }
    let Ok(id) = unsafe { CStr::from_ptr(id) }.to_str() else { return; };
    let drag_type_json = cstr_json(drag_type).unwrap_or_else(|| "\"\"".to_string());
    let payload_json = cstr_json_object(payload_json, "{}");
    // The payload (DraggablePayload) is `{ entityId?, sourceContainerId?, data? }`.
    // The UiDragContext's `item` (DroppedItem) is `{ dragType, entityId?,
    // sourceContainerId?, data? }` — so we lift entityId/sourceContainerId from
    // the payload and add dragType.
    let cursor_js = if has_cursor != 0 {
        format!(
            "getX: function(){{ return {cursor_x}; }}, \
             getY: function(){{ return {cursor_y}; }}"
        )
    } else {
        String::new()
    };
    let cursor_field = if has_cursor != 0 {
        format!(",cursor: {{{cursor_js}}}")
    } else {
        String::new()
    };
    // Build item from payload + dragType. `payload` is `{ entityId?, sourceContainerId?, data? }`.
    let item_builder_js = format!(
        "(function(){{\
         var p = {payload_json};\
         var item = {{ dragType: {drag_type_json} }};\
         if (p && typeof p === 'object') {{\
           if (p.entityId !== undefined) item.entityId = p.entityId;\
           if (p.sourceContainerId !== undefined) item.sourceContainerId = p.sourceContainerId;\
           item.data = p.data || {{}};\
         }} else {{ item.data = {{}}; }}\
         return item;\
       }})()"
    );
    let ctx_js = format!(
        "{{ dragType: {drag_type_json}, item: {item_builder_js} {cursor_field} ,\
          {EMIT_ACTION_JS}\
        }}"
    );
    let script = build_drag_handler_script(id, "onDragStart", &ctx_js);
    dispatch_emissions(&eval_js_emissions(&script));
}

/// Route a drag-end on a node whose `draggable.onDragEnd` is a JS function
/// (stashed in `globalThis.__uiDragHandlers[id].onDragEnd`). Builds a
/// `UiDragEndContext` (extends `UiDragContext` with `dropped`, `targetId?`),
/// invokes it, dispatches actions.
#[no_mangle]
pub extern "C" fn runtime_ui_js_drag_end(
    id: *const c_char,
    drag_type: *const c_char,
    dropped: i32,
    target_id: *const c_char,
    payload_json: *const c_char,
    has_cursor: i32,
    cursor_x: i32,
    cursor_y: i32,
) {
    if id.is_null() { return; }
    let Ok(id) = unsafe { CStr::from_ptr(id) }.to_str() else { return; };
    let drag_type_json = cstr_json(drag_type).unwrap_or_else(|| "\"\"".to_string());
    let target_json = cstr_json(target_id);
    let payload_json = cstr_json_object(payload_json, "{}");
    let cursor_js = if has_cursor != 0 {
        format!(
            "getX: function(){{ return {cursor_x}; }}, \
             getY: function(){{ return {cursor_y}; }}"
        )
    } else {
        String::new()
    };
    let cursor_field = if has_cursor != 0 {
        format!(",cursor: {{{cursor_js}}}")
    } else {
        String::new()
    };
    let item_builder_js = format!(
        "(function(){{\
         var p = {payload_json};\
         var item = {{ dragType: {drag_type_json} }};\
         if (p && typeof p === 'object') {{\
           if (p.entityId !== undefined) item.entityId = p.entityId;\
           if (p.sourceContainerId !== undefined) item.sourceContainerId = p.sourceContainerId;\
           item.data = p.data || {{}};\
         }} else {{ item.data = {{}}; }}\
         return item;\
       }})()"
    );
    let dropped_lit = if dropped != 0 { "true" } else { "false" };
    let target_field = match target_json {
        Some(t) => format!(",targetId: {t}"),
        None => String::new(),
    };
    let ctx_js = format!(
        "{{ dragType: {drag_type_json}, item: {item_builder_js} {cursor_field} ,\
          {EMIT_ACTION_JS},\
          dropped: {dropped_lit}{target_field}\
        }}"
    );
    let script = build_drag_handler_script(id, "onDragEnd", &ctx_js);
    dispatch_emissions(&eval_js_emissions(&script));
}

/// Set client-state values from a `UiArgPair` array + string arena. A pair's
/// `vt` selects the JS value type: UI_ARG_NUMBER values are stored as
/// numbers, everything else as strings.
#[no_mangle]
pub extern "C" fn runtime_set_client_values(
    count: u32,
    pairs: *const crate::ui::abi::UiArgPair,
    arena: *const u8,
) {
    if count == 0 || pairs.is_null() || arena.is_null() { return; }
    let Some(ctx) = crate::js_executor::sim_ctx::ctx() else { return; };
    let pairs = unsafe { std::slice::from_raw_parts(pairs, count as usize) };
    let mut script = String::from(
        "globalThis.__uiClientState = globalThis.__uiClientState || \
         { clientId: 'local', actor: null, values: {} };\n\
         globalThis.__uiClientState.values = {\n");
    for (i, p) in pairs.iter().enumerate() {
        let key = if p.key == crate::ui::abi::NO_STR {
            String::new()
        } else {
            arena_str(arena, p.key)
        };
        let value = pair_value_json(arena, p.value, p.vt);
        script.push_str(&format!(
            "  {}: {}{}\n",
            serde_json::to_string(&key).unwrap_or_else(|_| "\"\"".to_string()),
            value,
            if i + 1 < pairs.len() { "," } else { "" }
        ));
    }
    script.push_str("};");
    let _ = crate::js_executor::sim_ctx::sim_with(ctx, |c| c.eval::<(), _>(script));
}

fn arena_str(arena: *const u8, offset: u32) -> String {
    unsafe {
        CStr::from_ptr(arena.add(offset as usize) as *const c_char)
            .to_string_lossy().into_owned()
    }
}

fn pair_value_json(arena: *const u8, offset: u32, vt: u8) -> String {
    if offset == crate::ui::abi::NO_STR {
        return if vt == crate::ui::abi::UI_ARG_NUMBER {
            "0".to_string()
        } else {
            "\"\"".to_string()
        };
    }
    let s = arena_str(arena, offset);
    if vt == crate::ui::abi::UI_ARG_NUMBER {
        if let Ok(n) = s.parse::<f64>() {
            if n.fract() == 0.0 && n.abs() < 9.007199254740992e15 {
                return format!("{}", n as i64);
            }
            return format!("{}", n);
        }
    }
    serde_json::to_string(&s).unwrap_or_else(|_| "\"\"".to_string())
}
