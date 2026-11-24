using System;
using System.IO;
using GdUnit4.Examples.Basics.Setup.Sources.UI;
using Godot;
using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;
using Vector2 = Godot.Vector2;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_temp.FullTestTick;

[TestSuite]
public partial class FullTestTickFfiTests : Steps {
    const string ModuleRoot = "E:\\workspace\\xml-xsd2\\application\\client\\solution\\Modules\\TestTick";

    [TestCategory("Stage_temp")]
    [TestCase(Timeout = 30_000)]
    [RequireGodotRuntime]
    public async Task Given_testtick_minimap_when_empty_tile_clicked_mcv_teleports() {
        CleanupArchive();
        foreach (var file in Directory.GetFiles(ModuleRoot, "*.*", SearchOption.AllDirectories)) {
            var rel = Path.GetRelativePath(ModuleRoot, file).Replace('\\', '/');
            AddFileToArchive("../../../../Modules/TestTick/" + rel, rel);
        }
        ProcessArchive();
        var scene = await AttachUiScene();
        RuntimeInterop.RunIteration();
        await runner.SimulateFrames(3);

        var view = scene.GetWindowOrNull("mcv-map-view");
        Assertions.AssertThat(view != null).IsTrue();
        var screen = scene.Window("__ui-screen-1");
        Assertions.AssertThat(view!.Position).IsEqual((screen.Size - new Vector2(800, 600)) / 2f);
        int count = 0;
        UiWindow? target = null;
        for (int y = 0; y < 10; y++) for (int x = 0; x < 10; x++) {
            var tile = scene.GetWindowOrNull($"$$groupViewEmpty_12_mcv-map-view_{x}_{y}");
            if (tile != null) count++;
            // Stay well outside the 280x140 MCV window over the top-left map.
            if (x == 2 && y == 5) target = tile;
        }
        Assertions.AssertThat(count).IsEqual(99);
        Assertions.AssertThat(target != null).IsTrue();
        Assertions.AssertThat(target!.MouseFilter == Control.MouseFilterEnum.Pass).IsTrue();
        var pos = target!.GlobalPosition + target.Size / 2f;
        // Verify the live tile is actually positioned at cell (2,5), rather
        // than merely existing in the snapshot, and the floating window is not in the way.
        Assertions.AssertThat(target.GlobalPosition - view!.GlobalPosition).IsEqual(new Vector2(160, 300));
        Assertions.AssertThat(scene.GetWindowOrNull("mcv-window")!.GetGlobalRect().HasPoint(pos)).IsFalse();
        var viewport = runner.Scene().GetViewport();
        viewport.PushInput(new InputEventMouseButton { Position = pos, GlobalPosition = pos,
            ButtonIndex = MouseButton.Left, Pressed = true, ButtonMask = MouseButtonMask.Left });
        viewport.PushInput(new InputEventMouseButton { Position = pos, GlobalPosition = pos,
            ButtonIndex = MouseButton.Left, Pressed = false, ButtonMask = 0 });
        await runner.SimulateFrames(2);
        RuntimeInterop.RunIteration();
        await runner.SimulateFrames(2);
        Assertions.AssertThat(double.Parse(RuntimeInterop.ReadEntityNumberValue("mcv", "x"))).IsEqual(2.0);
        Assertions.AssertThat(double.Parse(RuntimeInterop.ReadEntityNumberValue("mcv", "y"))).IsEqual(5.0);
        ClearSimulatedMouse();
    }

