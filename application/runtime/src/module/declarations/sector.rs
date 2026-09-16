//! Parse the `sector` declarations captured from JS (one JSON string per
//! attached container: `{"id": <containerId>, "sector": {...}}`) into the
//! sector/portal state, recomputing each affected grid.

use crate::js_host_api::Declarations;
use crate::state::{compute_grid, SectorDeclaration, set_sector_grid};
use serde::Deserialize;
use std::collections::HashMap;

#[derive(Debug, Deserialize)]
struct SectorAttachment {
    id: String,
    sector: SectorAttachmentData,
}

#[derive(Debug, Deserialize)]
struct SectorAttachmentData {
    #[serde(rename = "grid")]
    grid: String,
    #[serde(default)]
    footprint: Vec<[i32; 2]>,
    #[serde(default)]
    at: Option<[i32; 2]>,
    #[serde(default)]
    openings: Vec<OpeningDecl>,
}

#[derive(Debug, Deserialize)]
struct OpeningDecl {
    #[serde(default)]
    cell: [i32; 2],
    #[serde(default)]
    side: String,
    #[serde(default)]
    start: i32,
    #[serde(default)]
    length: i32,
}

pub fn apply_sector_declarations(dec: &Declarations) {
    if dec.sectors.is_empty() {
        return;
    }
    let mut by_grid: HashMap<String, Vec<SectorDeclaration>> = HashMap::new();
    for raw in dec.sectors.iter() {
        let att: SectorAttachment = match serde_json::from_str(raw) {
            Ok(a) => a,
            Err(e) => {
                runtime_log!("sector attach parse error: {}", e);
                continue;
            }
        };
        let at = att.sector.at.unwrap_or([0, 0]);
        let footprint = att.sector.footprint.iter().map(|p| (p[0], p[1])).collect();
        let openings = att
            .sector
            .openings
            .iter()
            .filter_map(|o| {
                let side = crate::state::Side::parse(&o.side)?;
                Some(crate::state::Opening {
                    cell: (o.cell[0], o.cell[1]),
                    side,
                    start: o.start,
                    length: o.length,
                })
            })
            .collect();
        let grid_id = att.sector.grid;
        by_grid
            .entry(grid_id.clone())
            .or_insert_with(Vec::new)
            .push(SectorDeclaration {
                grid: grid_id,
                container: att.id,
                at: (at[0], at[1]),
                footprint,
                openings,
            });
    }
    for (grid_id, decls) in by_grid {
        let state = compute_grid(&grid_id, &decls);
        runtime_log!(
            "sector grid '{}' computed: {} cells, {} portals",
            grid_id,
            state.cells.len(),
            state.portals.len()
        );
        set_sector_grid(state);
    }
}
