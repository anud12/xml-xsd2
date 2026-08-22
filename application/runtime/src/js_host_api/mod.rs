pub mod script_behavior;
pub mod script_emit;
pub mod script_register;
pub mod script_panel_entity;
pub mod script_rest;
mod script_extract;
mod script_extract_map;

use anyhow::Result;
use rquickjs::Context;
use serde::Deserialize;
use std::collections::HashMap;

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
    #[serde(default)]
    pub containers: Vec<String>,
    #[serde(default)]
    pub pending_effects: Vec<String>,
    #[serde(default)]
    pub animations: HashMap<String, serde_json::Value>,
    #[serde(default)]
    pub behavior_definitions: HashMap<String, serde_json::Value>,
    #[serde(default)]
    pub behavior_attachments: HashMap<String, String>,
}

pub fn install_host_api(ctx: &Context) -> Result<()> {
    let script = [
        script_emit::host_api_script_part1(),
        script_emit::host_api_script_emit(),
        script_rest::host_api_script_rest().as_str(),
        script_rest::host_api_script_tail().as_str(),
    ].join("\n");
    ctx.with(|ctx| { ctx.eval::<(), _>(script) })?;
    Ok(())
}

pub fn extract_declarations(
    ctx: &Context,
) -> Result<Declarations> {
    let script =
        script_extract::extract_declarations_script();
    let json =
        ctx.with(|ctx| ctx.eval::<String, _>(script))?;
    let dec: Declarations =
        serde_json::from_str(&json)?;
    Ok(dec)
}
