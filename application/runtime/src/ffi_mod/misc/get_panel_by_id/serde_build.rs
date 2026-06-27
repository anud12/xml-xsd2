use libc::c_char;

use super::ParsedFields;
use super::panel_json::{build_panel_json_raw, build_children_json};

type PanelFfi = crate::ffi_mod::types::PanelFfi;
type AnchorFfi = crate::ffi_mod::types::AnchorFfi;
type OffsetFfi = crate::ffi_mod::types::OffsetFfi;
type SizeFfi = crate::ffi_mod::types::SizeFfi;

pub(crate) fn build_panel_from_parsed(
    id_ptr: *mut c_char,
    bg_ptr: *mut c_char,
    panel_json: &str,
    fields: ParsedFields,
) -> *mut PanelFfi {
    let children_json = build_children_json(panel_json);
    let panel_json_raw = build_panel_json_raw(panel_json);

    let panel = Box::new(PanelFfi {
        id: id_ptr,
        background: bg_ptr,
        anchor: AnchorFfi {
            x: fields.ax,
            y: fields.ay,
        },
        pivot: AnchorFfi {
            x: fields.px,
            y: fields.py,
        },
        offset: OffsetFfi {
            top: fields.ot,
            bottom: fields.ob,
            left: fields.ol,
            right: fields.or_val,
        },
        size: SizeFfi {
            height: fields.sh,
            width: fields.sw,
        },
        children_json,
        panel_json: panel_json_raw,
    });
    Box::into_raw(panel)
}
