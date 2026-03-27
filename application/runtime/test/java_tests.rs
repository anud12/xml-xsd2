use std::process::Command;

fn main() {
    // Example using Maven:
    let status = Command::new("E:\\Program Files\\IntelliJ IDEA 2025.2.2\\plugins\\maven\\lib\\maven3\\bin\\mvn.cmd")
        .arg("test")
        .arg("-ntp")
        .current_dir("../tests-java") // Point to your Java root
        .status()
        .expect("Failed to execute Java test runner");

    if !status.success() {
        std::process::exit(status.code().unwrap_or(1));
    }
}