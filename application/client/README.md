# Game Client

This is the Godot-based game client for the project. The client handles presentation, input and rendering while delegating deterministic game logic and module execution to the Rust runtime (located at `application/runtime`). The runtime powers the game's simulation and module system used by both the client and the test harness.

## Project layout (relevant paths)
- `application/client/` — this Godot project (project.godot).
- `application/runtime/` — Rust deterministic runtime that runs game modules.
- `application/suite/` — Java/TypeScript test harness and integration tests.

## Requirements
- Godot 4.6.x (Mono-enabled build if using C# scripts)
- Rust toolchain (stable) to build the runtime
- (Optional) Java + Maven and Node/npm for the test suite

## Building the runtime
From the repository root run:

```
cargo build --manifest-path application\runtime\Cargo.toml --release
```

This produces a runtime binary under `application\runtime\target\release`.

## How the client uses the runtime
The client communicates with the runtime to load and execute module ZIPs that contain deterministic game logic. The runtime is responsible for deterministic execution, state persistence (it writes `state.db`) and — in debug/test modes — may emit raw SQLite bytes to stdout (surrounded by `--SQLITE-START--` and a delimiter). The client can integrate the runtime as a subprocess (stdio/IPC) or via native embedding depending on platform and build choices. Configure the client to locate the runtime binary before running.

## Running locally (example)
1. Build the runtime (see above).
2. Ensure the client can find the runtime binary. Example environment variable (PowerShell):

```powershell
$env:RUNTIME_BIN = "..\runtime\target\release\runtime.exe"
```

3. Open `project.godot` with the Godot editor and run the project.

Adjust paths if running from the repo root or another working directory.

## Tests
- Runtime unit tests: `cargo test --manifest-path application\runtime\Cargo.toml`
- Full suite (Java + TypeScript): `mvn -f application\suite\pom.xml test -ntp`

## Development notes
- Use the runtime for deterministic replayable simulations and authoritative game logic.
- For interoperability with the test harness, use `--stdioDebugWithDelimiterWrap=DELIM` when launching the runtime to emit SQLite bytes for capture.

## Contributing
1. Fork, create a topic branch, and submit a PR with clear description.
2. Run runtime and suite tests locally before opening PRs.

## License
Specify a license for this code (e.g., MIT, Apache-2.0) or add a LICENSE file to the repository.

## Native embedding (P/Invoke)

The runtime can be built as a native shared library and called from C# via P/Invoke. Quick steps:

1. Build the runtime as a DLL from the repository root:

```
cargo build --manifest-path application\runtime\Cargo.toml --release
```

2. Copy the produced DLL into the Godot client folder (Windows example):

```
copy application\runtime\target\release\xml_xsd2.dll application\client\
```

3. Use the provided RuntimeInterop.cs wrapper (application/client/RuntimeInterop.cs). Example usage from a Godot C# script:

```
var dbPath = RuntimeInterop.ProcessArchive("path\\to\\module.zip");
GD.Print("Persisted state at: " + dbPath);
```

Notes:
- The built DLL is named `xml_xsd2.dll` on Windows; update the LIB_NAME constant in RuntimeInterop.cs if needed.
- Ensure the DLL is accessible to the Godot runtime (place it in the project root or add its folder to PATH).
- Alternatively use the subprocess/stdio approach if you prefer decoupling the runtime from the engine process.
