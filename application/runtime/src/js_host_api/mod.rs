pub mod script_emit;
pub mod script_register;
pub mod script_panel_entity;
pub mod script_rest;
pub mod script_entity_filter;
pub mod script_behavior;
pub mod script_blueprint;
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
    #[serde(default)]
    pub logs: Vec<String>,
    #[serde(default)]
    pub panels: Vec<String>,
    #[serde(default)]
    pub entity_data: serde_json::Value,
    #[serde(default)]
    pub containers: Vec<String>,
    #[serde(default)]
    pub sectors: Vec<String>,
    #[serde(default)]
    pub pending_effects: Vec<String>,
    #[serde(default)]
    pub animations: HashMap<String, serde_json::Value>,
}

pub fn install_host_api(ctx: &Context) -> Result<()> {
    let script = [
        // Install the shared LXM PRNG (seeded from globalThis.__lxmSeed) as
        // globalThis.__lxmNext, so every module-API random entry point
        // (number.random, randomElement, ...) draws from one coherent seeded
        // stream. It must come first, before any host script that consumes it.
        lxm_prng_script(),
        // Install the shared entity-filter runtime (FilterArg evaluator,
        // entity proxy, condition reader) before the host is assembled so
        // every context's getEntityBy can use it.
        script_entity_filter::host_api_script_entity_filter(),
        // Register the stringBlueprint factory (globalThis.__stringBlueprintFactory)
        // before the host is assembled, so the hostApi builders can install
        // runtime.stringBlueprint.
        script_blueprint::host_api_script_string_blueprint(),
        // Register the numberBlueprint factory (globalThis.__numberBlueprintFactory)
        // so the hostApi builders can install runtime.numberBlueprint.
        script_blueprint::host_api_script_number_blueprint(),
        // Register the entityBlueprint factory (globalThis.__entityBlueprintFactory)
        // so the hostApi builders can install runtime.entityBlueprint.
        script_blueprint::host_api_script_entity_blueprint(),
        script_emit::host_api_script_part1(),
        script_emit::host_api_script_emit(),
        script_rest::host_api_script_rest().as_str(),
        script_rest::host_api_script_tail().as_str(),
    ].join("\n");
    ctx.with(|ctx| { ctx.eval::<(), _>(script) })?;
    Ok(())
}

/// The shared LXM PRNG (LCG + Xorshift + MurmurHash3 mixer) exposed as
/// `globalThis.__lxmNext() -> [0,1)`. The seed is read from
/// `globalThis.__lxmSeed` (a 32-bit value the host sets at install time),
/// falling back to 2147483647 (0x7FFFFFFF, Mersenne M31). Every module-API
/// random entry point draws from this single stream, so the seed governs all
/// module randomness and the sequence is reproducible.
pub fn lxm_prng_script() -> &'static str {
    r#"(function(){
  var s = (typeof globalThis.__lxmSeed === 'number') ? (globalThis.__lxmSeed >>> 0) : 0x7FFFFFFF;
  globalThis.__lxmNext = function() {
    s = (Math.imul(s, 1664525) + 1013904223) & 0xFFFFFFFF;
    var x = s;
    x ^= x << 13; x &= 0xFFFFFFFF;
    x ^= x >>> 17;
    x ^= x << 5; x &= 0xFFFFFFFF;
    x ^= x >>> 16;
    x = Math.imul(x, 0x85ebca6b);
    x ^= x >>> 13;
    x = Math.imul(x, 0xc2b2ae35);
    x ^= x >>> 16;
    return (x >>> 0) / 4294967296;
  };
})();"#
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
