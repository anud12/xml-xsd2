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

/// Bundled TypeScript bridge loaded at compile time.
fn bridge_script() -> &'static str {
    include_str!("js/scripts/bridge.js")
}

/// Wrapper that calls Bridge.install() to populate globals.
fn bridge_host_install_script() -> &'static str {
    include_str!("js/scripts/bridge_host_install.js")
}

/// Wrapper that calls Bridge.serializeDeclarations() and returns JSON.
fn bridge_extract_script() -> &'static str {
    include_str!("js/scripts/bridge_extract.js")
}

/// Install a minimal, explicit host API into the provided QuickJS Context.
pub fn install_host_api(ctx: &Context) -> Result<()> {
    ctx.with(|c| c.eval::<(), _>(bridge_script()))?;
    ctx.with(|c| c.eval::<(), _>(bridge_host_install_script()))?;
    Ok(())
}

/// Inspect the QuickJS global scope and return a JSON-deserializable
/// representation of discovered declarations (events, actions, functions, entities).
pub fn extract_declarations(ctx: &Context) -> Result<Declarations> {
    let json = ctx.with(|c| c.eval::<String, _>(bridge_extract_script()))?;
    let dec: Declarations = serde_json::from_str(&json)?;
    Ok(dec)
}
