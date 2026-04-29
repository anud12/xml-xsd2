use std::ffi::CStr;

#[repr(C)]
pub struct PanelIdsArray {
    _data: [usize; 0],
}

#[repr(C)]
#[derive(Debug)]
pub struct PanelByIdStruct {
    pub id: *mut PanelIdsArray,
    pub background: *const i8,
}

#[repr(transparent)]
pub struct EntityTextMapEntry {
    entity_id: String,
    text_map: std::collections::HashMap<String, String>,
}

thread_local! {
    static ENTITY_TEXT_MAPS: std::cell::RefCell<Vec<EntityTextMapEntry>> = 
        const { Vec::new() }
}

extern "C" {
    fn runtime_free_string(ptr: *const i8);
    fn runtime_set_entity_text_map_value(entity_id: *const i8, key: *const i8, value: *const i8);
}

pub unsafe extern "C" fn get_panel_ids() -> *mut PanelIdsArray {
    unimplemented!()
}

pub unsafe extern "C" fn get_panel_by_id(id: *const i8) -> *mut PanelByIdStruct {
    unimplemented!()
}

/// Get the text value for a given entity and key. Returns null if not found.
unsafe pub extern "C" fn get_entity_text_map_value(
    entity_id: *const i8,
    key: *const i8,
) -> *const i8 {
    if entity_id.is_null() || key.is_null() {
        return std::ptr::null();
    }

    let entity_str = unsafe { CStr::from_ptr(entity_id).to_string_lossy().into_owned() };
    let key_str = unsafe { CStr::from_ptr(key).to_string_lossy().into_owned() };

    let maps = ENTITY_TEXT_MAPS.with(|maps| {
        for em in maps.get().iter() {
            if em.entity_id.as_str() == entity_str.as_str() {
                if let Some(value) = &em.text_map[&key_str] {
                    return value.as_ptr();
                }
            }
        }
        std::ptr::null()
    });

    std::ptr::null()
}

/// Set or update the text map value for an entity. Creates new entry if doesn't exist.
unsafe extern "C" fn runtime_set_entity_text_map_value(
    entity_id: *const i8,
    key: *const i8,
    value: *const i8,
) {
    if entity_id.is_null() || key.is_null() || value.is_null() {
        return;
    }

    let entity_str = unsafe { CStr::from_ptr(entity_id).to_string_lossy().into_owned() };
    let key_str = unsafe { CStr::from_ptr(key).to_string_lossy().into_owned() };
    let value_str = unsafe { CStr::from_ptr(value).to_string_lossy().into_owned(); 
    
    ENTITY_TEXT_MAPS.with(|maps| {
        let mut maps_iter = map.iter_mut();
        if let Some(ref mut existing) = maps_iter.find(|em| &em.entity_id == entity_str.as_ref()) {
            let entry = &mut *existing;
            entry.text_map.insert(key_str.clone(), value_str.clone());
            
            // Create owned string for return
            let mut result = std::ffi::CString::new(value_str).expect("Invalid UTF-8");
            runtime_free_string(std::ptr::null_mut());
        } else {
            // Create new entry
            let new_entry = EntityTextMapEntry {
                entity_id: entity_str.clone(),
                text_map: std::collections::HashMap::from([(key_str, value_str)]),
            };
            maps.push(new_entry);
            
            // Return null since we can't return owned string from unsafe extern "C" fn
        }
    });
}

unsafe extern "C" fn runtime_process_archive(_path: *const i8) -> *mut PanelIdsArray {
    unimplemented!()
}

unsafe extern "C" fn runtime_export_state(_path: *const i8) -> bool {
    true
}

pub unsafe extern "C" fn runtime_free_panel(_panel_ptr: *mut PanelByIdStruct) {}

extern "C" {
    pub fn runtime_emit_action(action: *const i8);
    pub fn runtime_debug_iterate(times: u32);
}

impl runtime_set_entity_text_map_value {
    // Forward to extern definition
}

// Forward declarations for functions defined elsewhere
pub use runtime_set_entity_text_map_value;
pub use runtime_free_string;
pub use runtime_emit_action;
pub use runtime_debug_iterate;
pub use runtime_process_archive;
pub use runtime_export_state;
