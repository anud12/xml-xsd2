# Save / Load functionality

## Goal

Let the player persist a live game into the save directory and resume it later from
the main menu, so that all *dynamic* progress (entity values, positions, sector
state, time, in-flight action plans) is restored on top of the freshly re-loaded
static module.

## What already exists

The runtime already exposes half of a save system through the FFI surface
(`application/runtime/src/ffi_mod/`):

- `runtime_process_archive(zipPath)` → boot path. The most complete loader:
  `set_archive_path` + `read_zip_files` + `build_file_rows` + `process_module` +
  `persist_state` + `set_archive_files` + `sim_ctx::install(&files)`
  (`ffi_mod/export/process_archive.rs`).
- `runtime_load_archive(bytes, len)` → re-runs `process_module` from a zip
  buffer. Static state only; does **not** call `sim_ctx::install`.
- `runtime_export_state(path)` / `runtime_export_state_struct()` → SQLite
  snapshot of *static* module data (`state/export.rs`). Proves the
  rusqlite + row-mapping pattern already works in this codebase.
- `runtime_clear_state()` → resets all cached state.

Godot side:

- `SettingsStore.SaveDirectory` already resolves the user save directory — reuse.
- `MainMenu.tscn` has a **disabled** `LoadGameButton` (no handler wired in
  `MainMenu.cs` yet).
- `Game._Ready` boots via `CreateArchive(...)` + `RuntimeInterop.ProcessArchive`
  unless `SKIP_CREATE_ARCHIVE`; the sim loop is a background thread gated on
  `Game.RUN_RUNTIME_LOOP`.

Deps: `rusqlite = { version = "0.39", features = ["bundled", "backup"] }` is
already a runtime dependency. **No new dependencies are needed for this feature.**

## The gap

Everything the player *does* at runtime lives in process-global Rust state
(`state/mod.rs`) and is lost on reload. The dynamic state that must survive a
save:

| State | Accessor | Setter exists? |
|---|---|---|
| Entity text values | `last_entity_data()` | `set_last_entity_data` |
| Entity number values | `last_entity_number_data()` | `set_last_entity_number_data` |
| Containers (JSON rows) | `last_containers()` | `set_last_containers` |
| Sector grids | `sector_grids()` | `set_sector_grid` (per grid) |
| Entity areas | `entity_areas_inner()` | `set_entity_area` (per entry) |
| Elapsed time | `elapsed_time_units()` | atomic `store` |
| Scheduled effects | `scheduled_effects()` | **missing — add `set_scheduled_effects`** |
| Active plans | `active_plans()` | **missing — add `set_active_plans`** |

`initial_entity_data` is **not** saved: it is only ever written by module replay
(`declarations/entity_data.rs:19`), so re-running the module regenerates it.

## Design

### Save format: one SQLite file per slot

`<save-dir>/save.db` (v1 = single slot). Schema reference + working sample:
`plans/save-sqlite-example/` (`save.sql` + built `save.db`).

- `PRAGMA user_version` = save format version (currently `1`); a higher version
  is rejected with a "save is from a newer version" error, a lower one goes
  through a migration step (none needed for v1→v1).
- **Fully normalized — JSON in exactly two columns**, both genuinely opaque
  JS data: `scheduled_effects.payload` and `active_plans.steps`.
- Tables (16; the full DDL + sample data is `save.sql`):

  | Table | Holds | Notes |
  |---|---|---|
  | `meta` (1 row) | `saved_at_utc`, module `id`/`name`/`version`, `elapsed_time` | |
  | `entity_text` / `entity_number` | entity values | EAV, PK `(entity_id, key)`; game keys are dynamic, can't be fixed columns |
  | `containers` | container declarations | `id` PK + `alignment`, `x_key`/`y_key`, `size_{x,y}`, `size_{x,y}_oob` |
  | `container_entities` | container membership | `(container_id, seq, entity_id)` — `seq` preserves array order |
  | `container_text` / `container_number` | container maps | EAV, PK `(container_id, key)` |
  | `container_positions` | position snapshots | `(container_id, field, entity_id, value)`, `field ∈ getX\|getY\|getSpanX\|getSpanY` |
  | `sector_grids` | grid identity | keeps grid id even for empty grids |
  | `sector_cells` | footprint occupancy | `(grid_id, x, y, container)` |
  | `sector_cell_edges` | boundary edges | `(grid_id, sq_x, sq_y, side)`, `side CHECK IN ('N','E','S','W')` |
  | `sector_cell_openings` | openings | `(grid_id, cell_x, cell_y, side, start, length)` — ≤1 per edge per spec |
  | `sector_portals` | portals | `a_/b_` cell+side+span+length columns |
  | `entity_area_points` | polygons | `(entity_id, area_id, seq, x, y)` |
  | `scheduled_effects` | timers | `name` PK (uniqueness guaranteed, `scheduled.rs:6`), `payload` JSON (opaque) |
  | `active_plans` | in-flight plans | `steps` JSON (heterogeneous step objects, opaque), `interruptible CHECK IN (0,1)` |