    [TestCategory("Stage_temp")]
    [TestCase(Timeout = 30_000)]
    [RequireGodotRuntime]
    public async Task Given_generated_land_when_bought_it_moves_to_inventory_and_is_displayed() {
        CleanupArchive();
        foreach (var file in Directory.GetFiles(ModuleRoot, "*.*", SearchOption.AllDirectories)) {
            var rel = Path.GetRelativePath(ModuleRoot, file).Replace('\\', '/');
            AddFileToArchive("../../../../Modules/TestTick/" + rel, rel);
        }
        ProcessArchive();
        var scene = await AttachUiScene();

        RuntimeInterop.RunIteration(10);
        await runner.SimulateFrames(3);
        var firstLand = EntityInterop.GetEntityById("lands")!.Members("members")[0];
        var initialValue = double.Parse(RuntimeInterop.ReadEntityNumberValue("game", "value"));
        Assertions.AssertThat(initialValue < double.Parse(RuntimeInterop.ReadEntityNumberValue(firstLand, "cost"))).IsTrue();
        RuntimeInterop.UiJsClick(firstLand + ":buy", 0, 0);
        RuntimeInterop.RunIteration();
        Assertions.AssertThat(EntityInterop.GetEntityById("lands")!.Members("members").Contains(firstLand)).IsTrue();
        Assertions.AssertThat(EntityInterop.GetEntityById("inventory")!.Members("members").Length).IsEqual(0);
        Assertions.AssertThat(double.Parse(RuntimeInterop.ReadEntityNumberValue("game", "value"))).IsEqual(initialValue);

        for (int i = 0; i < 40; i++) {
            RuntimeInterop.RunIteration(10);
            await runner.SimulateFrames(1);
        }

        var landId = EntityInterop.GetEntityById("lands")!.Members("members")[0];
        var name = RuntimeInterop.GetEntityTextMapValue(landId, "name");
        var cost = double.Parse(RuntimeInterop.ReadEntityNumberValue(landId, "cost"));
        var before = double.Parse(RuntimeInterop.ReadEntityNumberValue("game", "value"));
        Assertions.AssertThat(double.Parse(RuntimeInterop.ReadEntityNumberValue(landId, "land"))).IsEqual(1.0);
        Assertions.AssertThat(before >= cost).IsTrue();

        RuntimeInterop.UiJsClick(landId + ":buy", 0, 0);
        RuntimeInterop.RunIteration();
        await runner.SimulateFrames(3);

        Assertions.AssertThat(EntityInterop.GetEntityById("lands")!.Members("members").Contains(landId)).IsFalse();
        Assertions.AssertThat(EntityInterop.GetEntityById("inventory")!.Members("members").Contains(landId)).IsTrue();
        Assertions.AssertThat(double.Parse(RuntimeInterop.ReadEntityNumberValue("game", "value"))).IsEqual(before - cost);
        Assertions.AssertThat(RuntimeInterop.GetEntityTextMapValue(landId, "name")).IsEqual(name);
        var inventoryWindow = scene.GetWindowOrNull("inventory-list");
        var inventoryItem = scene.GetWindowOrNull(landId);
        Assertions.AssertThat(inventoryWindow != null && inventoryItem != null && inventoryWindow.IsAncestorOf(inventoryItem)).IsTrue();

        RuntimeInterop.PublishActionArgsAndQueueEffect("inventory-buy", "inventory-purchase", "{\"entityId\":\"" + landId + "\"}");
        RuntimeInterop.RunIteration();
        Assertions.AssertThat(double.Parse(RuntimeInterop.ReadEntityNumberValue("game", "value"))).IsEqual(before - cost);
        Assertions.AssertThat(EntityInterop.GetEntityById("inventory")!.Members("members").Count(id => id == landId)).IsEqual(1);

        var secondLand = EntityInterop.GetEntityById("lands")!.Members("members")[0];
        RuntimeInterop.UiJsClick(secondLand + ":buy", 0, 0);
        RuntimeInterop.RunIteration();
        await runner.SimulateFrames(3);
        var secondItem = scene.GetWindowOrNull(secondLand);
        Assertions.AssertThat(EntityInterop.GetEntityById("inventory")!.Members("members").Length).IsEqual(2);
        Assertions.AssertThat(secondItem != null && inventoryWindow!.IsAncestorOf(secondItem)).IsTrue();
    }

    [TestCategory("Stage_temp")]
    [TestCase]
    [RequireGodotRuntime]
    public async Task Given_full_testtick_module_when_ticks_run_add_btn_label_stays_clean() {
        CleanupArchive();
        // Load the real, compiled TestTick module (all files, correct in-archive paths).
        foreach (var file in Directory.GetFiles(ModuleRoot, "*.*", SearchOption.AllDirectories)) {
            var rel = Path.GetRelativePath(ModuleRoot, file).Replace('\\', '/');
            AddFileToArchive("../../../../Modules/TestTick/" + rel, rel);
        }
        ProcessArchive();

        var scene = await AttachUiScene();

        // Advance time so the 1ms interest/value ticks and the 10ms land tick fire
        // many times, stressing the add-btn label re-render.
        for (int i = 0; i < 40; i++) {
            RuntimeInterop.RunIteration(10);
            await runner.SimulateFrames(1);
        }

        var lands = EntityInterop.GetEntityById("lands")!;
        var label = scene.Window("add-btn").GetNodeOrNull<Label>("text");
        var labelText = label?.Text ?? "<none>";
        var property = RuntimeInterop.ReadEntityNumberValue("game", "property");
        var childExists = scene.GetWindowOrNull("add-label") != null;
        Console.WriteLine($"DEBUG FullTestTick lands={lands.Members("members").Length} add-btn label='{labelText}' property='{property}' add-labelWindowExists={childExists}");

        // The add-btn label must show exactly the current property value as a
        // single clean number — never a concatenation of previous values.
        bool labelClean = double.TryParse(labelText, out var lv)
            && double.TryParse(property, out var pv)
            && lv == pv;
        Assertions.AssertThat(labelClean)
            .OverrideFailureMessage($"add-btn label '{labelText}' != current property '{property}' (text is not cleared / is accumulating)")
            .IsTrue();
    }

