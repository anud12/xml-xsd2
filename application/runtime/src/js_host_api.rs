use anyhow::Result;
use rquickjs::Context;
use serde::Deserialize;
use std::collections::HashMap;

/// Minimal shape describing declarations discovered in a script/context.
#[derive(Debug, Deserialize, serde::Serialize)]
pub struct Declarations {
    pub events: Vec<String>,
    pub actions: Vec<String>,
    pub creators: HashMap<String, Vec<String>>,
    pub emits: HashMap<String, Vec<String>>,
    pub functions: Vec<String>,
    pub entities: Vec<String>,
    pub logs: Vec<String>,
    pub panels: Vec<String>,
    #[serde(default)]
    pub entity_data: serde_json::Value,
}

/// Host API JavaScript loaded at compile time.
fn host_api_script() -> &'static str {
    include_str!("js/scripts/host_api.js")
}

/// Extract declarations JavaScript loaded at compile time.
fn extract_declarations_script() -> &'static str {
    include_str!("js/scripts/extract_declarations.js")
}

/// Install a minimal, explicit host API into the provided QuickJS Context.
pub fn install_host_api(ctx: &Context) -> Result<()> {
    let script = host_api_script();
    ctx.with(|ctx| { ctx.eval::<(), _>(script) })?;
    Ok(())
}

/// Inspect the QuickJS global scope and return a JSON-deserializable
/// representation of discovered declarations (events, actions, functions, entities).
pub fn extract_declarations(ctx: &Context) -> Result<Declarations> {
    let json = ctx.with(|ctx| ctx.eval::<String, _>(extract_declarations_script()))?;
    let dec: Declarations = serde_json::from_str(&json)?;
    Ok(dec)
}
