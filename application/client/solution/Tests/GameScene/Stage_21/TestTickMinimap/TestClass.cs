using System;
using GdUnit4.Examples.Basics.Setup.Sources.UI;
using Godot;
using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;
using Vector2 = Godot.Vector2;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_21.TestTickMinimap;

[TestSuite]
public partial class TestClass : Steps {
    [TestCategory("Stage_21")]
    [TestCase(Timeout = 30_000)]
    [RequireGodotRuntime]
    public async Task Given_testtick_minimap_when_empty_tile_clicked_mcv_teleports() {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js").AddFileToArchive("module/manifest.json", "manifest.json").AddFileToArchive("module/tile.png", "tile.png").AddFileToArchive("module/marker.png", "marker.png").EnsureDllAccessible().ProcessArchive();
        var scene = await AttachUiScene();
        RuntimeInterop.RunIteration();
        await runner.SimulateFrames(3);

        var view = scene.GetWindowOrNull("mcv-map-view");
        Assertions.AssertThat(view != null).IsTrue();
        var map = scene.Window("mcv-map-view");
        AssertPanelThat(map).ViewportMatches("teleport_before.png");
        var screen = scene.Window("__ui-screen-1");
        Assertions.AssertThat(view!.Position).IsEqual((screen.Size - new Vector2(800, 600)) / 2f);
        int count = 0;
        UiWindow? target = null;
        for (int y = 0; y < 10; y++) for (int x = 0; x < 10; x++) {
            var tile = scene.GetWindowOrNull($"$$groupViewEmpty_12_mcv-map-view_{x}_{y}");
            if (tile != null) count++;
            if (x == 2 && y == 5) target = tile;
        }
        Assertions.AssertThat(count).IsEqual(99);
        Assertions.AssertThat(target != null).IsTrue();
        Assertions.AssertThat(target!.MouseFilter == Control.MouseFilterEnum.Pass).IsTrue();
        var pos = target!.GlobalPosition + target.Size / 2f;
        // Verify the live tile is actually positioned at cell (2,5), rather
        // than merely existing in the snapshot.
        Assertions.AssertThat(target.GlobalPosition - view!.GlobalPosition).IsEqual(new Vector2(160, 300));
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
        AssertPanelThat(map).ViewportMatches("teleport_after.png");
        ClearSimulatedMouse();
    }

    [TestCategory("Stage_21")]
    [TestCase(Timeout = 30_000)]
    [RequireGodotRuntime]
    public async Task Minimap_teleport_under_stationary_cursor_shows_tooltip() {
        try {
            CleanupArchive();
            AddFileToArchive("module/index.js", "index.js").AddFileToArchive("module/manifest.json", "manifest.json").AddFileToArchive("module/tile.png", "tile.png").AddFileToArchive("module/marker.png", "marker.png").EnsureDllAccessible().ProcessArchive();
            SimulateMouse(Vector2.Zero);
            var scene = await AttachUiScene();
            RuntimeInterop.RunIteration();
            await runner.SimulateFrames(3);
            var map = scene.Window("mcv-map-view");
            var target = map.GlobalPosition + new Vector2(200, 330);
            HoverAt(target);
            await runner.SimulateFrames(10);
            Assertions.AssertThat(scene.Root.FindWindowById("mcv-map-view:hover-window")).IsNull();
            var viewport = runner.Scene().GetViewport();
            viewport.PushInput(new InputEventMouseButton { Position = target, GlobalPosition = target, ButtonIndex = MouseButton.Left, Pressed = true, ButtonMask = MouseButtonMask.Left });
            viewport.PushInput(new InputEventMouseButton { Position = target, GlobalPosition = target, ButtonIndex = MouseButton.Left, Pressed = false });
            await runner.SimulateFrames(2);
            RuntimeInterop.RunIteration();
            await runner.SimulateFrames(15);
            Assertions.AssertThat(double.Parse(RuntimeInterop.ReadEntityNumberValue("mcv", "x"))).IsEqual(2.0);
            Assertions.AssertThat(double.Parse(RuntimeInterop.ReadEntityNumberValue("mcv", "y"))).IsEqual(5.0);
            var tooltip = scene.Root.FindWindowById("mcv-map-view:hover-window");
            Assertions.AssertThat(tooltip).IsNotNull();
            if (tooltip != null) Assertions.AssertThat(tooltip.GlobalPosition).IsEqual(target + new Vector2(12, 12));
            await AssertPanelThat(tooltip!).AssertRenderMatches("stationary_after.png");
        }
        finally { ClearSimulatedMouse(); }
    }

