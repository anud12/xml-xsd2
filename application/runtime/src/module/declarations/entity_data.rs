use crate::js_host_api::Declarations;

pub fn store_entity_data(dec: &Declarations) {
    let mut entities = dec.entity_map.clone();
    for entity in entities.values_mut() {
        entity.normalize();
    }
    for entity in entities.values() {
        entity.log_dangling(&entities);
    }
    *crate::state::entity_map().lock().unwrap() = entities;
}
