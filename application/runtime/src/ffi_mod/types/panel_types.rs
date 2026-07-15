use std::ffi::CString;
use libc::c_char;

#[repr(C)]
pub struct AnchorFfi { pub x: f32, pub y: f32 }

#[repr(C)]
pub struct OffsetFfi {
    pub top: f32, pub bottom: f32,
    pub left: f32, pub right: f32,
}

#[repr(C)]
pub struct SizeFfi { pub height: f32, pub width: f32 }

#[repr(C)]
pub struct PanelFfi {
    pub id: *mut c_char,
    pub background: *mut c_char,
    pub anchor: AnchorFfi,
    pub pivot: AnchorFfi,
    pub offset: OffsetFfi,
    pub size: SizeFfi,
    pub children_json: *mut c_char,
    pub panel_json: *mut c_char,
}

#[repr(C)]
pub struct PanelArray {
    pub len: usize,
    pub data: *mut PanelFfi,
}

pub unsafe fn free_panel_array(ptr: *mut PanelFfi, len: usize) {
    if ptr.is_null() || len == 0 { return; }
    let slice = std::ptr::slice_from_raw_parts_mut(ptr, len);
    let boxed: Box<[PanelFfi]> = Box::from_raw(slice);
    for p in boxed.iter() {
        if !p.id.is_null() { let _ = CString::from_raw(p.id); }
        if !p.background.is_null() { let _ = CString::from_raw(p.background); }
        if !p.children_json.is_null() { let _ = CString::from_raw(p.children_json); }
        if !p.panel_json.is_null() { let _ = CString::from_raw(p.panel_json); }
    }
}
