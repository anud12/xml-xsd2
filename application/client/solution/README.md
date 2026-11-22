# New Game Project — Godot client

A Godot 4.6 / C# game client. The gameplay logic lives in a native Rust
runtime (`libxml_xsd2.dll` / `xml-xsd2.dll`) that the client talks to over a
thin C `extern` FFI layer; the playable content is shipped as **JS modules**
that the Rust runtime executes. The Godot side renders the runtime's UI state
and forwards user input back to it.

## Layout

- `Sources/Runtime/` — the FFI boundary (`RuntimeInterop`, `EntityInterop`,
  `SectorInterop`) and the native DLLs.
- `Sources/UI/` — the runtime-driven UI (`RootNode` paints the runtime's slab;
  `UiWindow` renders a node; `UiTraceability` records clicks for replay).
- `Sources/Layout/`, `Sources/Module/` — layout helpers and module
  archive/hot-reload helpers.
- `Scenes/` — the Godot scenes (`GameScene`, main menu).
- `Modules/` — the JS game modules (e.g. `TestTick`) that define entities,
  actions, effects, and UI.
- `Tests/` — the GdUnit4 test suite (Godot-runtime tests under
  `Tests/GameScene`, one `Stage_N/` per capability).

## Threads

The app runs several threads. Only the Godot main thread may touch the scene
tree / UI; everything that could block (file I/O, the runtime tick) is moved
off it.

| Thread | Origin | Lifetime | What it does |
| --- | --- | --- | --- |
| **Godot main** | engine | whole run | Runs the frame loop (`_Process`, input, `RootNode.Pump()` → paint/apply/layout), builds and drives the UI, forwards clicks to the runtime. Must never block on I/O. |
| **Runtime** | `Game._Ready` (managed `Thread`) | game run, off in tests (`RUN_RUNTIME_LOOP`) | The tick loop: calls `RuntimeInterop.RunIteration(1)` on a 125 ms (~8 Hz) cadence. This is the only producer of runtime work (action plans, recurring effects, UI deltas). |
| **Logging** | `Game.StartLogWriter` (managed `Thread`, named `"Logging"`) | process lifetime (background) | All file logging, off the main thread. Drains the runtime log queue → `user://runtime.log` and the UI-traceability queue → the configured `.replay` file. A slow sink (e.g. a minimized IDE console) can therefore never hitch a frame. |
| **Module watcher** | `Game._Ready` (`FileSystemWatcher` + `Timer`) | dev only (`AUTO_HOT_RELOAD && !TEST_MODE`) | Detects module directory changes (debounced) and triggers a hot reload on the main thread. |
| **Godot internal** | engine | whole run | Engine-owned threads (rendering on d3d12/GL-compatibility, Jolt physics, audio, resource I/O). |

Design rule: the main thread only ever **enqueues** (a `ConcurrentQueue`) and
**emits** (an FFI call that returns fast); it never does synchronous file I/O
or a blocking console write. The `Logging` thread does the I/O.

## Build & test

```
# from this directory
dotnet build "New Game Project.csproj" -v quiet -nologo
dotnet test  "New Game Project.csproj" --settings .runsettings -v minimal --nologo
```

Tests run **windowed** (`.runsettings` sets `Parameters = --verbose`, no
`--headless`) because the UI tests need a real viewport.
