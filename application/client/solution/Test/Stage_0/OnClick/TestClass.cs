using GdUnit4.Examples.Basics.Setup.Sources.UI;
using Godot;
using NewGameProject.Tests.XUnit;
using Vector2 = Godot.Vector2;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_0.OnClick;

[TestSuite]
public class TestClass : Steps {
    [TestCategory("Step_0")]
    [TestCase]
    [RequireGodotRuntime]
    public async Task AssertRuntimeOutputContains_it_should_see_action_logs_emitted_by_routed_clicks() {
        try {
            AddFileToArchive("module/index.js", "index.js")
                .AddFileToArchive("module/manifest.json", "manifest.json")
                .EnsureDllAccessible()
                .ProcessArchive();

            var scene = await AttachUiScene();

            var parent = scene.Window("parent");
            Assertions.AssertBool(parent.MouseFilter == Control.MouseFilterEnum.Stop)
                .OverrideFailureMessage("Window with onClick must have MouseFilter Stop")
                .IsTrue();

            Click(parent.GlobalPosition + new Vector2(1, 1));
            await runner.SimulateFrames(1);
            AssertRuntimeOutputContains("___From module stageAction fired line___");
            AssertRuntimeOutputContainsNot("___From module childAction fired line___");

            var child = scene.Window("child");
            Click(child.GlobalPosition + new Vector2(1, 1));
            await runner.SimulateFrames(1);
            AssertRuntimeOutputContains("___From module childAction fired line___");

            DebugSaveScreenshot("result.png");
        }
        catch (Exception e) {
            Assertions.AssertThat(true)
                .OverrideFailureMessage($"Error: {e.Message}\n{e.StackTrace}")
                .IsFalse();
        }
    }

    [TestCategory("Step_0")]
    [TestCase]
    [RequireGodotRuntime]
    public async Task AssertRuntimeOutputContains_it_should_fail_for_an_action_that_never_fired() {
        try {
            AddFileToArchive("module/index.js", "index.js")
                .AddFileToArchive("module/manifest.json", "manifest.json")
                .EnsureDllAccessible()
                .ProcessArchive();

            var scene = await AttachUiScene();

            // No clicks have been issued, so no action log lines exist yet.
            bool threw = false;
            try {
                AssertRuntimeOutputContains("___From module stageAction fired line___");
            }
            catch (Exception) {
                threw = true;
            }
            Assertions.AssertThat(threw)
                .OverrideFailureMessage(
                    "AssertRuntimeOutputContains should have failed for an action that never fired")
                .IsTrue();

            // Conversely, asserting a line that does not exist must pass.
            AssertRuntimeOutputContainsNot("___From module stageAction fired line___");
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
