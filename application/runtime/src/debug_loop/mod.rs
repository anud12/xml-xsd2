use std::io::{BufRead, Write};
use std::time::Instant;

mod load_handler;
mod action_handler;
mod export_handler;

const LOAD_PREFIX: &str = "DEBUG: Load:";
const ITERATE_PREFIX: &str = "DEBUG: ITERATE ";
const EXPORT_PREFIX: &str = "DEBUG: Export:";
const ACTION_PREFIX: &str = "DEBUG: ACTION ";
const SHUTDOWN_CMD: &str = "DEBUG: shutdown";

pub fn run(delimiter: &str) {
    let stdin = std::io::stdin();
    for line in stdin.lock().lines() {
        match line {
            Ok(cmd) if !dispatch(cmd.trim_end(), delimiter) => break,
            Err(_) => break,
            _ => {}
        }
    }
}

fn dispatch(cmd: &str, delimiter: &str) -> bool {
    if cmd == SHUTDOWN_CMD { return false; }
    if cmd.starts_with(ITERATE_PREFIX) {
        run_iterations(cmd, delimiter);
    }
    if cmd.starts_with(LOAD_PREFIX) {
        let payload = &cmd[LOAD_PREFIX.len()..];
        load_handler::handle_load(payload, delimiter);
    }
    if cmd.starts_with(EXPORT_PREFIX) {
        let path = &cmd[EXPORT_PREFIX.len()..];
        export_handler::handle_export(path, delimiter);
    }
    if cmd.starts_with(ACTION_PREFIX) {
        let payload = &cmd[ACTION_PREFIX.len()..];
        action_handler::handle_action(payload, delimiter);
    }
    true
}

fn run_iterations(cmd: &str, delimiter: &str) {
    let n: usize = cmd[ITERATE_PREFIX.len()..].trim().parse().unwrap_or(0);
    (0..n).for_each(|_| print_iteration_timing());
    debug_println!("{delimiter}OK{delimiter}");
    std::io::stdout().flush().ok();
}

fn print_iteration_timing() {
    let start = Instant::now();
    let elapsed = start.elapsed();
    debug_println!(
        "Iteration completed in {{{}:{}}}ns",
        elapsed.as_secs(),
        elapsed.subsec_nanos()
    );
}