    [TestCategory("Stage_21")]
    [TestCase(Timeout = 30_000)]
    [RequireGodotRuntime]
    public async Task Minimap_hover_shows_and_dismisses_stats() {
        try {
            CleanupArchive();
            AddFileToArchive("module/index.js", "index.js").AddFileToArchive("module/manifest.json", "manifest.json").AddFileToArchive("module/tile.png", "tile.png").AddFileToArchive("module/marker.png", "marker.png").EnsureDllAccessible().ProcessArchive();
            SimulateMouse(new Vector2(0, 0));
            var scene = await AttachUiScene();
            RuntimeInterop.RunIteration();
            await runner.SimulateFrames(3);
            var map = scene.Window("mcv-map-view");
            Assertions.AssertThat(scene.Root.FindWindowById("mcv-map-view:hover-window")).IsNull();
            // Move the MCV away from the origin before opening the tooltip.
            var click = map.GlobalPosition + new Vector2(200, 330);
            var viewport = runner.Scene().GetViewport();
            viewport.PushInput(new InputEventMouseButton { Position = click, GlobalPosition = click, ButtonIndex = MouseButton.Left, Pressed = true, ButtonMask = MouseButtonMask.Left });
            viewport.PushInput(new InputEventMouseButton { Position = click, GlobalPosition = click, ButtonIndex = MouseButton.Left, Pressed = false });
            await runner.SimulateFrames(2);
            RuntimeInterop.RunIteration();
            await runner.SimulateFrames(2);
            var mouse = map.GlobalPosition + new Vector2(400, 400);
            HoverAt(mouse);
            await runner.SimulateFrames(10);
            Assertions.AssertThat(scene.Root.FindWindowById("mcv-map-view:hover-window")).IsNull();
            mouse = map.GlobalPosition + new Vector2(200, 330);
            HoverAt(mouse);
            await runner.SimulateFrames(10);
            var stats = scene.Root.FindWindowById("mcv-map-view:hover-window");
            Assertions.AssertThat(stats).IsNotNull();
            Assertions.AssertThat(scene.Window("stats-x").GetNode<Label>("text").Text).IsEqual(RuntimeInterop.ReadEntityNumberValue("mcv", "x"));
            Assertions.AssertThat(scene.Window("stats-y").GetNode<Label>("text").Text).IsEqual(RuntimeInterop.ReadEntityNumberValue("mcv", "y"));
            Assertions.AssertThat(double.Parse(RuntimeInterop.ReadEntityNumberValue("mcv", "x"))).IsEqual(2.0);
            Assertions.AssertThat(double.Parse(RuntimeInterop.ReadEntityNumberValue("mcv", "y"))).IsEqual(5.0);
            var resource = scene.Window("stats-value").GetNode<Label>("text");
            Assertions.AssertThat(resource.Text).IsEqual("70");
            await AssertPanelThat(stats!).AssertRenderMatches("stats_hover_visible.png");
            var originalPosition = stats!.GlobalPosition;
            RuntimeInterop.SetEntityNumberMapValue("mcv", "resource", 42);
            RuntimeInterop.RunIteration();
            await runner.SimulateFrames(5);
            Assertions.AssertThat(resource.Text).IsEqual("42");
            Assertions.AssertThat(scene.Root.FindWindowById("mcv-map-view:hover-window") == stats).IsTrue();
            Assertions.AssertThat(stats.GlobalPosition).IsEqual(originalPosition);
            if (stats != null)
                Assertions.AssertThat(stats.GlobalPosition).IsEqual(mouse + new Vector2(12, 12));
            await AssertPanelThat(stats!).AssertRenderMatches("stats_updated.png");
            mouse += new Vector2(10, 10);
            HoverAt(mouse);
            await runner.SimulateFrames(5);
            if (stats != null)
                Assertions.AssertThat(stats.GlobalPosition).IsEqual(mouse + new Vector2(12, 12));
            HoverAt(map.GlobalPosition + new Vector2(700, 500)); // outside both map cell and tooltip
            await runner.SimulateFrames(10);
            Assertions.AssertThat(scene.Root.FindWindowById("mcv-map-view:hover-window")).IsNull();
            HoverAt(mouse);
            await runner.SimulateFrames(10);
            Assertions.AssertThat(scene.Root.FindWindowById("mcv-map-view:hover-window")).IsNotNull();
            HoverAt(new Vector2(0, 0));
            await runner.SimulateFrames(10);
            Assertions.AssertThat(scene.Root.FindWindowById("mcv-map-view:hover-window")).IsNull();
        }
        finally { ClearSimulatedMouse(); }
    }

