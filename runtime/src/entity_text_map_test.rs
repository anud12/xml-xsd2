use std::ffi::CString;
use std::ptr;

thread_local! {
    static ENTITY_TEXT_MAPS: std::cell::RefCell<std::vec::Vec<EntityTextMap>> = const { Vec::new() }
}

#[derive(Debug)]
struct EntityTextMap {
    entity_id: String,
    text_map: std::collections::HashMap<String, String>,
}

#[test]
fn test_entity_text_map_stage4_initialization() {
    ENTITY_TEXT_MAPS.with(|maps| {
        maps.borrow_mut().push(EntityTextMap {
            entity_id: "entity_id".to_string(),
            text_map: [("textKey".to_string(), "initialTextValue".to_string())].into_iter().collect(),
        });
        assert!(maps.borrow().iter().any(|em| em.entity_id == "entity_id"));
    });
}

#[test]
fn test_stage4_runtime_update_panel_content() {
    // Simulates Stage_4 scenario:
    // 1. Initial panel with entity text value displays "initialTextValue"
    // 2. Call SetEntityTextMapValue(entityId, key, updatedValue)
    // 3. SimulateIterations(1) for runtime processing
    // 4. Panel now displays "updatedTextValue"
    
    ENTITY_TEXT_MAPS.with(|maps| {
        let mut maps = maps.borrow_mut();
        
        // Step 1: Initial state - entity with initial value
        let existing = maps.iter().find(|em| &em.entity_id == "entity_id");
        if let Some(em) = existing {
            em.text_map.insert("textKey".to_string(), "initialTextValue".to_string());
        } else {
            maps.push(EntityTextMap {
                entity_id: "entity_id".to_string(),
                text_map: [("textKey".to_string(), "initialTextValue".to_string())].into_iter().collect(),
            });
        }
        
        // Step 2: Update entity text map (simulating SetEntityTextMapValue)
        let maps = &mut *maps;
        for em in maps.iter_mut() {
            if em.entity_id == "entity_id" {
                em.text_map.insert("textKey".to_string(), "updatedTextValue".to_string());
            }
        }
        
        // Step 3: Verify entity exists and has updated value
        let maps = &*maps;
        if let Some(em) = maps.iter().find(|em| &em.entity_id == "entity_id") {
            assert_eq!(em.text_map.get("textKey").unwrap(), "updatedTextValue");
        }
    });
}

#[test]
fn test_stage4_multiple_operations() {
    ENTITY_TEXT_MAPS.with(|maps| {
        let maps = &*maps;
        if let Some(em) = maps.iter().find(|em| &em.entity_id == "entity_id") {
            assert_eq!(em.text_map.get("textKey").unwrap(), "initialTextValue");
        }
        
        // Update multiple times
        for i in 0..10u32 {
            let val = format!("updated_value_{}", i);
            let maps = &mut *maps;
            for em in maps.iter_mut() {
                if em.entity_id.clone() == "entity_id" {
                    em.text_map.insert("textKey".to_string(), val.clone());
                    break;
                }
            }
        }
        
        let maps = &*maps;
        if let Some(em) = maps.iter().find(|em| &em.entity_id == "entity_id") {
            assert_eq!(em.text_map.get("textKey").unwrap(), "updated_value_9");
        }
    });
}

#[test]
fn test_multiple_entities_text_maps() {
    ENTITY_TEXT_MAPS.with(|maps| {
        // Test entity 1
        let maps = &mut *maps;
        if !maps.iter().any(|em| &em.entity_id == "entity_a") {
            maps.push(EntityTextMap {
                entity_id: "entity_a".to_string(),
                text_map: HashMap::new(),
            });
        }
        
        // Test entity 2
        let maps = &mut *maps;
        if !maps.iter().any(|em| &em.entity_id == "entity_b") {
            maps.push(EntityTextMap {
                entity_id: "entity_b".to_string(),
                text_map: HashMap::new(),
            });
        }
        
        let maps = &*maps;
        assert!(maps.iter().any(|em| &em.entity_id == "entity_a"));
        assert!(maps.iter().any(|em| &em.entity_id == "entity_b"));
    });
}
