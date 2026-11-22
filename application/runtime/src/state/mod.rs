#![allow(dead_code)]
use std::sync::atomic::{AtomicBool, AtomicI64, AtomicU64};
use std::sync::{Once, Mutex};
use std::collections::HashMap;

mod accessors; mod clear; mod export; mod hot_reload; mod markers;
mod persist; mod scheduled; mod active_plans; mod sector;
mod entity;
pub mod area; pub mod area_query;

pub use accessors::*; pub use clear::*; pub use export::*;
pub use hot_reload::*;
pub use markers::*; pub use persist::*; pub use scheduled::*;
pub use active_plans::*; pub use sector::*;
pub use entity::*;
pub use area_query::{
    entity_area, entity_area_ids, memberships,
    entities_inside_area,
    set_entity_area, world_pieces,
};

static INIT: Once = Once::new();
static mut PERSISTED_HAS_DATA: Option<&'static AtomicBool> = None;
static mut LAST_FILE_ROWS: Option<&'static Mutex<Vec<Vec<String>>>> = None;
static mut LAST_ENTITY_ROWS: Option<&'static Mutex<Vec<Vec<String>>>> = None;
static mut LAST_ACTION_ROWS: Option<&'static Mutex<Vec<Vec<String>>>> = None;
static mut LAST_EVENT_ROWS: Option<&'static Mutex<Vec<Vec<String>>>> = None;
static mut LAST_MODULE_ROWS: Option<&'static Mutex<Vec<Vec<String>>>> = None;
static mut LAST_ARCHIVE_PATH: Option<&'static Mutex<String>> = None;
static mut LAST_ENTITY_PATTERNS: Option<&'static Mutex<Vec<String>>> = None;
static mut LAST_PANELS: Option<&'static Mutex<Vec<String>>> = None;
static mut LAST_CREATED_BY: Option<&'static Mutex<HashMap<String, Vec<String>>>> = None;
static mut PENDING_EFFECTS: Option<&'static Mutex<Vec<String>>> = None;
static mut SCHEDULED_EFFECTS: Option<&'static Mutex<Vec<ScheduledEffect>>> = None;
static mut ELAPSED_TIME_UNITS: Option<&'static AtomicI64> = None;
static mut ARCHIVE_FILES: Option<&'static Mutex<HashMap<String, String>>> = None;
static mut ACTIVE_PLANS: Option<&'static Mutex<Vec<ActivePlan>>> = None;
static mut SECTOR_GRIDS: Option<&'static Mutex<HashMap<String, SectorGridState>>> = None;
// The LXM PRNG seed for the loaded module. u64::MAX means "not set" (the JS
// side then falls back to the 0x7FFFFFFF default). It is NOT reset by
// clear_state: the host sets it once before (re)installing the module, and a
// hot reload re-installs against the same seed. Kept as a standalone static
// (outside the INIT group) so the host can set it before the first
// process_archive without forcing the full state initialization.
static LXM_SEED: AtomicU64 = AtomicU64::new(u64::MAX);

#[derive(Clone, Debug)]
pub struct ScheduledEffect {
    pub name: String,
    pub payload: serde_json::Value,
    pub next_exec_time: i64,
    pub reoccurrence_interval: i64,
    pub execution_count: u64,
}

/// A parked action plan: recorded steps walked per tick by the active-plans
/// walker. Plain data (no JS engine needed to walk it).
#[derive(Clone, Debug)]
pub struct ActivePlan {
    pub action_name: String,
    pub actor: String,
    pub steps: Vec<serde_json::Value>,
    pub resume_at: i64,
    pub interruptible: bool,
}

fn persisted_flag() -> &'static AtomicBool {
    INIT.call_once(|| {
        unsafe {
            PERSISTED_HAS_DATA = Some(Box::leak(Box::new(AtomicBool::new(false))));
            LAST_FILE_ROWS = Some(Box::leak(Box::new(Mutex::new(Vec::new()))));
            LAST_ENTITY_ROWS = Some(Box::leak(Box::new(Mutex::new(Vec::new()))));
            LAST_ACTION_ROWS = Some(Box::leak(Box::new(Mutex::new(Vec::new()))));
            LAST_EVENT_ROWS = Some(Box::leak(Box::new(Mutex::new(Vec::new()))));
            LAST_MODULE_ROWS = Some(Box::leak(Box::new(Mutex::new(Vec::new()))));
            LAST_ARCHIVE_PATH = Some(Box::leak(Box::new(Mutex::new(String::new()))));
            LAST_ENTITY_PATTERNS = Some(Box::leak(Box::new(Mutex::new(Vec::new()))));
            LAST_PANELS = Some(Box::leak(Box::new(Mutex::new(Vec::new()))));
            LAST_CREATED_BY = Some(Box::leak(Box::new(Mutex::new(HashMap::new()))));
            PENDING_EFFECTS = Some(Box::leak(Box::new(Mutex::new(Vec::new()))));
            SCHEDULED_EFFECTS = Some(Box::leak(Box::new(Mutex::new(Vec::new()))));
            ELAPSED_TIME_UNITS = Some(Box::leak(Box::new(AtomicI64::new(0))));
            ARCHIVE_FILES = Some(Box::leak(Box::new(Mutex::new(HashMap::new()))));
            ACTIVE_PLANS = Some(Box::leak(Box::new(Mutex::new(Vec::new()))));
            SECTOR_GRIDS = Some(Box::leak(Box::new(Mutex::new(HashMap::new()))));
        }
    });
    unsafe { PERSISTED_HAS_DATA.expect("persisted flag initialized") }
}
