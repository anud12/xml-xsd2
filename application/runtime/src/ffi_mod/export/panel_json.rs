use std::ffi::{CStr, CString};
use libc::c_char;

fn parse_id(json: &str) -> Option<String> {
    serde_json::from_str::<serde_json::Value>(json)
        .ok()
        .and_then(|v| v.get("id").and_then(|i| i.as_str()).map(|s| s.to_string()))
}

/// Transform a single `__uiHost` node (kind/options shape) into the C#
/// `PanelParser` JSON shape (size/anchor/offset/hover/.../children).
const NODE_TO_PANEL_JS: &str = r#"
var __anchorMap = {
  'top-left': [0,0], 'top': [0.5,0], 'top-right': [1,0],
  'left': [0,0.5], 'center': [0.5,0.5], 'right': [1,0.5],
  'bottom-left': [0,1], 'bottom': [0.5,1], 'bottom-right': [1,1]
};
var __nodeToPanel = function (id, node, all) {
  var o = node.options || {};
  var p = o.panel || {};
  var out = { id: id };
  var hasSurface = node.kind === 'window'
    || p.x !== undefined || p.y !== undefined
    || p.width !== undefined || p.height !== undefined
    || p.background !== undefined || p.onHover !== undefined
    || p.onClick !== undefined || p.anchor !== undefined;
  if (hasSurface) {
    var anchor = [0.5, 0.5];
    if (p.anchor) {
      if (typeof p.anchor === 'string' && __anchorMap[p.anchor]) anchor = __anchorMap[p.anchor];
      else if (typeof p.anchor === 'object') {
        anchor = [p.anchor.x !== undefined ? p.anchor.x : 0.5,
                  p.anchor.y !== undefined ? p.anchor.y : 0.5];
      }
    }
    if (p.background !== undefined) out.background = p.background;
    out.surface = true;
    out.size = { width: p.width || 0, height: p.height || 0 };
    out.anchor = { x: anchor[0], y: anchor[1] };
    out.offset = {
      top: p.y || 0, bottom: 0,
      left: p.x || 0,
      right: p.width ? (p.width - (p.x || 0)) : 0
    };
    if (p.onHover) {
      out.hover = {
        texture: p.onHover.texture !== undefined ? p.onHover.texture : null,
        thickness: p.onHover.thickness !== undefined ? p.onHover.thickness : 0,
        background: p.onHover.background !== undefined ? p.onHover.background : null,
        emitAction: p.onHover.emitAction || null,
        stopPropagation: p.onHover.stopPropagation || false
      };
    }
    if (p.onClick !== undefined) {
      out.onClick = typeof p.onClick === 'string' && p.onClick !== '__jsHandler'
        ? { type: 'emitAction', actionName: p.onClick }
        : { type: 'jsHandler' };
    }
  }
  if (node.kind === 'text') {
    out.content = { type: 'constant', value: node.value };
  }
  if (node.kind === 'image') {
    out.background = node.src;
  }
  if (node.kind === 'field' && node.binding) {
    out.content = {
      type: node.binding.map === 'number' ? 'entityNumberValue' : 'entityTextValue',
      name: node.binding.name,
      entityId: node.binding.entity,
      fallback: node.binding.fallback
    };
  }
  if (p.border && typeof p.border === 'object') {
    out.border = {
      width: p.border.width !== undefined ? p.border.width : 1,
      texture: p.border.texture !== undefined ? p.border.texture : null
    };
  }
  var __containerId = (p.layout && p.layout.container) || o.container;
  if (__containerId !== undefined) out.container = __containerId;
  if (p.layout !== undefined) {
    out.layout = typeof p.layout === 'object' ? p.layout
      : (p.layout === 'row' ? { rowFirst: true } : { rowFirst: false });
  }
  var children = (node.children || []).filter(function (c) {
    return typeof c === 'string' && c.indexOf('$$') !== 0;
  });
  if (children.length > 0) {
    out.children = children.map(function (cid) {
      var found = all[cid];
      if (found) return __nodeToPanel(cid, found, all);
      return cid;
    });
  }
  return out;
};
globalThis.__nodeToPanel = __nodeToPanel;
"#;

#[export_name = "runtime_fetch_panel_json"]
pub extern "C" fn runtime_fetch_panel_json(id: *const c_char) -> *mut c_char {
    if id.is_null() { return std::ptr::null_mut(); }
    let c_str = unsafe { CStr::from_ptr(id) };
    let id = match c_str.to_str() { Ok(s) => s, Err(_) => return std::ptr::null_mut() };

    // Primary source: the persistent sim context's __uiHost node registry.
    if let Some(ctx) = crate::js_executor::sim_ctx::ctx() {
        if let Some(json) = fetch_panel_json_from_sim(ctx, id) {
            return CString::new(json).map(|s| s.into_raw()).unwrap_or_else(|_| std::ptr::null_mut());
        }
    }

    // Fallback: the extracted-declarations panel cache.
    let panels = crate::state::last_panels().lock().unwrap();
    for p in panels.iter() {
        if parse_id(p).as_deref() == Some(id) {
            match CString::new(p.as_str()) {
                Ok(s) => return s.into_raw(),
                Err(_) => return std::ptr::null_mut(),
            }
        }
    }
    std::ptr::null_mut()
}

fn fetch_panel_json_from_sim(
    ctx: &rquickjs::Context,
    id: &str,
) -> Option<String> {
    let id_json = serde_json::to_string(id).unwrap_or_else(|_| "\"\"".to_string());
    let script = format!(
        "{}(function(){{\
          var h = globalThis.__uiHost;\
          if (!h) return 'null';\
          var snap = h.snapshot();\
          var all = {{}};\
          for (var i = 0; i < snap.length; i++) all[snap[i].id] = snap[i];\
          var n = all[{}];\
          if (!n) return 'null';\
          return JSON.stringify(__nodeToPanel({}, n, all));\
        }})()",
        NODE_TO_PANEL_JS, id_json, id_json
    );
    let raw = crate::js_executor::sim_ctx::sim_with(ctx, |c| c.eval::<String, _>(script.as_str())).ok()?;
    if raw == "null" { return None; }
    Some(raw)
}

#[export_name = "runtime_fetch_panel_ids"]
pub extern "C" fn runtime_fetch_panel_ids() -> *mut c_char {
    // Primary source: the persistent sim context's node ids.
    if let Some(ctx) = crate::js_executor::sim_ctx::ctx() {
        if let Some(ids) = fetch_panel_ids_from_sim(ctx) {
            return CString::new(ids).map(|s| s.into_raw()).unwrap_or_else(|_| std::ptr::null_mut());
        }
    }

    // Fallback: the extracted-declarations panel cache.
    let panels = crate::state::last_panels().lock().unwrap();
    let ids: Vec<String> = panels.iter().filter_map(|p| parse_id(p)).collect();
    match serde_json::to_string(&ids) {
        Ok(json) => match CString::new(json) {
            Ok(s) => s.into_raw(),
            Err(_) => std::ptr::null_mut(),
        },
        Err(_) => std::ptr::null_mut(),
    }
}

fn fetch_panel_ids_from_sim(ctx: &rquickjs::Context) -> Option<String> {
    let script = r#"(function(){{
      var h = globalThis.__uiHost;
      if (!h) return '[]';
      var snap = h.snapshot();
      return JSON.stringify(snap.map(function (n) {{ return n.id; }}));
    }})()"#;
    let raw = crate::js_executor::sim_ctx::sim_with(ctx, |c| c.eval::<String, _>(script)).ok()?;
    Some(raw)
}
