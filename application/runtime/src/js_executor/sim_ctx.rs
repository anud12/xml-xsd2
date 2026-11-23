//! Persistent simulation context.
//!
//! The module entry is evaluated exactly once, when the archive is
//! processed, in a QuickJS context that lives for the process lifetime.
//! Effect closures and the .ui layer (the `globalThis.__uiHost` node
//! registries, container render lambdas, transport) registered during that
//! evaluation stay live, so iterations and UI ticks run against the same
//! context without re-evaluating the module entry.

use std::collections::HashMap;
use std::sync::Mutex;
use anyhow::Result;
use rquickjs::{Context, Runtime};
use crate::js_runtime::{create_context, create_runtime};
use crate::js_host_api::install_host_api;
use super::sim_entry::{eval_entry_in_ctx, select_entry_source};

/// Transport for the persistent engine: node registrations are no-ops (the
/// engine reads `__uiHost.snapshot()` directly each tick, so collecting them
/// here would accumulate duplicates), while client-state reads resolve to
/// `globalThis.__uiClientState` written through the struct FFI.
const UI_LAYER: &str = r#"
globalThis.__uiTransport = {
  registerNode: function () {},
  emitDelta: function () {},
  readClientState: function () {
    return globalThis.__uiClientState ||
      { clientId: 'local', actor: null, values: {} };
  },
  resolveResource: function (name) { return name; }
};
"#;

