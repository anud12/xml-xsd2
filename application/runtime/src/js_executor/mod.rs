//! JavaScript execution module for extraction and simulation.

mod extract;
mod extract_invoke;
mod strip_export;
mod import_resolver;
mod simulate;
mod context_builders;
mod pending_effects;
mod scheduled_effects;
mod behavior_scripts;
mod js_strings_effect;
mod entity_sync;
mod entity_sync_map;
mod entity_sync_back;
mod sim_template;
mod sim_entry;
mod sim_store;
mod pending_ctx_p1;
mod pending_ctx_p2;
mod pending_ctx_p3;
mod scheduled_ctx_p1;
mod scheduled_ctx_p2;
mod scheduled_ctx_p3;
mod sim_tpl_p1;
mod sim_tpl_p2;
mod sim_tpl_p3;
mod sim_invoke;
mod archive_loader;

pub use extract::extract_from_source;
pub use archive_loader::{ArchiveResolver, ArchiveLoader};
pub use simulate::{simulate_action, convert_store_values};
pub use pending_effects::process_pending_effects;
pub use scheduled_effects::process_scheduled_effects;
pub use behavior_scripts::process_behavior_scripts;
