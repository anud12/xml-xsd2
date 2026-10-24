using GdUnit4.Examples.Basics.Setup.Sources.UI;
using Godot;
using NewGameProject.Tests.XUnit;
using Vector2 = Godot.Vector2;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_3.AddChild;

[TestSuite]
public class TestClass : Steps {
    [TestCategory("Step_3")]
    [TestCase(Timeout = 10_000)]
    [RequireGodotRuntime]
    public async Task Given_window_handle_it_should_render_children_added_before_and_after_the_first_paint() {
        try {
            AddFileToArchive("module/index.js", "index.js")
                .AddFileToArchive("module/fonts/Ubuntu-Regular.ttf", "fonts/Ubuntu-Regular.ttf")
                .AddFileToArchive("module/manifest.json", "manifest.json")
                .EnsureDllAccessible()
                .ProcessArchive();

            var scene = await AttachUiScene();

            var win = scene.Window("add-child");
            AssertPanelThat(win).IsNonNull();
            AssertPanelThat(win).IsPositionEqual(100, 100);
            AssertPanelThat(win).ViewportIsSize(200, 120);

            // The child appended via addChild at apply time is in the view...
            AssertPanelThat(win)
                .HasChildPanelNamed("early", child => child.HasContentText("early"));
            // ...and the runtime-appended child is not there yet.
            Assertions.AssertThat(scene.GetWindowOrNull("late"))
                .OverrideFailureMessage("late child must not exist before the click")
                .IsNull();

            DebugSaveScreenshot("before.png");

            // Click the button: the module's onClick appends a child to the
            // window handle at runtime. The next tick's snapshot carries it
            // and the engine materializes it.
            var button = scene.Window("button");
            Click(button.GlobalPosition + new Vector2(1, 1));
            await runner.SimulateFrames(2);

            AssertPanelThat(win)
                .HasChildPanelNamed("late", child => child.HasContentText("late"));

            DebugSaveScreenshot("after.png");
            ClearSimulatedMouse();
        }
        catch (Exception e) {
            Assertions.AssertThat(true)
                .OverrideFailureMessage($"Error: {e.Message}\n{e.StackTrace}")
                .IsFalse();
        }
    }

    void Click(Vector2 globalPos) {
        var press = new InputEventMouseButton {
            Position = globalPos,
            GlobalPosition = globalPos,
            ButtonIndex = MouseButton.Left,
            Pressed = true,
            ButtonMask = MouseButtonMask.Left
        };
        runner.Scene().GetViewport().PushInput(press);
        var release = (InputEventMouseButton)press.Duplicate();
        release.Pressed = false;
        runner.Scene().GetViewport().PushInput(release);
    }
}
