using GdUnit4.Examples.Basics.Setup.Sources.UI;
using Godot;
using NewGameProject.Tests.XUnit;
using Vector2 = Godot.Vector2;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_3.Resizable.PerAxis;

[TestSuite]
public partial class TestClass : Steps {
    [TestCategory("Step_3")]
    [TestCase(Timeout = 10_000)]
    [RequireGodotRuntime]
    public async Task Given_horizontal_only_panel_with_aspect_it_should_resize_width_alone() {
        try {
            AddFileToArchive("module/index.js", "index.js")
                .AddFileToArchive("module/manifest.json", "manifest.json")
                .AddFileToArchive("module/texture.png", "texture.png")
                .EnsureDllAccessible()
                .ProcessArchive();

            var scene = await AttachUiScene();

            // 200x100, only the horizontal axis is enabled. keepAspectRatio is
            // declared but has nothing to lock to, so the height must stay put
            // while the width tracks the right-edge drag.
            var panel = scene.Window("resizable-h");
            AssertPanelThat(panel).IsNonNull();
            AssertPanelThat(panel).ViewportIsSize(200, 100);
            AssertPanelThat(panel).IsPositionEqual(100, 100);

            Drag(panel, new Vector2(198, 50), new Vector2(100, 0));
            await runner.SimulateFrames(2);

            AssertPanelThat(panel).ViewportIsSize(300, 100);
            AssertPanelThat(panel).IsPositionEqual(100, 100);
            ClearSimulatedMouse();
        }
        catch (Exception e) {
            Assertions.AssertThat(true)
                .OverrideFailureMessage($"Error: {e.Message}\n{e.StackTrace}")
                .IsFalse();
        }
    }

    [TestCategory("Step_3")]
    [TestCase(Timeout = 10_000)]
    [RequireGodotRuntime]
    public async Task Given_vertical_only_panel_with_aspect_it_should_resize_height_alone() {
        try {
            AddFileToArchive("module/index.js", "index.js")
                .AddFileToArchive("module/manifest.json", "manifest.json")
                .AddFileToArchive("module/texture.png", "texture.png")
                .EnsureDllAccessible()
                .ProcessArchive();

            var scene = await AttachUiScene();

            // 200x100, only the vertical axis is enabled. keepAspectRatio is
            // declared but inert, so the width must stay put while the height
            // tracks the bottom-edge drag.
            var panel = scene.Window("resizable-v");
            AssertPanelThat(panel).IsNonNull();
            AssertPanelThat(panel).ViewportIsSize(200, 100);
            AssertPanelThat(panel).IsPositionEqual(400, 100);

            Drag(panel, new Vector2(100, 98), new Vector2(0, 50));
            await runner.SimulateFrames(2);

            AssertPanelThat(panel).ViewportIsSize(200, 150);
            AssertPanelThat(panel).IsPositionEqual(400, 100);
            ClearSimulatedMouse();
        }
        catch (Exception e) {
            Assertions.AssertThat(true)
                .OverrideFailureMessage($"Error: {e.Message}\n{e.StackTrace}")
                .IsFalse();
        }
    }

    [TestCategory("Step_3")]
    [TestCase(Timeout = 10_000)]
    [RequireGodotRuntime]
    public async Task Given_both_axes_panel_with_aspect_it_should_lock_the_ratio() {
        try {
            AddFileToArchive("module/index.js", "index.js")
                .AddFileToArchive("module/manifest.json", "manifest.json")
                .AddFileToArchive("module/texture.png", "texture.png")
                .EnsureDllAccessible()
                .ProcessArchive();

            var scene = await AttachUiScene();

            // 200x100 (a 2:1 ratio), both axes enabled. Dragging only the right
            // edge by +100 grows the width to 300; keepAspectRatio forces the
            // height to grow to 150 so the 2:1 ratio is preserved.
            var panel = scene.Window("resizable-both");
            AssertPanelThat(panel).IsNonNull();
            AssertPanelThat(panel).ViewportIsSize(200, 100);
            AssertPanelThat(panel).IsPositionEqual(700, 100);

            Drag(panel, new Vector2(198, 50), new Vector2(100, 0));
            await runner.SimulateFrames(2);

            AssertPanelThat(panel).ViewportIsSize(300, 150);
            AssertPanelThat(panel).IsPositionEqual(700, 100);
            ClearSimulatedMouse();
        }
        catch (Exception e) {
            Assertions.AssertThat(true)
                .OverrideFailureMessage($"Error: {e.Message}\n{e.StackTrace}")
                .IsFalse();
        }
    }

