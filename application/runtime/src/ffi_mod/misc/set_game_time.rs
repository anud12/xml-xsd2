#[no_mangle]
pub extern "C" fn runtime_set_game_time(ms: u64) {
    crate::state::set_game_time_ms(ms);
}
