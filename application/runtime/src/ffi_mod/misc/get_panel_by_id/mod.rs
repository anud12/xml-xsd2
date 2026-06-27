// Shared types for panel JSON deserialization
use serde::Deserialize;

#[derive(Deserialize)]
#[allow(dead_code)]
pub(crate) struct JsPanel {
    id: Option<String>,
    anchor: Option<Anchor>,
    pivot: Option<Anchor>,
    offset: Option<Anchor>,
    size: Option<Size>,
    background: Option<String>,
}

#[derive(Deserialize, Clone)]
pub(crate) struct Anchor {
    x: Option<f32>,
    y: Option<f32>,
    top: Option<f32>,
    bottom: Option<f32>,
    left: Option<f32>,
    right: Option<f32>,
}

#[derive(Deserialize, Clone)]
pub(crate) struct Size {
    height: f32,
    width: f32,
}

/// Parsed numeric fields from a JsPanel for FFI construction.
#[allow(dead_code)]
pub(crate) struct ParsedFields {
    pub ax: f32, pub ay: f32,
    pub px: f32, pub py: f32,
    pub ot: f32, pub ob: f32,
    pub ol: f32, pub or_val: f32,
    pub sh: f32, pub sw: f32,
}

pub mod allocate;
pub mod narrow;
#[cfg(target_os = "windows")]
pub mod wide;
pub mod panel_helpers;
pub mod json_extract;
pub mod parse_fields;
pub mod panel_json;
pub mod serde_build;
pub mod struct_lookup;
pub mod test_panel;

pub use narrow::get_panel_by_id_c;
#[cfg(target_os = "windows")]
pub use wide::get_panel_by_id_wide;
pub use narrow::get_panel_by_id;
pub use struct_lookup::get_panel_by_id_struct;
pub use test_panel::test_pointer_return;
pub use test_panel::get_test_panel_fixed;
