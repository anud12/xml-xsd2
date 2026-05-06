use std::mem;

#[repr(C)] struct CStringArray { len: usize, data: *mut *mut i8 }
#[repr(C)] struct PanelArray { len: usize, data: *mut u8 }
#[repr(C)] struct ModuleArray { len: usize, data: *mut u8 }
#[repr(C)] struct FileArray { len: usize, data: *mut u8 }
#[repr(C)] struct CreatedByRow { key: *mut i8, values_len: usize, values: *mut *mut i8 }
#[repr(C)] struct CreatedByArray { len: usize, data: *mut CreatedByRow }
#[repr(C)] struct EntityDataRow { id: *mut i8, text_map_len: usize, text_map_keys: *mut *mut i8, text_map_values: *mut *mut i8, number_map_len: usize, number_map_keys: *mut *mut i8, number_map_values_ptr: *mut f64 }
#[repr(C)] struct EntityDataArray { len: usize, data: *mut EntityDataRow }

fn main() {
    println!("EntityDataRow size: {}", std::mem::size_of::<EntityDataRow>());
    println!("EntityDataArray size: {}", std::mem::size_of::<EntityDataArray>());
}
