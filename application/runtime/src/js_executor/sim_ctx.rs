//! Persistent simulation context.
//!
//! The module entry is evaluated exactly once, when the archive is
//! processed, in a QuickJS context that lives for the process lifetime.
//! Effect closures and the .ui layer (the `globalThis.__uiHost` node
//! registries, container render lambdas, transport) registered during that
//! evaluation stay live, so iterations and UI ticks run against the same
//! context without re-evaluating the module entry.

use std::collections::HashMap;
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
/// `panel` is the module-facing surface: a positioned/sized/decorated panel
/// becomes a window node, a plain one a division. `__uiEntitiesFor` is the
/// container-list entity lookup the UI ticks use (a list node's
/// `options.container` names a runtime container; unknown containers render
/// zero items).
const UI_PREWIRE: &str = r#"
var __uih = globalThis.__uiHost;
if (__uih && globalThis.host) {
  globalThis.host.ui = globalThis.host.ui || {};
  var __u = globalThis.host.ui;
  // Legacy onClick handlers are functions, which JSON snapshots drop. Keep
  // them in a side table keyed by node id and publish a marker string so the
  // C# side wires input and routes clicks back through runtime_ui_js_click.
  globalThis.__uiClickHandlers = globalThis.__uiClickHandlers || {};
  var __uWin0 = __uih.window, __uDiv0 = __uih.div;
  function __prepClick(id, options, children, inner) {
    var opts = options || {};
    if (typeof opts.onClick === 'function') {
      globalThis.__uiClickHandlers[id] = opts.onClick;
      var copy = {};
      for (var k in opts) {
        if (Object.prototype.hasOwnProperty.call(opts, k)) copy[k] = opts[k];
      }
      copy.onClick = '__jsHandler';
      opts = copy;
    }
    return inner(id, opts, children);
  }
  __uih.window = function (id, options, children) {
    return __prepClick(id, options, children, __uWin0);
  };
  __uih.div = function (id, options, children) {
    return __prepClick(id, options, children, __uDiv0);
  };
  __u.div = __uih.div;
  __u.text = __uih.text;
  __u.window = __uih.window;
  __u.field = __uih.field;
  __u.image = __uih.image;
  __u.canvas = __uih.canvas;
  __u.container = __uih.container;
  __u.panel = function (id, options, children) {
    var opts = options || {};
    var surface = opts.x !== undefined || opts.y !== undefined
      || opts.width !== undefined || opts.height !== undefined
      || opts.background !== undefined || opts.onHover !== undefined
      || opts.onClick !== undefined || opts.anchor !== undefined;
    return surface ? __uih.window(id, opts, children)
                   : __uih.div(id, opts, children);
  };
  __u.spriteMapTIFF = function (mapPath, layers) {
    return {
      kind: 'spriteMap',
      map: mapPath,
      layers: (layers || []).map(function (l) {
        return { layer: l.layer, texture: l.texture };
      })
    };
  };
  globalThis.__uiEntitiesFor = function (name) {
    var snap = __uih.snapshot();
    for (var i = 0; i < snap.length; i++) {
      if (snap[i].id !== name) continue;
      var cid = snap[i].options && snap[i].options.container;
      if (typeof cid !== 'string') return [];
      var list = globalThis.__uiContainerList || [];
      var found = null;
      // The container store appends a row per setContainer call; the latest
      // row for an id wins.
      for (var j = 0; j < list.length; j++) {
        if (list[j].id === cid) found = list[j].entities || [];
      }
      return found || [];
    }
    return [];
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
    eval_entry_in_ctx(&ctx, &source)?;
    // Ensure hostApi has entity.filter for effect closures
    let _ = ctx.with(|c| c.eval::<(), _>(
        "if(globalThis.hostApi&&!globalThis.hostApi.entity){\
         globalThis.hostApi.entity=globalThis.host.entity;}"
    ));
    // The module's own log lines are already surfaced by the extraction
    // pipeline; flushing them here would duplicate them. The install-time
    // entries are dropped by the per-run `__logs` reset in the effect path.
    replace_host(rt, ctx);
    runtime_log!("sim: module installed in persistent context");
    Ok(())
}

/// The persistent context, if a module has been installed.
pub fn ctx() -> Option<&'static Context> {
    unsafe { SIM_HOST.map(|h| &h.ctx) }
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
