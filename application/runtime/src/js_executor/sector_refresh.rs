//! Refresh grids from live sector declarations.

use crate::js_executor::sim_ctx;

/// Read the current sector declarations from the
/// live sim context and recompute their grids.
pub fn recompute_sector_grids() {
    let Some(ctx) = sim_ctx::ctx() else {
        return;
    };
    let script = "(function(){\
        var sectors = globalThis.__registeredSectors || [];\
        var l = globalThis.__portalLinks || [];\
        var links = [];\
        for (var i = 0; i < l.length; i++) { links.push(JSON.stringify(l[i])); }\
        return JSON.stringify({sectors: sectors, links: links});\
    })()";
    let raw_json: String = match sim_ctx::sim_with(ctx, |c| c.eval::<String, _>(script)) {
        Ok(s) => s,
        Err(e) => {
            runtime_log!("sector refresh: read failed: {:?}", e);
            return;
        }
    };
    #[derive(serde::Deserialize)]
    struct Refresh {
        #[serde(default)]
        sectors: Vec<String>,
        #[serde(default)]
        links: Vec<String>,
    }
    let refresh: Refresh = match serde_json::from_str(&raw_json) {
        Ok(v) => v,
        Err(e) => {
            runtime_log!("sector refresh: parse failed: {}", e);
            return;
        }
    };
    crate::module::declarations::recompute_sector_grids_from(&refresh.sectors, &refresh.links);
}