/// Live .ui factories on `globalThis.host.ui`, installed before the SHIM
/// fills the rest of the hostApi (its `if(!...)` guards keep these).
/// `window` and `setScreen` are the top-level surface factories (a window,
/// or the single global full-screen screen); `panel` is a plain division. A
/// standalone (window-less, screen-less) node is a no-op — it registers but
/// is never added to the view. `__uiEntitiesFor` is the container-list
/// entity lookup the UI ticks use (a list node's `options.container` names a
/// runtime container; unknown containers render zero items).
const UI_PREWIRE: &str = r#"
var __uih = globalThis.__uiHost;
if (__uih && globalThis.host) {
  globalThis.host.ui = globalThis.host.ui || {};
  var __u = globalThis.host.ui;
  // Legacy onClick handlers are functions, which JSON snapshots drop. Keep
  // them in a side table keyed by node id and publish a marker string so the
  // C# side wires input and routes clicks back through runtime_ui_js_click.
  globalThis.__uiClickHandlers = globalThis.__uiClickHandlers || {};
  // Drag/drop handlers are functions too: stashed the same way so the C# side
  // can route drag-start / drag-end / drop events back through
  // runtime_ui_js_drag_start / _drag_end / _js_drop.
  globalThis.__uiDragHandlers = globalThis.__uiDragHandlers || {};
  globalThis.__uiDropHandlers = globalThis.__uiDropHandlers || {};
  var __uWin0 = __uih.window, __uDiv0 = __uih.div, __uCV0 = __uih.groupView, __uSG0 = __uih.sectorGrid;
  // Each feedback state (onDrag / onHover) carries a `text: { value, font }`
  // whose `font` is a setFont handle (it carries the resolved config on
  // `.__font`). Handles stringify to their bare name in the JSON snapshot, so
  // the renderer would only ever see the name, not the config. Resolve each
  // state's font to the plain config { file, size, color } before forwarding so
  // the C# renderer can load the font. `text.value` is a StringExpression which
  // stringifies to its concrete string on its own, so it is forwarded as is.
  // Returns the feedback unchanged (shared, not copied) when no state has a
  // font to resolve.
  function resolveFeedbackFont(fb) {
    if (!fb || typeof fb !== 'object') return fb;
    function stateFont(st) {
      return st && st.text && typeof st.text === 'object' && st.text.font &&
        typeof st.text.font === 'object' && st.text.font.__font;
    }
    if (!stateFont(fb.onDrag) && !stateFont(fb.onHover)) return fb;
    var c = {};
    for (var k in fb) {
      if (Object.prototype.hasOwnProperty.call(fb, k)) c[k] = fb[k];
    }
    function fixState(st) {
      if (!st || !st.text) return st;
      var t = st.text;
      var sc = {};
      for (var sk in st) {
        if (Object.prototype.hasOwnProperty.call(st, sk)) sc[sk] = st[sk];
      }
      sc.text = { value: t.value, font: (t.font && t.font.__font) ? t.font.__font : t.font };
      return sc;
    }
    if (fb.onDrag) c.onDrag = fixState(fb.onDrag);
    if (fb.onHover) c.onHover = fixState(fb.onHover);
    return c;
  }
  function __prepDragDrop(id, opts, children, inner) {
    var panel = opts.panel || null;
    if (!panel) return inner(id, opts, children);
    var needsCopy = false;
    var pcopy = panel;
    function ensurePcopy() {
      if (needsCopy) return pcopy;
      needsCopy = true;
      var c = {};
      for (var k in panel) {
        if (Object.prototype.hasOwnProperty.call(panel, k)) c[k] = panel[k];
      }
      pcopy = c;
      return c;
    }
    if (panel.droppable && typeof panel.droppable === 'object') {
      // Resolve the drop semantics from the accepted drag type (the onDrop /
      // feedback declared on dragAndDropChannel). The accepted type resolves to
      // its registry handle (globalThis.__uiDragTypes), which carries both.
      // Stash the resolved onDrop behind the "__jsHandler" marker (so the C#
      // fires the drop FFI) and forward the resolved feedback to the renderer.
      // A target only declares which type(s) it accepts — it has no per-target
      // override.
      var dro = panel.droppable;
      var effOnDrop = null;
      var effFeedback = null;
      var accepts = dro.accepts;
      var typeRefs = Array.isArray(accepts) ? accepts : (accepts != null ? [accepts] : null);
      if (typeRefs) {
        for (var ri = 0; ri < typeRefs.length; ri++) {
          var ref = typeRefs[ri];
          var tname = (ref != null && typeof ref === 'object')
            ? (ref.name != null ? ref.name : ref.value) : ref;
          var def = (typeof tname === 'string' && globalThis.__uiDragTypes)
            ? globalThis.__uiDragTypes[tname] : null;
          if (!def) continue;
          if (effOnDrop == null && typeof def.onDrop === 'function') effOnDrop = def.onDrop;
          if (effFeedback == null && def.feedback != null) effFeedback = def.feedback;
        }
      }
      if (effOnDrop != null || effFeedback != null) {
        var d = ensurePcopy();
        var dcopy = {};
        for (var dk in dro) {
          if (Object.prototype.hasOwnProperty.call(dro, dk)) dcopy[dk] = dro[dk];
        }
        if (typeof effOnDrop === 'function') {
          globalThis.__uiDropHandlers[id] = effOnDrop;
          dcopy.onDrop = '__jsHandler';
        }
        if (effFeedback != null) dcopy.feedback = resolveFeedbackFont(effFeedback);
        d.droppable = dcopy;
      }
    }
    if (panel.draggable && typeof panel.draggable === 'object') {
      var src = panel.draggable;
      var hasStart = typeof src.onDragStart === 'function';
      var hasEnd = typeof src.onDragEnd === 'function';
      if (hasStart || hasEnd) {
        var stashed = {};
        if (hasStart) stashed.onDragStart = src.onDragStart;
        if (hasEnd) stashed.onDragEnd = src.onDragEnd;
        globalThis.__uiDragHandlers[id] = stashed;
        var dr = ensurePcopy();
        var drcopy = {};
        for (var drk in src) {
          if (Object.prototype.hasOwnProperty.call(src, drk)) drcopy[drk] = src[drk];
        }
        if (hasStart) drcopy.onDragStart = '__jsHandler';
        if (hasEnd) drcopy.onDragEnd = '__jsHandler';
        dr.draggable = drcopy;
      }
    }
    if (!needsCopy) return inner(id, opts, children);
    var ocopy = {};
    for (var k2 in opts) {
      if (Object.prototype.hasOwnProperty.call(opts, k2)) ocopy[k2] = opts[k2];
    }
    ocopy.panel = pcopy;
    return inner(id, ocopy, children);
  }
  function __prepClick(id, options, children, inner) {
    var opts = options || {};
    var panel = opts.panel || null;
    if (panel && typeof panel.onClick === 'function') {
      globalThis.__uiClickHandlers[id] = panel.onClick;
      var pcopy = {};
      for (var k in panel) {
        if (Object.prototype.hasOwnProperty.call(panel, k)) pcopy[k] = panel[k];
      }
      pcopy.onClick = '__jsHandler';
      var ocopy = {};
      for (var k2 in opts) {
        if (Object.prototype.hasOwnProperty.call(opts, k2)) ocopy[k2] = opts[k2];
      }
      ocopy.panel = pcopy;
      opts = ocopy;
    }
    return inner(id, opts, children);
  }
  // The drag/drop prep runs first so its copies (if any) are the ones
  // __prepClick then reads from; either order works because each only
  // rewrites its own key, but keeping drag/drop first avoids __prepClick
  // rebuilding a fresh panel copy on top of an already-prepared one.
  function __prepBoth(id, options, children, inner) {
    return __prepDragDrop(id, options || {}, children, function (i, o, c) {
      return __prepClick(i, o, c, inner);
    });
  }
  __uih.window = function (id, options, children) {
    return __prepBoth(id, options, children, __uWin0);
  };
  __uih.div = function (id, options, children) {
    return __prepBoth(id, options, children, __uDiv0);
  };
  __uih.groupView = function (name, args, render) {
    return __prepBoth(name, args, render, __uCV0);
  };
  __uih.sectorGrid = function (name, args, render) {
    return __prepBoth(name, args, render, __uSG0);
  };
  __u.div = __uih.div;
  __u.text = __uih.text;
  __u.setFont = __uih.setFont;
  __u.window = __uih.window;
  // setScreen creates the single global full-screen screen (a top-level
  // surface like a window; each call replaces the previous screen).
  __u.setScreen = __uih.setScreen;
  __u.field = __uih.field;
  __u.image = __uih.image;
  __u.progress = __uih.progress;
  __u.canvas = __uih.canvas;
  __u.entityList = __uih.entityList;
  __u.groupView = __uih.groupView;
  __u.sectorGrid = __uih.sectorGrid;
  // panel is a plain flow container (a division): only ui.window creates a
  // window and ui.setScreen the screen. A standalone (window-less,
  // screen-less) node is a no-op — it registers but is never added to the
  // view.
  __u.panel = __uih.div;
  __u.spriteMapTIFF = function (mapPath, layers) {
    return {
      kind: 'spriteMap',
      map: mapPath,
      layers: (layers || []).map(function (l) {
        return { layer: l.layer, texture: l.texture };
      })
    };
  };
  globalThis.__uiEntitiesFor = function (group, list) {
    var data = globalThis.__readEntity(group);
    return (data && data.entityListMap && data.entityListMap[list]) || [];
  };
}
"#;

