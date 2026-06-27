use crate::js_host_api::Declarations;

pub fn collect_patterns(dec: &Declarations) -> Vec<String> {
    let mut patterns: Vec<String> = Vec::new();
    add_creator_patterns(&mut patterns, dec);
    add_entity_patterns(&mut patterns, dec);
    patterns
}

fn add_creator_patterns(
    patterns: &mut Vec<String>,
    dec: &Declarations,
) {
    for (_k, v) in dec.creators.iter() {
        for item in v.iter() {
            add_unique(patterns, item.clone());
        }
    }
}

fn add_entity_patterns(
    patterns: &mut Vec<String>,
    dec: &Declarations,
) {
    for en in dec.entities.iter() {
        add_unique(patterns, en.clone());
    }
}

fn add_unique(vec: &mut Vec<String>, item: String) {
    if !vec.contains(&item) {
        vec.push(item);
    }
}
