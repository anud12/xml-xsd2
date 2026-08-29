//! UI transport shim + hostApi wiring shared by all four host builders
//! (extract.rs get_host_api_script, extract_invoke, sim_entry, sim_invoke).
//!
//! The .ui layer is engine-agnostic: it only ever sees
//! `globalThis.__uiTransport` + `globalThis.__uiHost`. This module provides
//! the QuickJS-side implementation of the transport seam.

/// Transport stub for module-extraction contexts: node declarations are
/// collected into `globalThis.__uiNodes` and picked up by the extraction
/// script. The persistent UI host (crate::ui) implements the same seam with
/// a real delta sink.
pub fn ui_transport_shim() -> &'static str {
    r#"
globalThis.__uiNodes = globalThis.__uiNodes || [];
globalThis.__uiTransport = {
  registerNode: function (n) { globalThis.__uiNodes.push(n); },
  emitDelta: function (d) { globalThis.__uiLastDelta = d; },
  readClientState: function () {
    return globalThis.__uiClientState ||
      { clientId: 'local', actor: null, values: {} };
  },
  resolveResource: function (name) { return name; }
};
"#
}

/// Wires the .ui factories onto hostApi.ui (called from the invoke scripts,
/// after hostApi is constructed and before the module entrypoint runs).
pub fn host_api_ui_wiring() -> &'static str {
    r#"
if (globalThis.__uiHost && globalThis.hostApi) {
  globalThis.hostApi.ui.div = globalThis.__uiHost.div;
  hostApi.ui.text = globalThis.__uiHost.text;
  if (globalThis.__uiHost.window) hostApi.ui.window = globalThis.__uiHost.window;
  if (globalThis.__uiHost.field) hostApi.ui.field = globalThis.__uiHost.field;
  if (globalThis.__uiHost.image) hostApi.ui.image = globalThis.__uiHost.image;
  if (globalThis.__uiHost.canvas) hostApi.ui.canvas = globalThis.__uiHost.canvas;
  if (globalThis.__uiHost.container) hostApi.ui.container = globalThis.__uiHost.container;
  if (globalThis.__uiHost.setActor) hostApi.ui.setActor = globalThis.__uiHost.setActor;
}
if (globalThis.hostApi) {
  globalThis.hostApi.ui.spriteMapTIFF = function (mapPath, layers) {
    return {
      kind: 'spriteMap',
      map: mapPath,
      layers: (layers || []).map(function (l) {
        return { layer: l.layer, texture: l.texture };
      })
    };
  };
  if (globalThis.hostApi.runtime &&
      typeof globalThis.hostApi.runtime.getAnimation === 'function') {
    globalThis.hostApi.ui.getAnimation =
        globalThis.hostApi.runtime.getAnimation;
  }
}
"#
}
