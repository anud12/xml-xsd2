using GdUnit4.Examples.Basics.Setup.Sources.UI;
using Godot;
using NewGameProject.Tests.XUnit;
using Vector2 = Godot.Vector2;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_3.TitleDrag;

[TestSuite]
public partial class TestClass : Steps {
    [TestCategory("Step_3")]
    [TestCase(Timeout = 10_000)]
    [RequireGodotRuntime]
    public async Task Given_titled_window_it_should_render_the_title_strip_and_inset_the_content() {
        try {
            AddFileToArchive("module/index.js", "index.js")
                .AddFileToArchive("module/manifest.json", "manifest.json")
                .EnsureDllAccessible()
                .ProcessArchive();

            var scene = await AttachUiScene();

            var panel = scene.Window("titled");
            AssertPanelThat(panel).IsNonNull();
            AssertPanelThat(panel).IsPositionEqual(100, 100);
            AssertPanelThat(panel).ViewportIsSize(200, 120);

            var titleBar = panel.GetNodeOrNull<Godot.ColorRect>("__titleBar");
            Assertions.AssertThat(titleBar != null)
                .OverrideFailureMessage("titled window must have a title bar strip")
                .IsTrue();
            Assertions.AssertThat((float)titleBar.Size.Y)
                .OverrideFailureMessage("title bar strip height should be 20")
                .IsEqual(20f);
            var label = panel.GetNode<Godot.Label>("__titleBar/__titleLabel");
            Assertions.AssertThat(label.Text)
                .OverrideFailureMessage("title label should show the title text")
                .IsEqual("My Window");
            var box = panel.GetNode<Godot.BoxContainer>("box");
            Assertions.AssertThat((float)box.OffsetTop)
                .OverrideFailureMessage("flow content must be inset below the title strip")
                .IsEqual(20f);

            DebugSaveScreenshot("result.png");
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
    public async Task Given_titled_window_it_should_move_when_dragging_the_title_bar() {
        try {
            AddFileToArchive("module/index.js", "index.js")
                .AddFileToArchive("module/manifest.json", "manifest.json")
                .EnsureDllAccessible()
                .ProcessArchive();

            var scene = await AttachUiScene();

            var panel = scene.Window("titled");
            AssertPanelThat(panel).IsPositionEqual(100, 100);
            AssertPanelThat(panel).ViewportIsSize(200, 120);

            // Press the title bar (y=10, inside the 20px strip) and drag by
            // (40, 30): the window moves, its size is unchanged.
            Drag(panel, new Vector2(10, 10), new Vector2(40, 30));
            await runner.SimulateFrames(2);

            AssertPanelThat(panel).IsPositionEqual(140, 130);
            AssertPanelThat(panel).ViewportIsSize(200, 120);

            DebugSaveScreenshot("dragged.png");
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
    public async Task Given_titled_resizable_window_it_should_keep_the_top_edge_resize_over_the_strip() {
        try {
            AddFileToArchive("module/index.js", "index.js")
                .AddFileToArchive("module/manifest.json", "manifest.json")
                .EnsureDllAccessible()
                .ProcessArchive();

            var scene = await AttachUiScene();

            var panel = scene.Window("titled-resizable");
            AssertPanelThat(panel).IsPositionEqual(400, 100);
            AssertPanelThat(panel).ViewportIsSize(200, 120);

            // Press the top edge (y=2: inside both the strip and the 6px
            // resize zone) and drag down by 30: the resize, not the drag,
            // wins — the window shrinks and its top edge follows the mouse
            // (the bottom edge stays put).
            Drag(panel, new Vector2(100, 2), new Vector2(0, 30));
            await runner.SimulateFrames(2);

            AssertPanelThat(panel).ViewportIsSize(200, 90);
            AssertPanelThat(panel).IsPositionEqual(400, 130);

            DebugSaveScreenshot("resized.png");
            ClearSimulatedMouse();
        }
        catch (Exception e) {
            Assertions.AssertThat(true)
                .OverrideFailureMessage($"Error: {e.Message}\n{e.StackTrace}")
                .IsFalse();
        }
    }

    /// Presses the window's local point, drags by the given offset, releases —
    /// pushing synthetic mouse events so the title bar's GuiInput handler
    /// receives them.
    void Drag(UiWindow window, Vector2 local, Vector2 offset) {
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
