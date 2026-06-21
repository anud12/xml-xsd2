use anyhow::Result;
use rquickjs::Context;
use super::extract::HOST_API_JS;

pub fn select_entry_source(files: &std::collections::HashMap<String, String>) -> String {
    use serde_json::Value;
    for (name, content) in files.iter() {
        if name.ends_with("manifest.json") ||
            (name.to_lowercase().contains("manifest") && name.ends_with(".json")) {
            if let Ok(v) = serde_json::from_str::<Value>(content) {
                if let Some(entry) = v.get("entry").and_then(|v| v.as_str()) {
                    if let Some(src) = files.get(entry) { return src.clone(); }
                }
            }
        }
    }
    if let Some(src) = files.get("index.js") { return src.clone(); }
    if let Some((_k, v)) = files.iter().next() { return v.clone(); }
    "".to_string()
}

pub fn eval_entry_in_ctx(ctx: &Context, source: &str) -> Result<String> {
    let transformed = if source.contains("export default") {
        source.replace("export default", "var __module_default =")
    } else { source.to_string() };
    ctx.with(|ctx| ctx.eval::<(), _>(transformed.clone()))?;
    if transformed.contains("__module_default") {
        let s = format!("try{{{};if(typeof __module_default==='function')\
            {{__module_default(hostApi);}}}}catch(e){{}}", HOST_API_JS);
        let _ = ctx.with(|ctx| ctx.eval::<(), _>(s));
    }
    Ok(transformed)
}
