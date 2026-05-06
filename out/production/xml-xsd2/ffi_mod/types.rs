use std::ffi::CString;
use libc::{c_char, c_void};
use std::collections::HashMap;

#[repr(C)]
pub struct CStringArray {
    pub len: usize,
    pub data: *mut *mut c_char,
}

#[repr(C)]
pub struct ModuleRow {
    pub id: *mut c_char,
    pub name: *mut c_char,
    pub version: *mut c_char,
}

#[repr(C)]
pub struct ModuleArray {
    pub len: usize,
    pub data: *mut ModuleRow,
}

#[repr(C)]
pub struct FileRow {
    pub filename: *mut c_char,
    pub contents: *mut c_char,
}

#[repr(C)]
pub struct FileArray {
    pub len: usize,
    pub data: *mut FileRow,
}

#[repr(C)]
pub struct CreatedByRow {
    pub key: *mut c_char,
    pub values_len: usize,
    pub values: *mut *mut c_char,
}

#[repr(C)]
pub struct CreatedByArray {
    pub len: usize,
    pub data: *mut CreatedByRow,
}

#[repr(C)]
pub struct ExportedState {
    pub entities: CStringArray,
    pub actions: CStringArray,
    pub events: CStringArray,
    pub panels: PanelArray,
    pub modules: ModuleArray,
    pub files: FileArray,
    pub entity_patterns: CStringArray,
    pub created_by: CreatedByArray,
    pub has_data: bool,
}

/// Helpers for converting Rust Vecs/maps into C-friendly allocated memory.
pub unsafe fn string_vec_to_c_array(vec: Vec<String>) -> (*mut *mut c_char, usize) {
    if vec.is_empty() {
        return (std::ptr::null_mut(), 0);
    }
    let v: Vec<*mut c_char> = vec
        .into_iter()
        .map(|s| CString::new(s).unwrap_or_else(|_| CString::new("").unwrap()).into_raw())
        .collect();
    let len = v.len();
    let boxed = v.into_boxed_slice();
    let ptr = Box::into_raw(boxed) as *mut *mut c_char;
    (ptr, len)
}

pub unsafe fn free_c_string_array(ptr: *mut *mut c_char, len: usize) {
    if ptr.is_null() || len == 0 { return; }
    let slice = std::ptr::slice_from_raw_parts_mut(ptr, len);
    let boxed: Box<[*mut c_char]> = Box::from_raw(slice);
    for &p in boxed.iter() {
        if !p.is_null() { let _ = CString::from_raw(p); }
    }
}

pub unsafe fn module_rows_to_c_array(rows: Vec<Vec<String>>) -> (*mut ModuleRow, usize) {
    if rows.is_empty() { return (std::ptr::null_mut(), 0); }
    let mut out: Vec<ModuleRow> = Vec::with_capacity(rows.len());
    for r in rows.into_iter() {
        let id = CString::new(r.get(0).cloned().unwrap_or_default()).unwrap_or_else(|_| CString::new("").unwrap()).into_raw();
        let name = CString::new(r.get(1).cloned().unwrap_or_default()).unwrap_or_else(|_| CString::new("").unwrap()).into_raw();
        let version = CString::new(r.get(2).cloned().unwrap_or_default()).unwrap_or_else(|_| CString::new("").unwrap()).into_raw();
        out.push(ModuleRow { id, name, version });
    }
    let len = out.len();
    let boxed = out.into_boxed_slice();
    let ptr = Box::into_raw(boxed) as *mut ModuleRow;
    (ptr, len)
}

pub unsafe fn free_module_array(ptr: *mut ModuleRow, len: usize) {
    if ptr.is_null() || len == 0 { return; }
    let slice = std::ptr::slice_from_raw_parts_mut(ptr, len);
    let boxed: Box<[ModuleRow]> = Box::from_raw(slice);
    for m in boxed.iter() {
        if !m.id.is_null() { let _ = CString::from_raw(m.id); }
        if !m.name.is_null() { let _ = CString::from_raw(m.name); }
        if !m.version.is_null() { let _ = CString::from_raw(m.version); }
    }
}

pub unsafe fn free_file_array(ptr: *mut FileRow, len: usize) {
    if ptr.is_null() || len == 0 { return; }
    let slice = std::ptr::slice_from_raw_parts_mut(ptr, len);
    let boxed: Box<[FileRow]> = Box::from_raw(slice);
    for f in boxed.iter() {
        if !f.filename.is_null() { let _ = CString::from_raw(f.filename); }
        if !f.contents.is_null() { let _ = CString::from_raw(f.contents); }
    }
}

pub unsafe fn created_by_to_c_array(map: HashMap<String, Vec<String>>) -> (*mut CreatedByRow, usize) {
    if map.is_empty() { return (std::ptr::null_mut(), 0); }
    let mut rows: Vec<CreatedByRow> = Vec::with_capacity(map.len());
    for (k, v) in map.into_iter() {
        let key = CString::new(k).unwrap_or_else(|_| CString::new("").unwrap()).into_raw();
        let (values_ptr, values_len) = string_vec_to_c_array(v);
        rows.push(CreatedByRow { key, values_len, values: values_ptr });
    }
    let len = rows.len();
    let boxed = rows.into_boxed_slice();
    let ptr = Box::into_raw(boxed) as *mut CreatedByRow;
    (ptr, len)
}

#[repr(C)]
pub struct EntityRow {
    pub text_map_name: *mut c_char,
}

pub type EntityChangeCb = extern "C" fn(*const EntityRow, *mut c_void);

#[repr(C)]
pub struct Subscription {
    pub id: *mut c_char,
    pub cb: Option<EntityChangeCb>,
    pub user_data: *mut c_void,
}

