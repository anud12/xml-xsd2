use std::ffi::CString;
use libc::c_char;
use super::panel_types::*;

fn cstr(s: String) -> *mut c_char {
    CString::new(s).unwrap_or_else(|_| CString::new("").unwrap()).into_raw()
}

pub unsafe fn panels_to_c_array(panels: Vec<String>) -> (*mut PanelFfi, usize) {
    if panels.is_empty() { return (std::ptr::null_mut(), 0); }
    let g = |val: &serde_json::Value| val.as_f64().unwrap_or(0.0) as f32;
    let mut out: Vec<PanelFfi> = Vec::with_capacity(panels.len());
    for p in panels {
        if let Ok(v) = serde_json::from_str::<serde_json::Value>(&p) {
            out.push(PanelFfi {
                id: cstr(v["id"].as_str().unwrap_or("").to_string()),
                background: cstr(v["background"].as_str().unwrap_or("").to_string()),
                anchor: AnchorFfi { x: g(&v["anchor"]["x"]), y: g(&v["anchor"]["y"]) },
                pivot: AnchorFfi { x: g(&v["pivot"]["x"]), y: g(&v["pivot"]["y"]) },
                offset: OffsetFfi {
                    top: g(&v["offset"]["top"]), bottom: g(&v["offset"]["bottom"]),
                    left: g(&v["offset"]["left"]), right: g(&v["offset"]["right"]),
                },
                size: SizeFfi {
                    height: g(&v["size"]["height"]), width: g(&v["size"]["width"]),
                },
                children_json: std::ptr::null_mut(),
                panel_json: cstr(p),
            });
        } else {
            out.push(PanelFfi {
                id: cstr(p.clone()), background: cstr(String::new()),
                anchor: AnchorFfi { x: 0.0, y: 0.0 },
                pivot: AnchorFfi { x: 0.0, y: 0.0 },
                offset: OffsetFfi { top: 0.0, bottom: 0.0, left: 0.0, right: 0.0 },
                size: SizeFfi { height: 0.0, width: 0.0 },
                children_json: std::ptr::null_mut(),
                panel_json: cstr(p),
            });
        }
    }
    let len = out.len();
    (Box::into_raw(out.into_boxed_slice()) as *mut PanelFfi, len)
}
