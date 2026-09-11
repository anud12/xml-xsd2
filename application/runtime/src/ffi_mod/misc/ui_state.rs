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
        let mut pairs: Vec<(String, f64)> = Vec::new();
        if let Some(args) = em.get("args").and_then(|a| a.as_object()) {
            for (k, v) in args {
                pairs.push((k.clone(), v.as_f64().unwrap_or(0.0)));
            }
        }
        let Some(c_name) = CString::new(name).ok() else { continue; };
        crate::ffi_mod::debug::runtime_debug_simulate_action_args(c_name.as_ptr(), &pairs);
    }
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
