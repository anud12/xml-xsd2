use std::collections::HashMap;
use std::ffi::CString;
use super::CreatedByRow;
use super::string_vec_to_c_array;

/// Convert HashMap into C CreatedByRow array.
pub unsafe fn created_by_to_c_array(
    map: HashMap<String, Vec<String>>,
) -> (*mut CreatedByRow, usize) {
    if map.is_empty() {
        return (std::ptr::null_mut(), 0);
    }
    let mut rows: Vec<CreatedByRow> = Vec::with_capacity(map.len());
    for (k, v) in map.into_iter() {
        let key = CString::new(k)
            .unwrap_or_else(|_| CString::new("").unwrap())
            .into_raw();
        let (values_ptr, values_len) = string_vec_to_c_array(v);
        rows.push(CreatedByRow {
            key,
            values_len,
            values: values_ptr,
        });
    }
    let len = rows.len();
    let ptr = Box::into_raw(rows.into_boxed_slice()) as *mut CreatedByRow;
    (ptr, len)
}
