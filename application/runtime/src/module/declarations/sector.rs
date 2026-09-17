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
    #[serde(default)]
    links: Vec<LinkDecl>,
}

#[derive(Debug, Deserialize)]
struct LinkDecl {
    a: LinkEndDecl,
    b: LinkEndDecl,
}

/// One endpoint of an explicit link: the target container, its local cell, and
/// the side. Parsed straight from the JS `sector.links` object.
#[derive(Debug, Deserialize)]
struct LinkEndDecl {
    container: String,
    cell: [i32; 2],
    side: String,
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
    recompute_sector_grids_from(&dec.sectors, &[]);
}

/// One `linkOpening(a, b)` payload captured from JS: `{a: {container, cell,
/// side}, b: {container, cell, side}}`. Same shape as a `sector.links` entry.
#[derive(Debug, Deserialize)]
struct PortalLinkRaw {
    a: LinkEndDecl,
    b: LinkEndDecl,
}

/// Recompute each affected grid from a list of raw sector attachments
/// (`{"id": <containerId>, "sector": {...}}` JSON strings) plus raw explicit
/// `linkOpening(a, b)` payloads. Used at module load and on every simulation
/// tick, so a sector created by an effect at runtime re-computes its grid (and,
/// on the next UI tick, the view) as soon as the effect commits.
pub fn recompute_sector_grids_from(raws: &[String], link_raws: &[String]) {
    if raws.is_empty() {
        return;
    }
    let mut by_grid: HashMap<String, Vec<SectorDeclaration>> = HashMap::new();
    for raw in raws.iter() {
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
        let links = att
            .sector
            .links
            .iter()
            .filter_map(|l| {
                let a_side = crate::state::Side::parse(&l.a.side)?;
                let b_side = crate::state::Side::parse(&l.b.side)?;
                Some(crate::state::ExplicitLink {
                    a: crate::state::LinkEndpoint {
                        container: l.a.container.clone(),
                        cell: (l.a.cell[0], l.a.cell[1]),
                        side: a_side,
                    },
                    b: crate::state::LinkEndpoint {
                        container: l.b.container.clone(),
                        cell: (l.b.cell[0], l.b.cell[1]),
                        side: b_side,
                    },
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
                links,
            });
    }
    // Apply explicit `linkOpening(a, b)` links: group each link by the grid that
    // owns its first endpoint (falling back to the second), then attach it to
    // that grid's declarations so `compute_grid` resolves both openings and
    // emits the portal (it dedupes and skips adjacency-redundant links).
    if !link_raws.is_empty() {
        let container_to_grid: HashMap<String, String> = by_grid
            .iter()
            .flat_map(|(_, decls)| {
                decls.iter().map(|d| (d.container.clone(), d.grid.clone()))
            })
            .collect();
        let mut grid_links: HashMap<String, Vec<crate::state::ExplicitLink>> =
            HashMap::new();
        for raw in link_raws.iter() {
            let pl: PortalLinkRaw = match serde_json::from_str(raw) {
                Ok(v) => v,
                Err(e) => {
                    runtime_log!("portal link parse error: {}", e);
                    continue;
                }
            };
            let (Some(a_side), Some(b_side)) = (
                crate::state::Side::parse(&pl.a.side),
                crate::state::Side::parse(&pl.b.side),
            ) else {
                continue;
            };
            let link = crate::state::ExplicitLink {
                a: crate::state::LinkEndpoint {
                    container: pl.a.container.clone(),
                    cell: (pl.a.cell[0], pl.a.cell[1]),
                    side: a_side,
                },
                b: crate::state::LinkEndpoint {
                    container: pl.b.container.clone(),
                    cell: (pl.b.cell[0], pl.b.cell[1]),
                    side: b_side,
                },
            };
            let Some(grid_id) = container_to_grid
                .get(&link.a.container)
                .cloned()
                .or_else(|| container_to_grid.get(&link.b.container).cloned())
            else {
                continue;
            };
            grid_links.entry(grid_id).or_default().push(link);
        }
        for (grid_id, links) in grid_links {
            if let Some(decls) = by_grid.get_mut(&grid_id) {
                if let Some(first) = decls.first_mut() {
                    first.links.extend(links);
                }
            }
        }
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
