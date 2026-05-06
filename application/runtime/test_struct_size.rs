use std::ffi::{c_char, CString};
use libc::c_double;
use std::mem;

#[repr(C)]
pub struct EntityDataRow {
    pub id: *mut c_char,
    pub text_map_len: usize,
    pub text_map_keys: *mut *mut c_char,
    pub text_map_values: *mut *mut c_char,
    pub number_map_len: usize,
    pub number_map_keys: *mut *mut c_char,
    pub number_map_values: *mut c_double,
}

fn main() {
    println!("EntityDataRow size: {}", mem::size_of::<EntityDataRow>());
    let s = EntityDataRow { id: 0 as _, text_map_len: 0, text_map_keys: 0 as _, text_map_values: 0 as _, number_map_len: 0, number_map_keys: 0 as _, number_map_values: 0 as _ };
    println!("id offset: {}", mem::offset_of!(EntityDataRow, id));
    println!("text_map_len offset: {}", mem::offset_of!(EntityDataRow, text_map_len));
    println!("text_map_keys offset: {}", mem::offset_of!(EntityDataRow, text_map_keys));
    println!("text_map_values offset: {}", mem::offset_of!(EntityDataRow, text_map_values));
    println!("number_map_len offset: {}", mem::offset_of!(EntityDataRow, number_map_len));
    println!("number_map_keys offset: {}", mem::offset_of!(EntityDataRow, number_map_keys));
    println!("number_map_values offset: {}", mem::offset_of!(EntityDataRow, number_map_values));
}
