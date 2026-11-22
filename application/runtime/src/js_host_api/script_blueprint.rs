/// The stringBlueprint factory as a UMD script. When eval'd by the runtime it
/// registers `globalThis.__stringBlueprintFactory`, which the hostApi builders
/// use to install `runtime.stringBlueprint`. The JS source is the single source
/// of truth (also required directly by the Node unit tests).
pub fn host_api_script_string_blueprint() -> &'static str {
    include_str!("stringBlueprint.js")
}

/// The numberBlueprint factory as a UMD script. When eval'd by the runtime it
/// registers `globalThis.__numberBlueprintFactory`, which the hostApi builders
/// use to install `runtime.numberBlueprint`.
pub fn host_api_script_number_blueprint() -> &'static str {
    include_str!("numberBlueprint.js")
}

/// The entityBlueprint factory as a UMD script. When eval'd by the runtime it
/// registers `globalThis.__entityBlueprintFactory`, which the hostApi builders
/// use to install `runtime.entityBlueprint`.
pub fn host_api_script_entity_blueprint() -> &'static str {
    include_str!("entityBlueprint.js")
}
