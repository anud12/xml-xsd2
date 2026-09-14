using GdUnit4.Examples.Basics.Setup.Sources.UI;
using Godot;
using NewGameProject.Tests.XUnit;
using Vector2 = Godot.Vector2;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_3.ResizableAspect;

[TestSuite]
public partial class TestClass : Steps {
    [TestCategory("Step_3")]
    [TestCase(Timeout = 10_000)]
    [RequireGodotRuntime]
    public async Task Given_keep_aspect_resizable_panel_it_should_preserve_ratio_when_growing_bottom_right() {
        try {
            AddFileToArchive("module/index.js", "index.js")
                .AddFileToArchive("module/manifest.json", "manifest.json")
                .AddFileToArchive("module/texture.png", "texture.png")
                .EnsureDllAccessible()
                .ProcessArchive();

            var scene = await AttachUiScene();

            var panel = scene.Window("aspect");
            AssertPanelThat(panel).IsNonNull();
            // Declared 200x100 (2:1 ratio).
            AssertPanelThat(panel).ViewportIsSize(200, 100);
            AssertPanelThat(panel).IsPositionEqual(100, 100);

            // Press the bottom-right corner and drag outward. The exact scale
            // factor depends on where the press lands within the corner hit
            // zone, but keepAspectRatio guarantees the result stays 2:1. Free
            // resize would break the ratio (e.g. width grows faster than
            // height); with the lock, height == width / 2 always holds.
            Drag(panel, new Vector2(198, 98), new Vector2(100, 100));
            await runner.SimulateFrames(2);

            var size = panel.Size;
            // The panel grew (width increased well beyond the declared 200).
            Assertions.AssertThat(size.X).IsGreater(250f);
            // Aspect ratio is preserved: height == width / 2 (within 1 px).
            Assertions.AssertThat(Mathf.Abs(size.Y - size.X / 2f))
                .OverrideFailureMessage($"aspect not preserved: {size.X}x{size.Y}")
                .IsLessEqual(1f);
            // Bottom-right growth keeps the top-left corner fixed.
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
    public async Task Given_keep_aspect_resizable_panel_it_should_preserve_ratio_when_shrinking_top_left() {
        try {
            AddFileToArchive("module/index.js", "index.js")
                .AddFileToArchive("module/manifest.json", "manifest.json")
                .AddFileToArchive("module/texture.png", "texture.png")
                .EnsureDllAccessible()
                .ProcessArchive();

            var scene = await AttachUiScene();

            var panel = scene.Window("aspect");
            AssertPanelThat(panel).IsNonNull();
            AssertPanelThat(panel).ViewportIsSize(200, 100);
            AssertPanelThat(panel).IsPositionEqual(100, 100);

            // Press the top-left corner and drag inward. The exact scale
            // factor depends on where the press lands within the corner hit
            // zone, but keepAspectRatio keeps the result 2:1 and the bottom-
            // right corner fixed (the top-left shifts by the shrunken amount).
            Drag(panel, new Vector2(2, 2), new Vector2(50, 50));
            await runner.SimulateFrames(2);

            var size = panel.Size;
            // The panel shrank (width fell well below the declared 200).
            Assertions.AssertThat(size.X).IsLess(180f);
            // Aspect ratio is preserved: height == width / 2 (within 1 px).
            Assertions.AssertThat(Mathf.Abs(size.Y - size.X / 2f))
                .OverrideFailureMessage($"aspect not preserved: {size.X}x{size.Y}")
                .IsLessEqual(1f);
            ClearSimulatedMouse();
        }
        catch (Exception e) {
            Assertions.AssertThat(true)
                .OverrideFailureMessage($"Error: {e.Message}\n{e.StackTrace}")
                .IsFalse();
        }
    }

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