    [TestCategory("Step_3")]
    [TestCase(Timeout = 10_000)]
    [RequireGodotRuntime]
    public async Task Given_horizontal_only_free_panel_it_should_resize_width_alone() {
        try {
            AddFileToArchive("module/index.js", "index.js")
                .AddFileToArchive("module/manifest.json", "manifest.json")
                .AddFileToArchive("module/texture.png", "texture.png")
                .EnsureDllAccessible()
                .ProcessArchive();

            var scene = await AttachUiScene();

            // 200x100, only the horizontal axis is enabled, no keepAspectRatio.
            // The width tracks the right-edge drag; the height stays fixed.
            var panel = scene.Window("resizable-h-free");
            AssertPanelThat(panel).IsNonNull();
            AssertPanelThat(panel).ViewportIsSize(200, 100);
            AssertPanelThat(panel).IsPositionEqual(100, 300);

            Drag(panel, new Vector2(198, 50), new Vector2(100, 0));
            await runner.SimulateFrames(2);

            AssertPanelThat(panel).ViewportIsSize(300, 100);
            AssertPanelThat(panel).IsPositionEqual(100, 300);
            ClearSimulatedMouse();
        }
        catch (Exception e) {
            Assertions.AssertThat(true)
                .OverrideFailureMessage($"Error: {e.Message}\n{e.StackTrace}")
                .IsFalse();
        }
    }

    [TestCategory("Step_3")]
    [TestCase(Timeout = 10_000)]
    [RequireGodotRuntime]
    public async Task Given_vertical_only_free_panel_it_should_resize_height_alone() {
        try {
            AddFileToArchive("module/index.js", "index.js")
                .AddFileToArchive("module/manifest.json", "manifest.json")
                .AddFileToArchive("module/texture.png", "texture.png")
                .EnsureDllAccessible()
                .ProcessArchive();

            var scene = await AttachUiScene();

            // 200x100, only the vertical axis is enabled, no keepAspectRatio.
            // The height tracks the bottom-edge drag; the width stays fixed.
            var panel = scene.Window("resizable-v-free");
            AssertPanelThat(panel).IsNonNull();
            AssertPanelThat(panel).ViewportIsSize(200, 100);
            AssertPanelThat(panel).IsPositionEqual(400, 300);

            Drag(panel, new Vector2(100, 98), new Vector2(0, 50));
            await runner.SimulateFrames(2);

            AssertPanelThat(panel).ViewportIsSize(200, 150);
            AssertPanelThat(panel).IsPositionEqual(400, 300);
            ClearSimulatedMouse();
        }
        catch (Exception e) {
            Assertions.AssertThat(true)
                .OverrideFailureMessage($"Error: {e.Message}\n{e.StackTrace}")
                .IsFalse();
        }
    }

    [TestCategory("Step_3")]
    [TestCase(Timeout = 10_000)]
    [RequireGodotRuntime]
    public async Task Given_both_axes_free_panel_it_should_resize_without_a_ratio() {
        try {
            AddFileToArchive("module/index.js", "index.js")
                .AddFileToArchive("module/manifest.json", "manifest.json")
                .AddFileToArchive("module/texture.png", "texture.png")
                .EnsureDllAccessible()
                .ProcessArchive();

            var scene = await AttachUiScene();

            // 200x100, both axes enabled, no keepAspectRatio. Dragging only the
            // right edge by +100 grows the width to 300 and leaves the height
            // at 100 (the 2:1 ratio is NOT preserved, unlike the aspect set).
            var panel = scene.Window("resizable-both-free");
            AssertPanelThat(panel).IsNonNull();
            AssertPanelThat(panel).ViewportIsSize(200, 100);
            AssertPanelThat(panel).IsPositionEqual(700, 300);

            Drag(panel, new Vector2(198, 50), new Vector2(100, 0));
            await runner.SimulateFrames(2);

            AssertPanelThat(panel).ViewportIsSize(300, 100);
            AssertPanelThat(panel).IsPositionEqual(700, 300);
            ClearSimulatedMouse();
        }
        catch (Exception e) {
            Assertions.AssertThat(true)
                .OverrideFailureMessage($"Error: {e.Message}\n{e.StackTrace}")
                .IsFalse();
        }
    }

    /// Presses the window's local point, drags by the given offset, releases —
    /// pushing synthetic mouse events so the window's GuiInput resize handler
    /// (and the background TextureRect, which ignores mouse) receives them.
    void Drag(UiWindow window, Vector2 local, Vector2 offset)
    {
        var viewport = runner.Scene().GetViewport();
        var gPos = window.GlobalPosition + local;
        var press = new InputEventMouseButton {
            Position = gPos,
            GlobalPosition = gPos,
            ButtonIndex = MouseButton.Left,
            Pressed = true,
            ButtonMask = MouseButtonMask.Left
        };
        viewport.PushInput(press);
        var gMotion = window.GlobalPosition + local + offset;
        var motion = new InputEventMouseMotion {
            Position = gMotion,
            GlobalPosition = gMotion,
            Relative = offset
        };
        viewport.PushInput(motion);
        var gRelease = window.GlobalPosition + local + offset;
        var release = new InputEventMouseButton {
            Position = gRelease,
            GlobalPosition = gRelease,
            ButtonIndex = MouseButton.Left,
            Pressed = false,
            ButtonMask = 0
        };
        viewport.PushInput(release);
    }
}
