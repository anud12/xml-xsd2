use std::mem::offset_of;

#[repr(C)]
struct AnchorFfi {
    x: f32,
    y: f32,
}

#[repr(C)]
struct OffsetFfi {
    top: f32,
    bottom: f32,
    left: f32,
    right: f32,
}

#[repr(C)]
struct SizeFfi {
    height: f32,
    width: f32,
}

#[repr(C)]
struct PanelFfi {
    id: *mut i8,
    background: *mut i8,
    anchor: AnchorFfi,
    pivot: AnchorFfi,
    offset: OffsetFfi,
    size: SizeFfi,
    children_json: *mut i8,
    panel_json: *mut i8,
}

fn main() {
    println!("PanelFfi size: {}", std::mem::size_of::<PanelFfi>());
    println!("offset of id: {}", offset_of!(PanelFfi, id));
    println!("offset of background: {}", offset_of!(PanelFfi, background));
    println!("offset of anchor: {}", offset_of!(PanelFfi, anchor));
    println!("offset of pivot: {}", offset_of!(PanelFfi, pivot));
    println!("offset of offset: {}", offset_of!(PanelFfi, offset));
    println!("offset of size: {}", offset_of!(PanelFfi, size));
    println!("offset of children_json: {}", offset_of!(PanelFfi, children_json));
    println!("offset of panel_json: {}", offset_of!(PanelFfi, panel_json));
}
