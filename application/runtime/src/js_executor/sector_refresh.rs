//! Refreshes sector grids from the live container state on every simulation
//! tick. A sector is attached to a container via `setContainer`'s `sector`
//! field; the JS `setContainer` records the latest data per id in
//! `globalThis.__containerData` (latest wins). Reading that map and
//! re-computing the affected grids means a sector created by an effect at
//! runtime takes effect in the grid state on the same tick, and the view
//! picks it up on the next UI tick.

use crate::js_executor::sim_ctx;

/// Read the current sectors (containers carrying a `sector` field) from the
/// live sim context and recompute their grids.
pub fn recompute_sector_grids() {
    let Some(ctx) = sim_ctx::ctx() else {
        return;
    };
    let script = "(function(){\
        var d = globalThis.__containerData || {};\
        var sectors = [];\
        for (var k in d) {\
            var v = d[k];\
            if (v && v.sector) { sectors.push(JSON.stringify({id: k, sector: v.sector})); }\
        }\
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