/// Owns a QuickJS runtime and its context. The context is declared before
/// the runtime so it is dropped first.
pub struct SimHost {
    pub ctx: Context,
    pub rt: Runtime,
}

static mut SIM_HOST: Option<&'static SimHost> = None;

fn replace_host(rt: Runtime, ctx: Context) -> &'static SimHost {
    unsafe {
        if let Some(old) = SIM_HOST {
            drop(Box::from_raw(old as *const SimHost as *mut SimHost));
        }
        let host = Box::leak(Box::new(SimHost { ctx, rt }));
        SIM_HOST = Some(host);
        host
    }
}

/// Evaluate the module entry from `files` once, in a fresh persistent
/// context, keeping the effect closures live for later iterations.
pub fn install(files: &HashMap<String, String>) -> Result<()> {
    let source = select_entry_source(files);
    if source.is_empty() {
        reset();
        return Ok(());
    }
    let rt = create_runtime()?;
    let ctx = create_context(&rt)?;
    // Seed the LXM PRNG (number.random) before the host is built, so the
    // IIFE that initializes the PRNG state reads it. When the host never set a
    // seed this is skipped and the PRNG keeps its 0x7FFFFFFF default.
    if let Some(seed) = crate::state::lxm_seed() {
        ctx.with(|c| c.eval::<(), _>(format!("globalThis.__lxmSeed = {seed};")))
            .map_err(|e| anyhow::anyhow!("lxm seed install failed: {:?}", e))?;
    }
    install_host_api(&ctx)?;
    // .ui layer: transport + node registries + live host.ui factories, all
    // installed before the module entry declares nodes.
    let bundle = [
        UI_LAYER,
        include_str!("../../../ui/ui/host.js"),
        UI_PREWIRE,
    ]
    .join("\n");
    ctx.with(|c| c.eval::<(), _>(bundle))
        .map_err(|e| anyhow::anyhow!("ui layer install failed: {:?}", e))?;
    eval_entry_in_ctx(&ctx, &source)
        .map_err(|e| anyhow::anyhow!("module entry install failed: {:?}", e))?;
    // The module's own log lines are already surfaced by the extraction
    // pipeline; flushing them here would duplicate them. The install-time
    // entries are dropped by the per-run `__logs` reset in the effect path.
    crate::js_host_api::entity_access::install(&ctx)?;
    replace_host(rt, ctx);
    runtime_log!("sim: module installed in persistent context");
    Ok(())
}

