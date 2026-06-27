use std::ffi::CString;
use std::ptr;
use libc::c_char;

type PanelFfi = crate::ffi_mod::types::PanelFfi;
type AnchorFfi = crate::ffi_mod::types::AnchorFfi;
type OffsetFfi = crate::ffi_mod::types::OffsetFfi;
type SizeFfi = crate::ffi_mod::types::SizeFfi;

pub(crate) fn debug_append(msg: &str) {
    std::fs::OpenOptions::new()
        .create(true).append(true)
        .open("E:\\workspace\\rust_debug_complete.txt")
        .ok()
        .and_then(|mut f| {
            use std::io::Write;
            f.write_all(msg.as_bytes()).ok()
        });
}

pub(crate) fn build_fallback_panel(
    id_ptr: *mut c_char,
    bg_ptr: *mut c_char,
    panel_json: &str,
) -> *mut PanelFfi {
    let pj = CString::new(panel_json)
        .unwrap_or_else(|_| CString::new("").unwrap());
    let ch = CString::new("[]").unwrap();
    let panel = Box::new(PanelFfi {
        id: id_ptr,
        background: bg_ptr,
        anchor: AnchorFfi { x: 0.0, y: 0.0 },
        pivot: AnchorFfi { x: 0.0, y: 0.0 },
        offset: OffsetFfi {
            top: 0.0, bottom: 0.0,
            left: 0.0, right: 0.0,
        },
        size: SizeFfi {
            height: 100.0, width: 100.0,
        },
        children_json: ch.into_raw(),
        panel_json: pj.into_raw(),
    });
    Box::into_raw(panel)
}

pub(crate) fn build_empty_panel(
    id_str: &str
) -> *mut PanelFfi {
    let id_c = CString::new(id_str)
        .unwrap_or_else(|_| CString::new("").unwrap());
    let pj = format!("{{\"id\":\"{}\"}}", id_str);
    let panel_json = CString::new(pj)
        .unwrap_or_else(|_| CString::new("").unwrap());
    let ch = CString::new("[]").unwrap();
    let panel = Box::new(PanelFfi {
        id: id_c.into_raw(),
        background: ptr::null_mut(),
        anchor: AnchorFfi { x: 0.0, y: 0.0 },
        pivot: AnchorFfi { x: 0.0, y: 0.0 },
        offset: OffsetFfi {
            top: 0.0, bottom: 0.0,
            left: 0.0, right: 0.0,
        },
        size: SizeFfi {
            height: 100.0, width: 100.0,
        },
        children_json: ch.into_raw(),
        panel_json: panel_json.into_raw(),
    });
    Box::into_raw(panel)
}
