use libc::c_char;

pub mod string_array;
pub mod module_array;
pub mod created_by;
pub mod entity;
pub mod panel_types;
pub mod panel_conversions;

pub use string_array::*;
pub use module_array::*;
pub use created_by::*;
pub use entity::*;
pub use panel_types::*;
pub use panel_conversions::*;

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