- **Why containers normalize cleanly:** the row format is a closed field set
  built in JS (`script_panel_entity.rs` + `script_rest.rs`), and
  `getX/getY/getSpanX/getSpanY` are *not* functions in the row —
  `evalPositionFn` evaluates them against every entity at declaration time and
  stores a `{entityId: value}` snapshot map, which maps 1:1 to
  `container_positions`. Load re-serializes the row from the five container
  tables with exactly the field names the ~7 consumers parse; the field set
  won't change, so the re-serializer is stable.
- Load path stays JSON-light: sector grids build the `SectorGridState` struct
  directly from rows (no JSON parse); only `payload`/`steps` are
  `serde_json::from_str`'d into `Value`.

- **The module archive is NOT bundled.** The save is only valid against the
  installed module. The `meta` module triple is load-bearing: on load it is
  compared against the replayed module's `last_module_rows()`
  (`module/manifest.rs:set_module_rows`); mismatch → load rejected.
- **Integrity** comes from SQL validation itself (schema, types, JSON parse of
  blob columns) — no separate checksum layer.
- **Atomic writes**: the save is built in a temp file in the same directory,
  then the old `save.db` (if any) is removed and the temp renamed into place.
  A crash mid-save can never leave a torn `save.db`.
- **Clock coherence:** `next_exec_time` / `resume_at` are absolute sim-clock
  values; `elapsed_time` is saved alongside, so they stay valid after restore.

### Rust: new FFI (`ffi_mod/export/save_game.rs`, `load_game.rs`)

`runtime_save_game(path) -> bool`
1. Snapshot every dynamic-state accessor into memory (the Godot side pauses the
   sim loop for the duration — no torn reads).
2. Build a temp DB (`.db.tmp` in the same dir): set `user_version`, insert all
   tables in one transaction, `VACUUM`, close.
3. Replace the old file atomically (remove + rename). Return success.

`runtime_load_game(path) -> bool` — **validate-then-mutate**:
1. Open the DB **read-only**.
2. Validate everything *before touching any global state*:
   - `user_version` supported (higher → reject).
   - Exactly one `meta` row; its module `id`/`name`/`version` matches
     `last_module_rows()` (the module was just replayed by the normal boot).
   - Read **all** tables into memory (any SQL/JSON error → reject).
3. Hydrate dynamic state:
   - `entity_text`/`entity_number` → **per-entity merge**: save values overwrite
     the replayed initial values; module entities absent from the save keep
     their authored values; save entities absent from the module are skipped
     with a warning log.
   - `containers` → replace `last_containers` wholesale (save is authoritative).
   - `sector_grids` → merge by grid id, save wins (module-added grids absent
     from the save survive).
   - `entity_areas` → replace via `set_entity_area` per row.
   - `scheduled_effects` / `active_plans` → replace via the new setters.
   - `elapsed_time` → `elapsed_time_units().store(...)`.
4. Return success.

Notes:

- No `serde` derives needed: `ScheduledEffect.payload` / `ActivePlan.steps` are
  `serde_json::Value` (already (de)serializable) stored as TEXT; everything else
  maps to plain columns.
- No archive bundling, no temp zips, no `sim_ctx` plumbing — the module replay
  (including `sim_ctx::install`) already happened in the normal
  `ProcessArchive` boot, before `LoadGame` is called.
- The `reload_from_files` extraction (unifying `process_archive` /
  `load_archive` / debug load) is still worth doing as hygiene, but is no
  longer on the save feature's critical path.

### C# interop (`Sources/Runtime/RuntimeInterop.cs`)

```csharp
[DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
private static extern bool runtime_save_game([MarshalAs(UnmanagedType.LPStr)] string path);
[DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
private static extern bool runtime_load_game([MarshalAs(UnmanagedType.LPStr)] string path);

public static bool SaveGame(string path) => runtime_save_game(path);
public static bool LoadGame(string path) => runtime_load_game(path);
```

