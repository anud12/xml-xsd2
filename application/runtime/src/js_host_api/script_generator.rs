/// The stringGenerator factory as a UMD script. When eval'd by the runtime it
/// registers `globalThis.__stringGeneratorFactory`, which the hostApi builders
/// use to install `runtime.stringGenerator`. The JS source is the single source
/// of truth (also required directly by the Node unit tests).
pub fn host_api_script_generator() -> &'static str {
    include_str!("stringGenerator.js")
}
