pub fn host_api_script_log() -> &'static str {
    r#"log(msg) {
        try {
            globalThis.__logs =
                globalThis.__logs || [];
            globalThis.__logs.push(String(msg));
        } catch(e) { }
    }, number: { of: function(n) { return n; } },
    string: { of: function(s) { return s; } },
    texture: { of: function(t) { return t; } }"#
}

pub fn host_api_script_rest() -> String {
    use super::script_register::host_api_script_register_block;
    use super::script_panel_entity::{
        host_api_script_panel,
        host_api_script_create_entity,
        host_api_script_set_entity,
    };
    let mut parts: Vec<String> = Vec::new();
    parts.push(
        host_api_script_register_block("registerEvent"));
    parts.push(
        host_api_script_register_block("registerAction"));
    parts.push(
        host_api_script_register_block("registerEffect"));
    parts.push(host_api_script_panel().to_string());
    parts.push(
        host_api_script_create_entity().to_string());
    parts.push(
        host_api_script_set_entity().to_string());
    parts.push(host_api_script_log().to_string());
    let mut s = parts.join("");
    s.push_str(" }"); // close globalThis.host object
    s
}

pub fn host_api_script_tail() -> &'static str {
    "\n// Provide convenient aliases that scripts \
     sometimes use\nglobalThis.createEntity = \
     function(o) { return globalThis.host.createEntity(o); \
     };\nglobalThis.entity = globalThis.entity || {};\
     \nglobalThis.entity.create = function(o) { \
     return globalThis.host.createEntity(o); \
     };\nfunction string_of(s) { return s; }"
}
