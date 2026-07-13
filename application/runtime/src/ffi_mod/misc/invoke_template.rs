use std::ffi::{CString, c_char};
use anyhow::Result;
use crate::js_runtime::{create_runtime, create_context};

/// Invoke a containerList template function with (entityId, index) and return the result as JSON.
#[no_mangle]
pub extern "C" fn runtime_invoke_template(
    template_source: *const c_char,
    entity_id: *const c_char,
    index: i32,
) -> *mut c_char {
    let source = unsafe {
        if template_source.is_null() {
            return CString::new("{}").unwrap().into_raw();
        }
        std::ffi::CStr::from_ptr(template_source).to_string_lossy().into_owned()
    };

    let eid = unsafe {
        if entity_id.is_null() {
            String::new()
        } else {
            std::ffi::CStr::from_ptr(entity_id).to_string_lossy().into_owned()
        }
    };

    match invoke_template_impl(&source, &eid, index) {
        Ok(json) => {
            CString::new(json).unwrap_or_else(|_| CString::new("{}").unwrap()).into_raw()
        }
        Err(e) => {
            eprintln!("invoke_template error: {}", e);
            CString::new("{}").unwrap().into_raw()
        }
    }
}

fn invoke_template_impl(source: &str, entity_id: &str, index: i32) -> Result<String> {
    let rt = create_runtime()?;
    let ctx = create_context(&rt)?;

    ctx.with(|c| {
        let script = format!(
            r#"(function(){{
                var templateFn = {};
                var result = templateFn("{entity_id}", {index});
                return JSON.stringify(result);
            }})()"#,
            source,
            entity_id = entity_id.replace('\\', "\\\\").replace('"', "\\\""),
            index = index
        );
        let result: String = c.eval(script)?;
        Ok(result)
    })
}