    [TestCategory("Stage_21")]
    [TestCase(Timeout = 30_000)]
    [RequireGodotRuntime]
    public async Task Minimap_tooltip_pin_freezes_and_drag_persists_until_unpin() {
        try {
            CleanupArchive();
            AddFileToArchive("module/index.js", "index.js").AddFileToArchive("module/manifest.json", "manifest.json").AddFileToArchive("module/tile.png", "tile.png").AddFileToArchive("module/marker.png", "marker.png").EnsureDllAccessible().ProcessArchive();
            SimulateMouse(Vector2.Zero);
            var scene = await AttachUiScene();
            RuntimeInterop.RunIteration();
            await runner.SimulateFrames(3);
            var map = scene.Window("mcv-map-view");
            var target = map.GlobalPosition + new Vector2(200, 330);
            var viewport = runner.Scene().GetViewport();
            viewport.PushInput(new InputEventMouseButton { Position = target, GlobalPosition = target, ButtonIndex = MouseButton.Left, Pressed = true, ButtonMask = MouseButtonMask.Left });
            viewport.PushInput(new InputEventMouseButton { Position = target, GlobalPosition = target, ButtonIndex = MouseButton.Left, Pressed = false });
            await runner.SimulateFrames(2);
            RuntimeInterop.RunIteration();
            await runner.SimulateFrames(2);
            HoverAt(target);
            await runner.SimulateFrames(10);
            var window = scene.Root.FindWindowById("mcv-map-view:hover-window");
            Assertions.AssertThat(window).IsNotNull();
            await AssertPanelThat(window!).AssertRenderMatches("pin_hover.png");
            var windowName = window!.Name.ToString();
            var indicator = window.GetNode<Button>("__pinIndicator");
            var label = indicator.GetNode<Label>("key");
            Assertions.AssertThat(label.Text).IsEqual("P");
            Assertions.AssertThat(indicator.IsVisibleInTree()).IsTrue();
            Assertions.AssertThat(indicator.MouseFilter == Control.MouseFilterEnum.Ignore).IsTrue();
            var initial = window.GlobalPosition;
            // One coupled cursor glides northwest and southeast, including over
            // the old tooltip rectangle. Every sample and extra frame is stable.
            async Task MoveCursor(Vector2 pos) {
                HoverAt(pos);
                viewport.PushInput(new InputEventMouseMotion { Position = pos, GlobalPosition = pos });
                await runner.SimulateFrames(1);
            }
            for (int step = 1; step <= 15; step++) {
                var pos = target - new Vector2(step, step);
                await MoveCursor(pos);
                Assertions.AssertThat(scene.Root.FindWindowById("mcv-map-view:hover-window") == window).IsTrue();
                Assertions.AssertThat(window.GlobalPosition).IsEqual(pos + new Vector2(12, 12));
            }
            for (int step = -14; step <= 20; step++) {
                var pos = target + new Vector2(step, step);
                await MoveCursor(pos);
                Assertions.AssertThat(scene.Root.FindWindowById("mcv-map-view:hover-window") == window).IsTrue();
                Assertions.AssertThat(window.GlobalPosition).IsEqual(pos + new Vector2(12, 12));
                await runner.SimulateFrames(1);
                Assertions.AssertThat(window.GlobalPosition).IsEqual(pos + new Vector2(12, 12));
            }
            await MoveCursor(target);
            initial = window.GlobalPosition;
            var pinPos = indicator.GlobalPosition + indicator.Size / 2;
            scene.Root._UnhandledKeyInput(new InputEventKey { Keycode = Key.P, Pressed = true });
            Assertions.AssertThat(label.GetThemeColor("font_color") == Colors.Gold).IsTrue();
            Assertions.AssertThat(indicator.MouseFilter == Control.MouseFilterEnum.Stop).IsTrue();
            Assertions.AssertThat(window.GlobalPosition).IsEqual(initial);
            var resource = scene.Window("stats-value").GetNode<Label>("text");
            RuntimeInterop.SetEntityNumberMapValue("mcv", "resource", 41);
            RuntimeInterop.RunIteration();
            await runner.SimulateFrames(5);
            Assertions.AssertThat(resource.Text).IsEqual("41");
            Assertions.AssertThat(scene.Root.FindWindowById("mcv-map-view:hover-window") == window).IsTrue();
            Assertions.AssertThat(label.GetThemeColor("font_color") == Colors.Gold).IsTrue();
            Assertions.AssertThat(window.GlobalPosition).IsEqual(initial);
            await AssertPanelThat(window).AssertRenderMatches("pin_pinned_updated.png");
            HoverAt(pinPos); // hovering an independent pinned window is harmless
            await runner.SimulateFrames(5);
            Assertions.AssertThat(scene.Root.FindWindowById("mcv-map-view:hover-window") == window).IsTrue();
            Assertions.AssertThat(window.GlobalPosition).IsEqual(initial);
            await MoveCursor(target - new Vector2(15, 15));
            Assertions.AssertThat(window.GlobalPosition).IsEqual(initial);
            await MoveCursor(target + new Vector2(15, 15));
            Assertions.AssertThat(window.GlobalPosition).IsEqual(initial);
            // Unpin while the source remains active: the transient must survive until exit.
            HoverAt(target);
            await runner.SimulateFrames(5);
            viewport.PushInput(new InputEventMouseButton { Position = pinPos, GlobalPosition = pinPos, ButtonIndex = MouseButton.Left, Pressed = true, ButtonMask = MouseButtonMask.Left });
            viewport.PushInput(new InputEventMouseButton { Position = pinPos, GlobalPosition = pinPos, ButtonIndex = MouseButton.Left, Pressed = false });
            await runner.SimulateFrames(2);
            Assertions.AssertThat(scene.Root.FindWindowById("mcv-map-view:hover-window") == window).IsTrue();
            Assertions.AssertThat(label.GetThemeColor("font_color") != Colors.Gold).IsTrue();
            Assertions.AssertThat(indicator.MouseFilter == Control.MouseFilterEnum.Ignore).IsTrue();
            await AssertPanelThat(window).AssertRenderMatches("pin_unpinned_hover.png");
            await MoveCursor(target - new Vector2(5, 5));
            Assertions.AssertThat(window.GlobalPosition).IsEqual(target + new Vector2(7, 7));
            HoverAt(new Vector2(0, 0));
            await runner.SimulateFrames(10);
            Assertions.AssertThat(scene.Root.FindWindowById("mcv-map-view:hover-window")).IsNull();
            HoverAt(target);
            await runner.SimulateFrames(10);
            window = scene.Root.FindWindowById("mcv-map-view:hover-window");
            Assertions.AssertThat(window).IsNotNull();
            indicator = window!.GetNode<Button>("__pinIndicator");
            label = indicator.GetNode<Label>("key");
            initial = window.GlobalPosition;
            resource = scene.Window("stats-value").GetNode<Label>("text");
            scene.Root._UnhandledKeyInput(new InputEventKey { Keycode = Key.P, Pressed = true });
            Assertions.AssertThat(label.GetThemeColor("font_color") == Colors.Gold).IsTrue();
            HoverAt(new Vector2(0, 0)); // leave both the source and pinned window
            await runner.SimulateFrames(10);
            RuntimeInterop.SetEntityNumberMapValue("mcv", "resource", 39);
            RuntimeInterop.RunIteration();
            await runner.SimulateFrames(5);
            Assertions.AssertThat(resource.Text).IsEqual("39");
            Assertions.AssertThat(scene.Root.FindWindowById("mcv-map-view:hover-window") == window).IsTrue();
            Assertions.AssertThat(label.GetThemeColor("font_color") == Colors.Gold).IsTrue();
            Assertions.AssertThat(window.GlobalPosition).IsEqual(initial);
            await AssertPanelThat(window).AssertRenderMatches("pin_persisted_outside.png");
            HoverAt(target); // re-enter the source without creating a second window
            await runner.SimulateFrames(10);
            Assertions.AssertThat(scene.Root.FindWindowById("mcv-map-view:hover-window") == window).IsTrue();
            Assertions.AssertThat(scene.Root.FindChildren(windowName, "", true, false).Count).IsEqual(1);
            HoverAt(new Vector2(0, 0));
            await runner.SimulateFrames(10);
            RuntimeInterop.SetEntityNumberMapValue("mcv", "resource", 37);
            RuntimeInterop.RunIteration();
            await runner.SimulateFrames(5);
            Assertions.AssertThat(resource.Text).IsEqual("37");
            scene.Root._UnhandledKeyInput(new InputEventKey { Keycode = Key.P, Pressed = true });
            await runner.SimulateFrames(2);
            Assertions.AssertThat(scene.Root.FindWindowById("mcv-map-view:hover-window") == window).IsTrue();
            Assertions.AssertThat(label.GetThemeColor("font_color") == Colors.Gold).IsTrue();
            Assertions.AssertThat(window.GetNode<Control>("__titleBar").MouseFilter == Control.MouseFilterEnum.Stop).IsTrue();
            var p = window.GlobalPosition + new Vector2(10, 10);
            var q = p + new Vector2(30, 20);
            viewport.PushInput(new InputEventMouseButton { Position = p, GlobalPosition = p, ButtonIndex = MouseButton.Left, Pressed = true, ButtonMask = MouseButtonMask.Left });
            viewport.PushInput(new InputEventMouseMotion { Position = q, GlobalPosition = q, Relative = q - p });
            viewport.PushInput(new InputEventMouseButton { Position = q, GlobalPosition = q, ButtonIndex = MouseButton.Left, Pressed = false });
            await runner.SimulateFrames(2);
            var moved = window.GlobalPosition;
            Assertions.AssertThat(moved != initial).IsTrue();
            HoverAt(map.GlobalPosition + new Vector2(500, 500));
            await runner.SimulateFrames(10);
            Assertions.AssertThat(window.GlobalPosition).IsEqual(moved);
            await AssertPanelThat(window).AssertRenderMatches("pin_dragged.png");
            pinPos = indicator.GlobalPosition + indicator.Size / 2;
            viewport.PushInput(new InputEventMouseButton { Position = pinPos, GlobalPosition = pinPos, ButtonIndex = MouseButton.Left, Pressed = true, ButtonMask = MouseButtonMask.Left });
            viewport.PushInput(new InputEventMouseButton { Position = pinPos, GlobalPosition = pinPos, ButtonIndex = MouseButton.Left, Pressed = false });
            await runner.SimulateFrames(2);
            Assertions.AssertThat(scene.Root.FindWindowById("mcv-map-view:hover-window")).IsNull();
            await runner.SimulateFrames(10);
            RuntimeInterop.RunIteration();
            await runner.SimulateFrames(3);
            Assertions.AssertThat(scene.Root.FindWindowById("mcv-map-view:hover-window")).IsNull();
            Assertions.AssertThat(scene.Root.FindChildren(windowName, "", true, false).Count).IsEqual(0);
        }
        finally { ClearSimulatedMouse(); }
    }