// Unsubscribe callback type and handle returned to callers.
pub type UnsubscribeCb = extern "C" fn(*mut c_void);

#[repr(C)]
pub struct UnsubscribeHandle {
    pub unsub: Option<UnsubscribeCb>,
    pub user_data: *mut c_void,
}

static SUBS_INIT: std::sync::Once = std::sync::Once::new();
static mut ENTITY_SUBSCRIPTIONS: Option<&'static std::sync::Mutex<Vec<*mut Subscription>>> = None;
#[allow(dead_code)]
pub(super) static SUB_ID_COUNTER: std::sync::atomic::AtomicUsize = std::sync::atomic::AtomicUsize::new(1);

pub fn ensure_entity_subscriptions() {
    SUBS_INIT.call_once(|| {
        let v = Box::leak(Box::new(std::sync::Mutex::new(Vec::new())));
        unsafe { ENTITY_SUBSCRIPTIONS = Some(v); }
    });
}

pub fn entity_subscriptions() -> &'static std::sync::Mutex<Vec<*mut Subscription>> {
    ensure_entity_subscriptions();
    unsafe { ENTITY_SUBSCRIPTIONS.expect("entity subs initialized") }
}

// FFI Panel struct exposed to managed clients
#[repr(C)]
pub struct AnchorFfi {
    pub x: f32,
    pub y: f32,
}

#[repr(C)]
pub struct OffsetFfi {
    pub top: f32,
    pub bottom: f32,
    pub left: f32,
    pub right: f32,
}

#[repr(C)]
pub struct SizeFfi {
    pub height: f32,
    pub width: f32,
}

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

pub unsafe fn panels_to_c_array(panels: Vec<String>) -> (*mut PanelFfi, usize) {
    if panels.is_empty() { return (std::ptr::null_mut(), 0); }
    let mut out: Vec<PanelFfi> = Vec::with_capacity(panels.len());
    for p in panels.into_iter() {
        let panel_json_ptr = CString::new(p.clone()).unwrap_or_else(|_| CString::new("").unwrap()).into_raw();
        let ffi = if p.trim_start().starts_with('{') {
            if let Ok(v) = serde_json::from_str::<serde_json::Value>(&p) {
                let id = v["id"].as_str().unwrap_or("").to_string();
                let bg = v["background"].as_str().unwrap_or("").to_string();
                PanelFfi {
                    id: CString::new(id).unwrap_or_else(|_| CString::new("").unwrap()).into_raw(),
                    background: CString::new(bg).unwrap_or_else(|_| CString::new("").unwrap()).into_raw(),
                    anchor: AnchorFfi {
                        x: v["anchor"]["x"].as_f64().unwrap_or(0.0) as f32,
                        y: v["anchor"]["y"].as_f64().unwrap_or(0.0) as f32,
                    },
                    pivot: AnchorFfi {
                        x: v["pivot"]["x"].as_f64().unwrap_or(0.0) as f32,
                        y: v["pivot"]["y"].as_f64().unwrap_or(0.0) as f32,
                    },
                    offset: OffsetFfi {
                        top: v["offset"]["top"].as_f64().unwrap_or(0.0) as f32,
                        bottom: v["offset"]["bottom"].as_f64().unwrap_or(0.0) as f32,
                        left: v["offset"]["left"].as_f64().unwrap_or(0.0) as f32,
                        right: v["offset"]["right"].as_f64().unwrap_or(0.0) as f32,
                    },
                    size: SizeFfi {
                        height: v["size"]["height"].as_f64().unwrap_or(0.0) as f32,
                        width: v["size"]["width"].as_f64().unwrap_or(0.0) as f32,
                    },
                    children_json: std::ptr::null_mut(),
                    panel_json: panel_json_ptr,
                }
            } else {
                PanelFfi {
                    id: CString::new(p.clone()).unwrap_or_else(|_| CString::new("").unwrap()).into_raw(),
                    background: CString::new("").unwrap().into_raw(),
                    anchor: AnchorFfi { x: 0.0, y: 0.0 },
                    pivot: AnchorFfi { x: 0.0, y: 0.0 },
                    offset: OffsetFfi { top: 0.0, bottom: 0.0, left: 0.0, right: 0.0 },
                    size: SizeFfi { height: 0.0, width: 0.0 },
                    children_json: std::ptr::null_mut(),
                    panel_json: panel_json_ptr,
                }
            }
        } else {
            PanelFfi {
                id: CString::new(p.clone()).unwrap_or_else(|_| CString::new("").unwrap()).into_raw(),
                background: CString::new("").unwrap().into_raw(),
                anchor: AnchorFfi { x: 0.0, y: 0.0 },
                pivot: AnchorFfi { x: 0.0, y: 0.0 },
                offset: OffsetFfi { top: 0.0, bottom: 0.0, left: 0.0, right: 0.0 },
                size: SizeFfi { height: 0.0, width: 0.0 },
                children_json: std::ptr::null_mut(),
                panel_json: panel_json_ptr,
            }
        };
        out.push(ffi);
    }
    let len = out.len();
    let ptr = Box::into_raw(out.into_boxed_slice()) as *mut PanelFfi;
    (ptr, len)
}

pub unsafe fn free_panel_array(ptr: *mut PanelFfi, len: usize) {
    if ptr.is_null() || len == 0 { return; }
    let boxed: Box<[PanelFfi]> = Box::from_raw(std::ptr::slice_from_raw_parts_mut(ptr, len));
    for p in boxed.iter() {
        if !p.id.is_null() { let _ = CString::from_raw(p.id); }
        if !p.background.is_null() { let _ = CString::from_raw(p.background); }
        if !p.children_json.is_null() { let _ = CString::from_raw(p.children_json); }
        if !p.panel_json.is_null() { let _ = CString::from_raw(p.panel_json); }
    }
}
