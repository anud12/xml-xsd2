use std::process::Command;
use std::env;
use std::path::PathBuf;

fn find_mvn() -> PathBuf {
    if let Ok(home) = env::var("MAVEN_HOME").or_else(|_| env::var("M2_HOME")) {
        let mut p = PathBuf::from(&home);
        p.push("bin");
        p.push("mvn.cmd");
        if p.exists() {
            return p;
        }
        let mut p2 = PathBuf::from(&home);
        p2.push("bin");
        p2.push("mvn");
        if p2.exists() {
            return p2;
        }
    }

    PathBuf::from("mvn")
}

fn main() {
    // Example using Maven:
    let mvn = find_mvn();
    let status = Command::new(&mvn)
        .arg("test")
        .arg("-ntp")
        .current_dir("../suite") // Point to your Java root
        .status()
        .expect("Failed to execute Java test runner");

    if !status.success() {
        std::process::exit(status.code().unwrap_or(1));
    }
}