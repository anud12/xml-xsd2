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
    pub panels: CStringArray,
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
    pub offset: AnchorFfi,
    pub size: SizeFfi,
    // children callback placeholder
    pub children_callback: *mut c_void,
}
