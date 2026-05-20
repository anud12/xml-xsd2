#![allow(dead_code)]
use std::sync::atomic::{AtomicBool, Ordering};
use std::sync::{Once, Mutex};

use std::collections::HashMap;
use crate::module::compiled_ast::module::CompiledModule;

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
static mut LAST_ENTITY_DATA: Option<&'static Mutex<HashMap<String, HashMap<String, String>>>> = None;
static mut LAST_ENTITY_NUMBER_DATA: Option<&'static Mutex<HashMap<String, HashMap<String, f64>>>> = None;
static mut COMPILED_MODULE: Option<&'static Mutex<Option<CompiledModule>>> = None;
static mut GAME_TIME_MS: Option<&'static Mutex<u64>> = None;
static mut SCHEDULED_EFFECTS: Option<&'static Mutex<Vec<ScheduledEffect>>> = None;
static mut LAST_SCHEDULED_TIME: Option<&'static Mutex<HashMap<String, u64>>> = None;
static mut EFFECTS_AUTO_QUEUED: Option<&'static AtomicBool> = None;

#[derive(Debug, Clone)]
pub struct ScheduledEffect {
    pub name: String,
    pub scheduled_at_ms: u64,
    pub reoccur_after_ms: u64,
}

fn persisted_flag() -> &'static AtomicBool {
    INIT.call_once(|| {
        let b = Box::leak(Box::new(AtomicBool::new(false)));
        unsafe { PERSISTED_HAS_DATA = Some(b); }
        let f = Box::leak(Box::new(Mutex::new(Vec::new())));
        unsafe { LAST_FILE_ROWS = Some(f); }
        let e = Box::leak(Box::new(Mutex::new(Vec::new())));
        unsafe { LAST_ENTITY_ROWS = Some(e); }
        let a = Box::leak(Box::new(Mutex::new(Vec::new())));
        unsafe { LAST_ACTION_ROWS = Some(a); }
        let ev = Box::leak(Box::new(Mutex::new(Vec::new())));
        unsafe { LAST_EVENT_ROWS = Some(ev); }
        let m = Box::leak(Box::new(Mutex::new(Vec::new())));
        unsafe { LAST_MODULE_ROWS = Some(m); }
        let ap = Box::leak(Box::new(Mutex::new(String::new())));
        unsafe { LAST_ARCHIVE_PATH = Some(ap); }
        let p = Box::leak(Box::new(Mutex::new(Vec::new())));
        unsafe { LAST_ENTITY_PATTERNS = Some(p); }
        let panels = Box::leak(Box::new(Mutex::new(Vec::new())));
        unsafe { LAST_PANELS = Some(panels); }
        let cb = Box::leak(Box::new(Mutex::new(HashMap::new())));
        unsafe { LAST_CREATED_BY = Some(cb); }
        let pe = Box::leak(Box::new(Mutex::new(Vec::new())));
        unsafe { PENDING_EFFECTS = Some(pe); }
        let ed = Box::leak(Box::new(Mutex::new(HashMap::new())));
        unsafe { LAST_ENTITY_DATA = Some(ed); }
        let en = Box::leak(Box::new(Mutex::new(HashMap::new())));
        unsafe { LAST_ENTITY_NUMBER_DATA = Some(en); }
        let cm = Box::leak(Box::new(Mutex::new(None)));
        unsafe { COMPILED_MODULE = Some(cm); }
        let gt = Box::leak(Box::new(Mutex::new(0u64)));
        unsafe { GAME_TIME_MS = Some(gt); }
        let se = Box::leak(Box::new(Mutex::new(Vec::new())));
        unsafe { SCHEDULED_EFFECTS = Some(se); }
        let lst = Box::leak(Box::new(Mutex::new(HashMap::new())));
        unsafe { LAST_SCHEDULED_TIME = Some(lst); }
        let eq = Box::leak(Box::new(AtomicBool::new(false)));
        unsafe { EFFECTS_AUTO_QUEUED = Some(eq); }
    });
    unsafe { PERSISTED_HAS_DATA.expect("persisted flag initialized") }
}

