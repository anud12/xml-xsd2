#![allow(dead_code)]
use std::sync::atomic::{AtomicBool, AtomicI64};
use std::sync::{Once, Mutex};
use std::collections::HashMap;

mod accessors; mod clear; mod export; mod markers;
mod persist; mod scheduled;

pub use accessors::*; pub use clear::*; pub use export::*;
pub use markers::*; pub use persist::*; pub use scheduled::*;

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
static mut LAST_ENTITY_DATA: Option<&'static Mutex<HashMap<String, HashMap<String, String>>>> = None;
static mut LAST_ENTITY_NUMBER_DATA: Option<&'static Mutex<HashMap<String, HashMap<String, f64>>>> = None;
static mut INITIAL_ENTITY_DATA: Option<&'static Mutex<HashMap<String, HashMap<String, String>>>> = None;
static mut ELAPSED_TIME_UNITS: Option<&'static AtomicI64> = None;

#[derive(Clone, Debug)]
pub struct ScheduledEffect {
    pub name: String,
    pub payload: serde_json::Value,
    pub next_exec_time: i64,
    pub reoccurrence_interval: i64,
    pub execution_count: u64,
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
            LAST_ENTITY_DATA = Some(Box::leak(Box::new(Mutex::new(HashMap::new()))));
            LAST_ENTITY_NUMBER_DATA = Some(Box::leak(Box::new(Mutex::new(HashMap::new()))));
            INITIAL_ENTITY_DATA = Some(Box::leak(Box::new(Mutex::new(HashMap::new()))));
            ELAPSED_TIME_UNITS = Some(Box::leak(Box::new(AtomicI64::new(0))));
        }
    });
    unsafe { PERSISTED_HAS_DATA.expect("persisted flag initialized") }
}