/// Install while holding the ctx guard so the persistent-context swap in
/// `replace_host` cannot race an in-flight `sim_with` on the iteration thread.
pub fn install_locked(files: &HashMap<String, String>) -> Result<()> {
    let _guard = CTX_GUARD.lock().unwrap();
    install(files)
}

/// The persistent context, if a module has been installed.
pub fn ctx() -> Option<&'static Context> {
    unsafe { SIM_HOST.map(|h| &h.ctx) }
}

/// The persistent QuickJS `Context` is shared by the background iteration
/// thread (behavior machine, effect dispatch) and the main/UI thread (`.ui`
/// tick, clicks, actor/client-state writes). rquickjs `Context::with` borrows
/// an interior `RefCell`, so two threads calling it at once panic with
/// "RefCell already borrowed". This guard is the single serialization point:
/// every sim-context `with` below goes through `sim_with`, which holds it.
/// `sim_with` is a leaf (it never re-enters itself), so the guard is never
/// held across a nested acquisition and no call deadlocks on it.
static CTX_GUARD: Mutex<()> = Mutex::new(());

/// Run `f` against the shared sim Context while holding `CTX_GUARD`.
pub fn sim_with<R, F>(ctx: &Context, f: F) -> R
where
    F: FnOnce(rquickjs::Ctx<'_>) -> R,
{
    let _guard = CTX_GUARD.lock().unwrap();
    ctx.with(|c| {
        let result = f(c.clone());
        // Registration-time messages are delivered at the next persistent sim
        // invocation (normally the next game-loop tick, at most one tick late).
        // Take them under the guard alongside action/effect/behavior/UI messages.
        drain_ui_messages(&c);
        result
    })
}

/// Take pending messages from either a persistent or fresh QuickJS context.
pub(crate) fn drain_ui_messages(c: &rquickjs::Ctx<'_>) {
    let raw = c.eval::<String, _>("JSON.stringify((function(){var m=globalThis.__pendingUiMessages; globalThis.__pendingUiMessages=[]; return m == null ? [] : m;})())");
    match raw.ok().and_then(|s| serde_json::from_str::<Vec<serde_json::Value>>(&s).ok()) {
        Some(messages) => {
            let mut queue = crate::state::action_messages().lock().unwrap();
            for message in messages {
                if let Some(message) = message.as_str() {
                    queue.push(serde_json::json!({"message": message}));
                } else {
                    runtime_log!("appendErrorMessage: ignoring malformed message");
                }
            }
        }
        None => runtime_log!("appendErrorMessage: ignoring malformed message queue"),
    }
}

/// Stash a typed input payload for the named effect in the persistent sim
/// context, so that when the effect fires, `prepare(context, input)` receives
/// `input` = payload. No-op if no module is installed. Used by the action
/// paths (FFI publish, plan walker) that queue an effect by name and must also
/// carry its payload into the typed `prepare` flow.
pub fn stash_effect_input(name: &str, payload: serde_json::Value) {
    if let Some(ctx) = ctx() {
        let name_json = serde_json::to_string(name).unwrap_or_else(|_| "\"\"".to_string());
        let payload_json = serde_json::to_string(&payload)
            .unwrap_or_else(|_| "{}".to_string());
        let _ = sim_with(ctx, |c| c.eval::<(), _>(format!(
            "globalThis.__effectInputs = globalThis.__effectInputs || {{}};\
             globalThis.__effectInputs[{name_json}] = {payload_json};"
        )));
    }
}

/// Drop the persistent context, if any.
pub fn reset() {
    unsafe {
        if let Some(old) = SIM_HOST {
            SIM_HOST = None;
            drop(Box::from_raw(old as *const SimHost as *mut SimHost));
        }
    }
}
