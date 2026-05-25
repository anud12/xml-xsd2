# Investigation Report: TASK-001 — Panel Not Updating at Runtime

**Date**: 2026-05-25
**Status**: Root cause identified with high confidence
**Branch**: `task/TASK-2026-05-25-001-investigate-panel-not-updating`

---

## Executive Summary

**Root Cause**: The main scene (`Scenes/ManualTest.tscn`) and its associated script (`ManualTest.cs`) are a bare-bones placeholder that **never wires up the panel rendering system**. It creates only a static "Hello world" label and does not:

1. Process the game archive via `RuntimeInterop.ProcessArchive()`
2. Instantiate a `RootNode` to create panels from runtime data
3. Call `RuntimeInterop.RunIteration()` to advance game state

The working test scenes (Stage 4) do all three programmatically, which is why panels update correctly in tests but not in the main scene.

**Confidence**: High (95%) — the discrepancy is directly observable in the source code.

---

## Root Cause Analysis

### What the Main Scene Does

**`Scenes/ManualTest.tscn`** (the main scene set in `project.godot`):

The scene tree is:
```
Node (ManualTest.cs)
└── Control
    └── Panel
        ├── RichTextLabel (static "Text")
        └── BoxContainer
```

**`Scenes/ManualTest.cs`** root script:
```csharp
public partial class ManualTest : Node
{
    public override void _Ready()
    {
        var textNode = new Label { Text = "Hello world" };
        AddChild(textNode);
    }

    public override void _Process(double delta) { }  // EMPTY — does nothing
}
```

This script:
- Creates a simple "Hello world" label in `_Ready()`
- Has an **empty** `_Process()` override that does nothing
- **Never** calls `RuntimeInterop.ProcessArchive()`, `RuntimeInterop.RunIteration()`, or creates any `RootNode`/`Panel` instances

### What the Test Scenes Do (Working Reference)

**`Test/Stage_4/EntityNumberValueUpdate/TestClass.cs`** (representative of all working tests):

```csharp
public async Task Given_panel_it_should_update_number_value_when_entity_changes()
{
    // 1. Set up the archive and process it through the Rust runtime
    AddFileToArchive("module/index.js", "index.js")
        .AddFileToArchive("module/manifest.json", "manifest.json")
        .AddFileToArchive("module/texture.exr", "texture.exr")
        .EnsureDllAccessible()
        .ProcessArchive();  // <-- RuntimeInterop.ProcessArchive() called here

    // 2. Load a minimal test scene (just an empty Node)
    var scene = LoadTestScene();

    // 3. Programmatically create RootNode which instantiates all panels
    var rootNode = new RootNode();
    scene.AddChild(rootNode);
    rootNode.SetSize(new Vector2 { X = 1000, Y = 1000 });
    rootNode.SetAnchorsPreset(Control.LayoutPreset.Center);
    await runner.SimulateFrames(1);

    // 4. Assert initial panel content
    var assertions = AssertPanelThat(rootNode.GetNode<Panel>(idList[0]))
        .HasContentText("42");

    // 5. Mutate entity state
    RuntimeInterop.SetEntityNumberMapValue("entity_id", "numberKey", 99);
    RuntimeInterop.RunIteration();  // <-- Advances game state
    await runner.SimulateFrames(1);  // <-- Triggers _Process on all nodes

    // 6. Assert panel content updated
    assertions.HasContentText("99");
}
```

**Key differences:**

| Aspect | Main Scene (`ManualTest`) | Test Scenes (working) |
|--------|--------------------------|----------------------|
| Archive processing | **Never called** | `ProcessArchive()` called before scene setup |
| RootNode instantiation | **Never created** | `new RootNode()` added to scene tree |
| Panel creation | **No panels at all** | `RootNode` creates `Panel` instances for each panel ID from runtime |
| `_Process` loop | **Empty** | `EntityNumberValueContentNode._Process()` and `EntityTextValueContentNode._Process()` read live entity values every frame |
| RunIteration() | **Never called** | Called after state mutation to advance game state |
| Frame simulation | **Relies on Godot's game loop** | `await runner.SimulateFrames(1)` ensures processing |

---

## Panel Update Pipeline (How It Works When Wired Correctly)

### Pipeline Flow

```
RuntimeInterop.RunIteration()
    └── Calls native runtime_run_iteration()
        └── Rust runtime advances game state (entity numbers/text updated in memory)

Every frame (Godot _Process loop):
    EntityNumberValueContentNode._Process(delta)
        └── RuntimeInterop.ReadEntityNumberValue(entityId, name)
            └── Calls native get_entity_number_map_value()
            └── Returns current value from Rust runtime memory

    EntityTextValueContentNode._Process(delta)
        └── RuntimeInterop.ReadEntityTextValue(entityId, name)
            └── Calls native get_entity_text_map_value()
            └── Returns current value from Rust runtime memory
```

### RootNode Bootstrap

`Sources/UI/RootNode.cs`:
```csharp
public partial class RootNode : Godot.Panel
{
    public RootNode()
    {
        SetAnchorsPreset(LayoutPreset.FullRect);
        var idList = RuntimeInterop.GetPanelIds();  // Gets panel IDs from Rust runtime
        foreach (var id in idList)
        {
            var p = new UIPanel(RuntimeInterop.GetPanelById(id))
            {
                Name = id
            };
            base.AddChild(p);
        }
    }
}
```

### Panel Content Node Update Loop

`Sources/UI/EntityNumberValueContentNode.cs` (line 60-62):
```csharp
public override void _Process(double delta)
{
    Text = RuntimeInterop.ReadEntityNumberValue(content.EntityId, content.Name);
}
```

