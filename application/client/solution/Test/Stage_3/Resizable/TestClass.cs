using GdUnit4.Examples.Basics.Setup.Sources.UI;
using Godot;
using NewGameProject.Tests.XUnit;
using Vector2 = Godot.Vector2;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_3.Resizable;

[TestSuite]
public partial class TestClass : Steps {
    [TestCategory("Step_3")]
    [TestCase(Timeout = 10_000)]
    [RequireGodotRuntime]
    public async Task Given_resizable_panel_it_should_grow_when_dragging_the_bottom_right_corner() {
        try {
            AddFileToArchive("module/index.js", "index.js")
                .AddFileToArchive("module/manifest.json", "manifest.json")
                .AddFileToArchive("module/texture.png", "texture.png")
                .EnsureDllAccessible()
                .ProcessArchive();

            var scene = await AttachUiScene();

            var panel = scene.Window("resizable");
            AssertPanelThat(panel).IsNonNull();
            AssertPanelThat(panel).ViewportIsSize(200, 200);
            AssertPanelThat(panel).IsPositionEqual(100, 100);

            // Press the bottom-right corner, drag outward by +50 on both axes,
            // release. The panel grows by 50x50; position is unchanged.
            var corner = new Vector2(198, 198);
            Drag(panel, corner, new Vector2(50, 50));
            await runner.SimulateFrames(2);

            AssertPanelThat(panel).ViewportIsSize(250, 250);
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
    public async Task Given_resizable_panel_it_should_shrink_and_move_when_dragging_the_top_left_corner() {
        try {
            AddFileToArchive("module/index.js", "index.js")
                .AddFileToArchive("module/manifest.json", "manifest.json")
                .AddFileToArchive("module/texture.png", "texture.png")
                .EnsureDllAccessible()
                .ProcessArchive();

            var scene = await AttachUiScene();

            var panel = scene.Window("resizable");
            AssertPanelThat(panel).IsNonNull();
            AssertPanelThat(panel).ViewportIsSize(200, 200);
            AssertPanelThat(panel).IsPositionEqual(100, 100);

            // Press the top-left corner and drag inward by +50 on both axes:
            // the panel shrinks to 150x150 and its top-left moves to (150,150).
            var corner = new Vector2(2, 2);
            Drag(panel, corner, new Vector2(50, 50));
            await runner.SimulateFrames(2);

            AssertPanelThat(panel).ViewportIsSize(150, 150);
            AssertPanelThat(panel).IsPositionEqual(150, 150);
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
    public async Task Given_resizable_panel_it_should_show_the_resize_cursor_over_an_edge() {
        try {
            AddFileToArchive("module/index.js", "index.js")
                .AddFileToArchive("module/manifest.json", "manifest.json")
                .AddFileToArchive("module/texture.png", "texture.png")
                .EnsureDllAccessible()
                .ProcessArchive();

            var scene = await AttachUiScene();
            var panel = scene.Window("resizable");
            AssertPanelThat(panel).IsNonNull();

            // Hover the bottom-right corner: the cursor shows the diagonal
            // (NW-SE) resize shape for a bottom-right corner.
            Hover(panel, new Vector2(198, 198));
            await runner.SimulateFrames(2);
            Assertions.AssertThat(panel.MouseDefaultCursorShape)
                .OverrideFailureMessage("cursor should be Fdiagsize over the bottom-right corner")
                .IsEqual(Godot.Control.CursorShape.Fdiagsize);

            // Move to the center: the cursor reverts to the default arrow.
            Hover(panel, new Vector2(100, 100));
            await runner.SimulateFrames(2);
            Assertions.AssertThat(panel.MouseDefaultCursorShape)
                .OverrideFailureMessage("cursor should be Arrow at the panel center")
                .IsEqual(Godot.Control.CursorShape.Arrow);

            // Hover the right edge (not a corner): the cursor shows the
            // horizontal resize shape.
            Hover(panel, new Vector2(50, 50));
            Hover(panel, new Vector2(198, 100));
            await runner.SimulateFrames(2);
            Assertions.AssertThat(panel.MouseDefaultCursorShape)
                .OverrideFailureMessage("cursor should be Hsize over the right edge")
                .IsEqual(Godot.Control.CursorShape.Hsize);

            ClearSimulatedMouse();
        }
        catch (Exception e) {
            Assertions.AssertThat(true)
                .OverrideFailureMessage($"Error: {e.Message}\n{e.StackTrace}")
                .IsFalse();
        }
    }

    Vector2 _hoverLastGlobal = Vector2.Zero;

    /// Moves the (already-positioned) mouse to the window's local point,
    /// pushing a synthetic motion event (with a non-zero relative delta so the
    /// engine does not drop it) so the window's resize handler can update the
    /// hover cursor shape.
    void Hover(UiWindow window, Vector2 local)
    {
        var viewport = runner.Scene().GetViewport();
        var gPos = window.GlobalPosition + local;
        var relative = _hoverLastGlobal == Vector2.Zero
            ? Vector2.One
            : gPos - _hoverLastGlobal;
        if (relative == Vector2.Zero) relative = Vector2.One;
        _hoverLastGlobal = gPos;
        var motion = new InputEventMouseMotion {
            Position = gPos,
            GlobalPosition = gPos,
            Relative = relative
        };
        viewport.PushInput(motion);
    }

    /// Presses the window's local point, drags by the given offset, releases —
    /// pushing synthetic mouse events so the window's GuiInput resize handler
    /// (and the background TextureRect, which ignores mouse) receives them.
    void Drag(UiWindow window, Vector2 local, Vector2 offset)
    {
        var viewport = runner.Scene().GetViewport();
        // The viewport treats the event Position as the viewport-global point
        // (GlobalPosition is overwritten on PushInput), so both fields are the
        // window-local point offset by the window's viewport position.
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