pub fn last_file_rows() -> &'static Mutex<Vec<Vec<String>>> {
    persisted_flag();
    unsafe { LAST_FILE_ROWS.expect("file rows initialized") }
}
pub fn last_entity_rows() -> &'static Mutex<Vec<Vec<String>>> {
    persisted_flag();
    unsafe { LAST_ENTITY_ROWS.expect("entity rows initialized") }
}
pub fn last_action_rows() -> &'static Mutex<Vec<Vec<String>>> {
    persisted_flag();
    unsafe { LAST_ACTION_ROWS.expect("action rows initialized") }
}
pub fn last_event_rows() -> &'static Mutex<Vec<Vec<String>>> {
    persisted_flag();
    unsafe { LAST_EVENT_ROWS.expect("event rows initialized") }
}
pub fn last_module_rows() -> &'static Mutex<Vec<Vec<String>>> {
    persisted_flag();
    unsafe { LAST_MODULE_ROWS.expect("module rows initialized") }
}
pub fn last_entity_patterns() -> &'static Mutex<Vec<String>> {
    persisted_flag();
    unsafe { LAST_ENTITY_PATTERNS.expect("entity patterns initialized") }
}

pub fn last_panels() -> &'static Mutex<Vec<String>> {
    persisted_flag();
    unsafe { LAST_PANELS.expect("panels initialized") }
}

pub fn last_created_by() -> &'static Mutex<HashMap<String, Vec<String>>> {
    persisted_flag();
    unsafe { LAST_CREATED_BY.expect("created by map initialized") }
}

pub fn set_last_created_by(map: HashMap<String, Vec<String>>) {
    *last_created_by().lock().unwrap() = map;
}

/// Public helper for other modules to mark that persisted state has data
pub fn mark_persisted_has_data() {
    persisted_flag().store(true, Ordering::SeqCst);
}

pub fn set_last_file_rows(rows: Vec<Vec<String>>) {
    *last_file_rows().lock().unwrap() = rows;
}
pub fn set_last_entity_rows(rows: Vec<Vec<String>>) {
    *last_entity_rows().lock().unwrap() = rows;
}
pub fn append_entity_row(row: Vec<String>) {
    last_entity_rows().lock().unwrap().push(row);
}
pub fn set_last_action_rows(rows: Vec<Vec<String>>) {
    *last_action_rows().lock().unwrap() = rows;
}
pub fn set_last_event_rows(rows: Vec<Vec<String>>) {
    *last_event_rows().lock().unwrap() = rows;
}
pub fn set_last_module_rows(rows: Vec<Vec<String>>) {
    *last_module_rows().lock().unwrap() = rows;
}
pub fn set_last_entity_patterns(rows: Vec<String>) {
    *last_entity_patterns().lock().unwrap() = rows;
}
pub fn set_last_panels(rows: Vec<String>) {
    *last_panels().lock().unwrap() = rows;
}

pub fn pending_effects() -> &'static Mutex<Vec<String>> {
    persisted_flag();
    unsafe { PENDING_EFFECTS.expect("pending effects initialized") }
}

pub fn last_entity_data() -> &'static Mutex<HashMap<String, HashMap<String, String>>> {
    persisted_flag();
    unsafe { LAST_ENTITY_DATA.expect("entity data initialized") }
}

pub fn set_last_entity_data(data: HashMap<String, HashMap<String, String>>) {
    *last_entity_data().lock().unwrap() = data;
}

pub fn last_entity_number_data() -> &'static Mutex<HashMap<String, HashMap<String, f64>>> {
    persisted_flag();
    unsafe { LAST_ENTITY_NUMBER_DATA.expect("entity number data initialized") }
}

pub fn set_last_entity_number_data(data: HashMap<String, HashMap<String, f64>>) {
    *last_entity_number_data().lock().unwrap() = data;
}

