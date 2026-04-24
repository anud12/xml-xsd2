use std::process::Command;

fn main() {

    // Always invoke 'mvn' from PATH. Tests will fail if Maven is missing.
    let status = Command::new("mvn.cmd")
        .arg("clean")
        .arg("compile")
        .arg("test")
        .arg("-ntp")
        .arg("-P stage1,stage2")

        .current_dir("../suite") // Point to your Java root
        .status()
        .expect("Failed to execute 'mvn' from PATH; ensure Maven is installed and 'mvn' is on PATH");

    if !status.success() {
        std::process::exit(status.code().unwrap_or(1));
    }


    // Define the arguments as a vector or just chain them
    let status = Command::new("dotnet")
        .arg("test")
        .arg("--settings:")
        .arg(".runsettings")
        // Set the working directory to where your solution/project lives
        .current_dir("../client/solution")
        .status()
        .expect("Failed to execute 'dotnet'. Ensure the .NET SDK is installed and in your PATH.");

    if !status.success() {
        std::process::exit(status.code().unwrap_or(1));
    }
}
