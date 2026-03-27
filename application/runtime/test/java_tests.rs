use std::process::Command;

fn main() {
    println!("Running external Java JUnit suite...");

    // Example using Maven:
    let status = Command::new("mvn")
        .arg("test")
        .current_dir("../tests-java") // Point to your Java root
        .status()
        .expect("Failed to execute Java test runner");

    if !status.success() {
        std::process::exit(status.code().unwrap_or(1));
    }
}