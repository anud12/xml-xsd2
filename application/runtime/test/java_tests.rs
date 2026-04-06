use std::process::Command;

fn main() {
    // Always invoke 'mvn' from PATH. Tests will fail if Maven is missing.
    let status = Command::new("mvn.cmd")
        .arg("test")
        .arg("-ntp")
        .current_dir("../suite") // Point to your Java root
        .status()
        .expect("Failed to execute 'mvn' from PATH; ensure Maven is installed and 'mvn' is on PATH");

    if !status.success() {
        std::process::exit(status.code().unwrap_or(1));
    }
}