`Sources/UI/EntityTextValueContentNode.cs` (line 60-62):
```csharp
public override void _Process(double delta)
{
    Text = RuntimeInterop.ReadEntityTextValue(content.EntityId, content.Name);
}
```

**These `_Process` loops are correct and working** — they are proven by the Stage 4 tests. The problem is that these nodes are never instantiated in the main scene.

---

## Key Files Examined

| File | Finding |
|------|---------|
| `project.godot` | Main scene is set to `uid://cutilh66xwud4` which maps to `Scenes/ManualTest.tscn` |
| `Scenes/ManualTest.tscn` | Minimal scene: Node → Control → Panel → RichTextLabel("Text") + BoxContainer |
| `Scenes/ManualTest.cs` | Bare-bones script: adds "Hello world" label in `_Ready()`, empty `_Process()` |
| `Scenes/Test.tscn` | Minimal empty scene: just `Node` (no children, no script) — panels are added programmatically |
| `Sources/UI/RootNode.cs` | Bootstrap class that reads panel IDs from runtime and creates `Panel` instances |
| `Sources/UI/Panel.cs` | Renders a single panel with content nodes; constructor adds `EntityNumberValueContentNode` / `EntityTextValueContentNode` as children |
| `Sources/UI/EntityNumberValueContentNode.cs` | `_Process()` loop reads live entity number from runtime every frame ✅ |
| `Sources/UI/EntityTextValueContentNode.cs` | `_Process()` loop reads live entity text from runtime every frame ✅ |
| `Sources/Runtime/RuntimeInterop.cs` | FFI bridge to Rust runtime; `RunIteration()`, `ReadEntityNumberValue()`, `ReadEntityTextValue()`, `ProcessArchive()` all present and working ✅ |
| `Test/Steps/LoadTestScene.cs` | Loads `Scenes/Test.tscn` via `ISceneRunner.Load()` |
| `Test/Steps/AssertPanel.cs` | Assertions for panel structure: `HasContentText()` reads `RichTextLabel("content").Text` |
| `Test/Stage_4/EntityNumberValueUpdate/TestClass.cs` | Working test: shows full pipeline of archive → RootNode → RunIteration → panel update |
| `Test/Stage_4/EntityTextValueUpdate/TestClass.cs` | Working test: same pipeline for text values |

---

## Recommended Fix

The `ManualTest.cs` script needs to be rewritten to:

1. **In `_Ready()`**: Process the archive and create the panel tree:
   ```csharp
   public override void _Ready()
   {
       // 1. Process the game archive
       var dbPath = RuntimeInterop.ProcessArchive("path/to/archive.zip");

       // 2. Create and add RootNode (which bootstraps all panels from runtime)
       var rootNode = new GdUnit4.Examples.Basics.Setup.Sources.UI.RootNode();
       AddChild(rootNode);
       rootNode.SetAnchorsPreset(LayoutPreset.FullRect);
   }
   ```

2. **In `_Process()`**: Call `RunIteration()` to advance game state each frame:
   ```csharp
   public override void _Process(double delta)
   {
       RuntimeInterop.RunIteration();
   }
   ```

3. **Remove the static "Hello world" label** and the static `Panel`/`RichTextLabel` from `ManualTest.tscn` — the scene should only contain the root `Node` with the `ManualTest.cs` script attached.

### Alternative (Cleaner) Approach

Rather than putting everything in `_Process()` (which runs every frame), consider calling `RunIteration()` at a controlled tick rate:

```csharp
public partial class ManualTest : Node
{
    private double _accumulator = 0;
    private const double TICK_RATE = 1.0 / 60.0; // 60 ticks per second

    public override void _Ready()
    {
        var dbPath = RuntimeInterop.ProcessArchive("path/to/archive.zip");
        var rootNode = new GdUnit4.Examples.Basics.Setup.Sources.UI.RootNode();
        AddChild(rootNode);
        rootNode.SetAnchorsPreset(LayoutPreset.FullRect);
    }

    public override void _Process(double delta)
    {
        _accumulator += delta;
        while (_accumulator >= TICK_RATE)
        {
            RuntimeInterop.RunIteration();
            _accumulator -= TICK_RATE;
        }
    }
}
```

### Open Questions for the Fix Task

1. **Archive path**: Where should the game archive be loaded from? The tests build a temporary archive at runtime. The main scene needs a persistent archive path or a file picker mechanism.
2. **Auto-run vs. manual control**: Should `RunIteration()` be called automatically every frame, or should there be a play/pause mechanism?
3. **Scene cleanup**: The current `ManualTest.tscn` has static UI nodes that should be removed since `RootNode` will create all panels dynamically.

---

## Summary

| Aspect | Finding |
|--------|---------|
| **Is the panel update pipeline broken?** | **No** — `EntityNumberValueContentNode._Process()` and `EntityTextValueContentNode._Process()` correctly read live values from the runtime |
| **Is `RunIteration()` broken?** | **No** — it correctly calls the native `runtime_run_iteration()` function |
| **Why doesn't the main scene show updating panels?** | The main scene never creates any panel content nodes. It's a placeholder with a "Hello world" label. The `RootNode` bootstrap is never invoked. |
| **What's missing?** | Three things: (1) `ProcessArchive()` call, (2) `RootNode` instantiation, (3) `RunIteration()` call in the update loop |
| **Can this be fixed?** | Yes — the fix is straightforward: wire up the same initialization sequence that the tests use |
