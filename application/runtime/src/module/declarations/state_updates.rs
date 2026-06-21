use crate::js_host_api::Declarations;

pub fn append_panels_to_cache(dec: &Declarations) {
    let mut existing =
        crate::state::last_panels().lock().unwrap();
    for p in dec.panels.iter() {
        if !existing.contains(p) {
            existing.push(p.clone());
        }
    }
}

pub fn store_pending_effects(dec: &Declarations) {
    if !dec.pending_effects.is_empty() {
        eprintln!(
            "DEBUG: apply setting pending_effects: {:?}",
            dec.pending_effects
        );
        runtime_log!(
            "pending effects: {:?}", dec.pending_effects
        );
        crate::state::set_pending_effects(
            dec.pending_effects.clone()
        );
        let verify =
            crate::state::pending_effects().lock().unwrap().clone();
        eprintln!(
            "DEBUG: after set_pending_effects, pending is: {:?}",
            verify
        );
    } else {
        eprintln!(
            "DEBUG: apply_declarations, pending_effects empty!"
        );
    }
}