    [TestCategory("Stage_temp")]
    [TestCase]
    [RequireGodotRuntime]
    public async Task Given_full_module_when_add_btn_clicked_repeatedly_label_stays_clean() {
        CleanupArchive();
        foreach (var file in Directory.GetFiles(ModuleRoot, "*.*", SearchOption.AllDirectories)) {
            var rel = Path.GetRelativePath(ModuleRoot, file).Replace('\\', '/');
            AddFileToArchive("../../../../Modules/TestTick/" + rel, rel);
        }
        ProcessArchive();

        var scene = await AttachUiScene();

        // Let the 1ms interest tick drift property, and simulate the user rapidly
        // clicking the add-btn (each click emits the `increment` action).
        for (int i = 0; i < 30; i++) {
            RuntimeInterop.RunIteration(10);
            RuntimeInterop.UiJsClick("add-btn", 0, 0);
            await runner.SimulateFrames(1);
        }

        var label = scene.Window("add-btn").GetNodeOrNull<Label>("text");
        var labelText = label?.Text ?? "<none>";
        var property = RuntimeInterop.ReadEntityNumberValue("game", "property");
        Console.WriteLine($"DEBUG FullTestTick-click add-btn label='{labelText}' property='{property}'");

        bool labelClean = double.TryParse(labelText, out var lv)
            && double.TryParse(property, out var pv)
            && lv == pv;
        Assertions.AssertThat(labelClean)
            .OverrideFailureMessage($"add-btn label '{labelText}' != current property '{property}' after clicks (text is not cleared / is accumulating)")
            .IsTrue();
    }

