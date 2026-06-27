use std::process::Command;

fn main() {

    // Always invoke 'mvn' from PATH. Tests will fail if Maven is missing.
    #[cfg(target_os = "windows")]
    let cmd = "mvn.cmd";
    #[cfg(not(target_os = "windows"))]
    let cmd = "mvn";
    let status = Command::new(cmd)
        .arg("clean")
        .arg("compile")
        .arg("test")
        .arg("-ntp")

        .current_dir("../suite") // Point to your Java root
        .status()
        .expect("Failed to execute 'mvn' from PATH; ensure Maven is installed and 'mvn' is on PATH");

    if !status.success() {
        std::process::exit(status.code().unwrap_or(1));
    }


    // Define the arguments as a vector or just chain them
    let status = Command::new("dotnet")
        .arg("test")
        .arg("New Game Project.csproj")
        .arg("--settings:.runsettings")
        .arg("-c")
        .arg("Release")
        .arg("-v")
        .arg("normal")
        .arg("--no-build")
        .arg("--logger")
        .arg("trx;LogFileName=test-result.trx")
        .arg("--logger")
        .arg("html;LogFileName=test-result.html")
        .arg("--results-directory")
        .arg("./TestResults")
        .arg("/p:GodotProjectDir=/workspace/application/client/solution")
        .arg("/p:SkipRustBuild=true")
        .arg("/p:SkipJsValidation=true")
        // Set the working directory to where your solution/project lives
        .current_dir("../client/solution")
        .status()
        .expect("Failed to execute 'dotnet'. Ensure the .NET SDK is installed and in your PATH.");

    if !status.success() {
        std::process::exit(status.code().unwrap_or(1));
    }
}
