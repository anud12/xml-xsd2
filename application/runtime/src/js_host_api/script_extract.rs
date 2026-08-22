use super::script_extract_map::extract_map_items;

pub(super) fn extract_declarations_script() -> String {
    format!(
        r#"(function(){{
            const out = {{
                events: [], actions: [], functions: [],
                entities: [], creators: {{}}, emits: {{}},
                panels: [], entity_data: {{}},
                containers: [], pending_effects: [],
                animations: {{}},
                autonomy_definitions: {{}},
                autonomy_attachments: {{}}
            }};
            {}
            out.logs = globalThis.__logs || [];
            out.functions = Object.getOwnPropertyNames(
                globalThis).filter(k => {{
                    try {{
                        return typeof globalThis[k]
                            === 'function'
                            && !k.startsWith('_')
                            && k !== 'host';
                    }} catch(e) {{ return false; }}
                }}).sort();
            out.creators =
                globalThis.__createdEntitiesFor || {{}};
            out.emits = globalThis.__emitsMap || {{}};
            out.panels =
                globalThis.__registeredPanels || [];
            out.entity_data =
                globalThis.__entityData || {{}};
            out.containers =
                globalThis.__registeredContainers || [];
            const pending =
                globalThis.__pendingEffects || [];
            out.pending_effects =
                pending.map(function(pe) {{
                    return (pe && typeof pe === 'object'
                        && typeof pe.name === 'string')
                        ? pe.name : String(pe);
                }});
            out.animations =
                globalThis.__registeredAnimations || {{}};
            out.autonomy_definitions =
                globalThis.__autonomyDefinitions || {{}};
            var attachments = globalThis.__autonomies || {{}};
            for (var aid in attachments) {{
                out.autonomy_attachments[aid] =
                    attachments[aid].name;
            }}
            return JSON.stringify(out);
        }})()"#,
        extract_map_items()
    )
}