### Godot: save store + UI

1. **`Scenes/Settings/SaveSlotStore.cs`** (new, mirrors `SettingsStore`)
   - `SavePath` = `Path.Combine(SettingsStore.SaveDirectory, "save.db")`.
   - `Save()` / `HasSave()` / `Load()` → `RuntimeInterop` calls.
2. **`Scenes/GameScene/Game.cs`**
   - **Loop barrier:** the background loop re-reads `RUN_RUNTIME_LOOP` each
     cycle. Wrap save/load with
     `bool prev = RUN_RUNTIME_LOOP; RUN_RUNTIME_LOOP = false; Thread.Sleep(300); ... RUN_RUNTIME_LOOP = prev;`
     (300 ms > one 250 ms cycle) so no `RunIteration` races the snapshot.
   - In-game save: `SaveGameButton` in `coreUI` **and** an `F5` keybind in
     `_UnhandledInput` → `SaveSlotStore.Save()`, brief "Saved" toast on coreUI.
   - Add `public static string? RESUME_FROM_SAVE;`. In `_Ready`:

     ```csharp
     if (ARCHIVE_DIR == null && !SKIP_CREATE_ARCHIVE) {
         string zip = CreateArchive(@"...MainModule");
         string db = RuntimeInterop.ProcessArchive(zip);   // always replay installed module
         if (db == null) { /* error */ return; }
         if (RESUME_FROM_SAVE != null && !RuntimeInterop.LoadGame(RESUME_FROM_SAVE)) {
             GD.PrintErr("Load rejected (module mismatch or corrupt save)");
             GetTree().ChangeSceneToFile("res://Scenes/MainMenu/MainMenu.tscn");
             return;
         }
     }
     ```

     The rest of `_Ready` (RootNode rebuild from `GetPanelIds()`, button
     wiring, loop start) runs unchanged.
3. **`Scenes/MainMenu/MainMenu.cs`**
   - `_Ready`: enable `LoadGameButton` iff `SaveSlotStore.HasSave()`.
   - On press: `Game.RESUME_FROM_SAVE = SaveSlotStore.SavePath;` then
     `ChangeSceneToFile("res://Scenes/GameScene/Game.tscn")`.

## Decisions (resolved)

- **Format:** SQLite `.db`, single file per slot (see `plans/save-sqlite-example/`).
  Queryable (future save-inspector tool = plain SQL), atomic writes, clean
  `user_version` migrations, zero new dependencies.
- **Slots:** v1 = single slot (`save.db`, overwritten). Multi-slot later =
  `save_<n>.db` + a `saves.json` index; the format is already slot-compatible.
- **Module:** not bundled in the save; validity enforced by the module triple
  check in `meta`.
- **Save trigger:** coreUI button + F5, both calling `SaveSlotStore.Save()`.
- **Auto-save on exit:** not in v1; trivial to add in `Game._ExitTree` later.
- **Loop pause:** required for both save and load (barrier above).
- **Failure policy:** load failure bounces to main menu (never a half-hydrated
  game — validate-then-mutate guarantees it); save failure prints an error and
  keeps playing.

## Phases

1. **Rust**
   - `set_scheduled_effects` / `set_active_plans` in `state/accessors.rs`.
   - Save codec: `save_game.rs` / `load_game.rs` (rusqlite; schema per
     `save-sqlite-example/save.sql`); export from `ffi_mod/export/mod.rs` +
     `ffi_mod/mod.rs`.
   - Round-trip unit test: seed dynamic state → `save_game` to temp `.db` →
     `runtime_clear_state` → re-seed module baseline → `load_game` → assert
     entity values, containers, sector grids, areas, scheduled effects, active
     plans, elapsed time all match; plus rejection tests (bad `user_version`,
     module mismatch, corrupt file).
2. **C#** — interop methods; `dotnet build` of `New Game Project.csproj` clean.
3. **Godot** — `SaveSlotStore`, coreUI `SaveGameButton` + F5, main-menu load
   wiring, `RESUME_FROM_SAVE` boot mode, loop barrier.
4. **Verification**
   - Rust: `cargo test` + `cargo build` (produces the dylib the client links).
   - C#: 0 build errors.
   - Manual Godot run: New Game → move the marker → Save (F5) → exit → Load →
     confirm marker position + clock + sector view restored; also New Game
     after a save (must ignore the save); also corrupt `save.db` → Load
     rejected cleanly → back to menu.
