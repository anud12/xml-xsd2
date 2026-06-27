use std::ffi::CString;
use libc::c_char;

pub(crate) fn build_panel_json_raw(
    panel_json: &str
) -> *mut c_char {
    let cstr = match CString::new(panel_json) {
        Ok(s) => s,
        Err(_) => {
            let san = panel_json
                .chars().filter(|&c| c != '\0')
                .collect::<String>();
            CString::new(san).unwrap_or_else(
                |_| CString::new("{}").unwrap()
            )
        }
    };
    cstr.into_raw()
}

pub(crate) fn build_children_json(
    panel_json: &str
) -> *mut c_char {
    let v2: serde_json::Value =
        serde_json::from_str(panel_json)
            .unwrap_or(serde_json::Value::Null);
    match v2.get("children") {
        Some(c) => {
            CString::new(c.to_string())
                .unwrap_or_else(
                    |_| CString::new("[]").unwrap()
                ).into_raw()
        }
        None => CString::new("[]")
            .unwrap().into_raw(),
    }
}