    [TestCategory("Stage_21")]
    [TestCase(Timeout = 30_000)]
    [RequireGodotRuntime]
    public async Task Minimap_enter_and_boundary_hover_decisions() {
        try {
            CleanupArchive();
            AddFileToArchive("module/index.js", "index.js").AddFileToArchive("module/manifest.json", "manifest.json").AddFileToArchive("module/tile.png", "tile.png").AddFileToArchive("module/marker.png", "marker.png").EnsureDllAccessible().ProcessArchive();
            var scene = await AttachUiScene();
            RuntimeInterop.RunIteration();
            await runner.SimulateFrames(3);
            var map = scene.Window("mcv-map-view");
            var target = map.GlobalPosition + new Vector2(200, 330);
            var viewport = runner.Scene().GetViewport();
            viewport.PushInput(new InputEventMouseButton { Position = target, GlobalPosition = target, ButtonIndex = MouseButton.Left, Pressed = true, ButtonMask = MouseButtonMask.Left });
            viewport.PushInput(new InputEventMouseButton { Position = target, GlobalPosition = target, ButtonIndex = MouseButton.Left, Pressed = false });
            await runner.SimulateFrames(2);
            RuntimeInterop.RunIteration();
            await runner.SimulateFrames(2);
            // Polling uses SimulatedMouse; transitions still go through RootNode's normal hover tracking.
            HoverAt(map.GlobalPosition + new Vector2(-3, 330));
            await runner.SimulateFrames(5);
            Assertions.AssertThat(scene.Root.FindWindowById("mcv-map-view:hover-window")).IsNull();
            HoverAt(target); // first event on the map is enter, directly on the MCV
            await runner.SimulateFrames(10);
            var tooltip = scene.Root.FindWindowById("mcv-map-view:hover-window");
            Assertions.AssertThat(tooltip).IsNotNull();
            if (tooltip != null) Assertions.AssertThat(tooltip.GlobalPosition).IsEqual(target + new Vector2(12, 12));
            HoverAt(map.GlobalPosition + new Vector2(280, 330)); // adjacent cell
            await runner.SimulateFrames(10);
            Assertions.AssertThat(scene.Root.FindWindowById("mcv-map-view:hover-window")).IsNull();
            HoverAt(map.GlobalPosition + new Vector2(799, 599)); // edge cell, not the MCV
            await runner.SimulateFrames(10);
            Assertions.AssertThat(scene.Root.FindWindowById("mcv-map-view:hover-window")).IsNull();
        }
        finally { ClearSimulatedMouse(); }
    }

}