pub fn set_pending_effects(effects: Vec<String>) {
    *pending_effects().lock().unwrap() = effects;
}

pub fn clear_pending_effects() {
    pending_effects().lock().unwrap().clear();
}

#[allow(dead_code)]
pub fn clear_state() {
    *last_file_rows().lock().unwrap() = Vec::new();
    *last_entity_rows().lock().unwrap() = Vec::new();
    *last_action_rows().lock().unwrap() = Vec::new();
    *last_event_rows().lock().unwrap() = Vec::new();
    *last_module_rows().lock().unwrap() = Vec::new();
    *last_entity_patterns().lock().unwrap() = Vec::new();
    *last_panels().lock().unwrap() = Vec::new();
    clear_pending_effects();
    *last_created_by().lock().unwrap() = HashMap::new();
    *last_archive_path().lock().unwrap() = String::new();
    *last_entity_data().lock().unwrap() = HashMap::new();
    *last_entity_number_data().lock().unwrap() = HashMap::new();
    clear_compiled_module();
    *game_time_ms().lock().unwrap() = 0;
    clear_scheduled_effects();
    clear_last_scheduled_time();
    reset_effects_auto_queued();
    persisted_flag().store(false, Ordering::SeqCst);
}

pub fn last_archive_path() -> &'static Mutex<String> {
    persisted_flag();
    unsafe { LAST_ARCHIVE_PATH.expect("archive path initialized") }
}

pub fn set_archive_path(path: &str) {
    *last_archive_path().lock().unwrap() = path.to_string();
}

pub fn compiled_module() -> &'static Mutex<Option<CompiledModule>> {
    persisted_flag();
    unsafe { COMPILED_MODULE.expect("compiled module initialized") }
}

pub fn set_compiled_module(module: CompiledModule) {
    *compiled_module().lock().unwrap() = Some(module);
}

pub fn get_compiled_module() -> Option<CompiledModule> {
    compiled_module().lock().unwrap().clone()
}

#[allow(dead_code)]
pub fn clear_compiled_module() {
    *compiled_module().lock().unwrap() = None;
}

pub fn game_time_ms() -> &'static Mutex<u64> {
    persisted_flag();
    unsafe { GAME_TIME_MS.expect("game time initialized") }
}

pub fn set_game_time_ms(ms: u64) {
    *game_time_ms().lock().unwrap() = ms;
}

pub fn increment_game_time_ms(ms: u64) {
    let mut gt = game_time_ms().lock().unwrap();
    *gt += ms;
}

pub fn scheduled_effects() -> &'static Mutex<Vec<ScheduledEffect>> {
    persisted_flag();
    unsafe { SCHEDULED_EFFECTS.expect("scheduled effects initialized") }
}

pub fn push_scheduled_effect(effect: ScheduledEffect) {
    scheduled_effects().lock().unwrap().push(effect);
}

pub fn clear_scheduled_effects() {
    scheduled_effects().lock().unwrap().clear();
}

pub fn last_scheduled_time() -> &'static Mutex<HashMap<String, u64>> {
    persisted_flag();
    unsafe { LAST_SCHEDULED_TIME.expect("last scheduled time initialized") }
}

pub fn get_effect_next_scheduled(effect_name: &str, reoccur_ms: u64) -> u64 {
    let mut lst = last_scheduled_time().lock().unwrap();
    let current = lst.entry(effect_name.to_string()).or_insert(0);
    let next_at = *current + reoccur_ms;
    *current = next_at;
    next_at
}

pub fn clear_last_scheduled_time() {
    *last_scheduled_time().lock().unwrap() = HashMap::new();
}

pub fn effects_auto_queued() -> &'static AtomicBool {
    persisted_flag();
    unsafe { EFFECTS_AUTO_QUEUED.expect("effects auto queued flag initialized") }
}

pub fn mark_effects_auto_queued() {
    effects_auto_queued().store(true, Ordering::SeqCst);
}

pub fn reset_effects_auto_queued() {
    effects_auto_queued().store(false, Ordering::SeqCst);
}