    /// Drives a live mouse drag of a land (a draggable child window in the land
    /// list) onto the inventory window (a droppable). The "land" drag type's
    /// onDrop fires the inventory's buy action, so the land is purchased: it
    /// leaves the "lands" container and enters "inventory". Mirrors the live-drag
    /// mechanics of Stage_16/ContainerDrag, but against the full compiled module.
    [TestCategory("Stage_temp")]
    [TestCase(Timeout = 30_000)]
    [RequireGodotRuntime]
    public async Task Given_full_module_when_a_land_is_dragged_to_the_inventory_it_is_bought() {
        try {
            CleanupArchive();
            foreach (var file in Directory.GetFiles(ModuleRoot, "*.*", SearchOption.AllDirectories)) {
                var rel = Path.GetRelativePath(ModuleRoot, file).Replace('\\', '/');
                AddFileToArchive("../../../../Modules/TestTick/" + rel, rel);
            }
            ProcessArchive();

            var scene = await AttachUiScene();

            // Advance the runtime so the land-tick (10ms) tops the "lands" pool
            // up to its cap (4) and the entityLists expand into per-land child
            // windows, then pump frames so those windows are real Godot nodes.
            for (int i = 0; i < 25; i++) {
                RuntimeInterop.RunIteration(10);
                await runner.SimulateFrames(1);
            }
            await runner.SimulateFrames(2);

            var lands = EntityInterop.GetEntityById("lands")!;
            Assertions.AssertThat(lands.Members("members").Length).IsGreater(0);
            var landId = lands.Members("members")[0];
            // The drag handle is the land's portrait (a child-less panel), not
            // the whole card — the card's stats/buy panels consume a press
            // before the drag handler would see it.
            var card = scene.GetWindowOrNull(landId);
            var cardDescendants = card != null ? CollectDescendantNames(card, 1) : "(card not found)";
            Assertions.AssertThat(card != null)
                .OverrideFailureMessage($"land card '{landId}' should exist (descendants: {cardDescendants})")
                .IsTrue();
            var source = FindDescendantWindow(card!, "portrait");
            Assertions.AssertThat(source != null)
                .OverrideFailureMessage($"land '{landId}' portrait drag handle should exist under the card (descendants: {cardDescendants})")
                .IsTrue();
            var target = scene.GetWindowOrNull("inventory-list");
            Assertions.AssertThat(target != null)
                .OverrideFailureMessage("inventory-list window should exist (the drop target)")
                .IsTrue();

            var sourceNode = source!;
            var targetNode = target!;

            // Live mouse drag: press on the portrait, cross the 4-px drag
            // threshold, move over the inventory window, release.
            var pressGlobal = sourceNode.GlobalPosition + sourceNode.Size / 2f;
            var releaseGlobal = targetNode.GlobalPosition + targetNode.Size / 2f;
            var viewport = runner.Scene().GetViewport();

            // Initial: nothing is dragging before any input.
            Assertions.AssertThat(sourceNode.IsDragging).IsFalse();

            viewport.PushInput(new InputEventMouseButton {
                Position = pressGlobal,
                GlobalPosition = pressGlobal,
                ButtonIndex = MouseButton.Left,
                Pressed = true,
                ButtonMask = MouseButtonMask.Left
            });
            var midGlobal = pressGlobal + new Vector2(20, 0); // >4px, still over the land
            viewport.PushInput(new InputEventMouseMotion {
                Position = midGlobal,
                GlobalPosition = midGlobal,
                Relative = midGlobal - pressGlobal
            });
            await runner.SimulateFrames(2);

            Assertions.AssertThat(sourceNode.IsDragging)
                .OverrideFailureMessage("the land portrait should report IsDragging once the drag has started")
                .IsTrue();

            viewport.PushInput(new InputEventMouseMotion {
                Position = releaseGlobal,
                GlobalPosition = releaseGlobal,
                Relative = releaseGlobal - midGlobal
            });
            await runner.SimulateFrames(2);

            Assertions.AssertThat(sourceNode.DragHoverTarget == targetNode)
                .OverrideFailureMessage("the drag hover target should be the inventory while the pointer is over it")
                .IsTrue();

            viewport.PushInput(new InputEventMouseButton {
                Position = releaseGlobal,
                GlobalPosition = releaseGlobal,
                ButtonIndex = MouseButton.Left,
                Pressed = false,
                ButtonMask = 0
            });
            await runner.SimulateFrames(2);

            // The release resolves the drop: the "land" drag type's onDrop emits
            // the inventory's buy action (a queued plan). Advance the runtime once
            // so the plan is applied (deduct cost + move the land from "lands" to
            // "inventory"), then pump frames so the entityLists re-render.
            RuntimeInterop.RunIteration();
            await runner.SimulateFrames(2);

            // END-TO-END: the dragged land was purchased — it left "lands" and
            // entered "inventory".
            var landsAfter = EntityInterop.GetEntityById("lands")!;
            var invAfter = EntityInterop.GetEntityById("inventory")!;
            Assertions.AssertThat(landsAfter.Members("members").Contains(landId))
                .OverrideFailureMessage($"land '{landId}' should no longer be in 'lands' after the drag-buy")
                .IsFalse();
            Assertions.AssertThat(invAfter.Members("members").Contains(landId))
                .OverrideFailureMessage($"land '{landId}' should be in 'inventory' after the drag-buy")
                .IsTrue();

            ClearSimulatedMouse();
        }
        catch (Exception e) {
            Assertions.AssertThat(true)
                .OverrideFailureMessage($"Error: {e.Message}\n{e.StackTrace}")
                .IsFalse();
        }
    }

    /// Returns the deepest descendant <see cref="UiWindow"/> whose node name
    /// contains the given substring (used to locate the portrait drag handle,
    /// whose declared id may be normalized in the Godot node name).
    static UiWindow? FindDescendantWindow(Node root, string nameSubstring) {
        foreach (var child in root.GetChildren()) {
            if (child is UiWindow win) {
                if (win.Name.ToString().Contains(nameSubstring)) return win;
                var nested = FindDescendantWindow(win, nameSubstring);
                if (nested != null) return nested;
            }
            else {
                var nested = FindDescendantWindow(child, nameSubstring);
                if (nested != null) return nested;
            }
        }
        return null;
    }

    /// Collects the descendant node names (indented by depth) for diagnostics.
    static string CollectDescendantNames(Node root, int depth) {
        var sb = new System.Text.StringBuilder();
        foreach (var child in root.GetChildren()) {
            sb.Append(new string(' ', 2 * depth)).Append(child.Name.ToString());
            if (child is UiWindow) sb.Append(" [UiWindow]");
            sb.Append('\n');
            sb.Append(CollectDescendantNames(child, depth + 1));
        }
        return sb.ToString();
    }
}
