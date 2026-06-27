use std::collections::HashMap;
use crate::js_host_api::Declarations;

pub fn build_action_to_created(
    dec: &Declarations,
) -> HashMap<String, Vec<String>> {
    let mut map: HashMap<String, Vec<String>> = HashMap::new();
    insert_creator_actions(&mut map, dec);
    insert_emitted_actions(&mut map, dec);
    map
}

fn insert_creator_actions(
    map: &mut HashMap<String, Vec<String>>,
    dec: &Declarations,
) {
    for (k, v) in dec.creators.iter() {
        if dec.actions.iter().any(|a| a == k) {
            map.insert(k.clone(), v.clone());
        }
    }
}

fn insert_emitted_actions(
    map: &mut HashMap<String, Vec<String>>,
    dec: &Declarations,
) {
    for (action, emitted) in dec.emits.iter() {
        if !dec.actions.iter().any(|a| a == action) {
            continue;
        }
        let mut pats: Vec<String> = Vec::new();
        for e_name in emitted.iter() {
            if let Some(p) = dec.creators.get(e_name) {
                pats.extend(p.clone());
            }
        }
        if !pats.is_empty() {
            map.insert(action.clone(), pats);
        }
    }
}
