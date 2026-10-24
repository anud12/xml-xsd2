using GdUnit4.Examples.Basics.Setup.Sources.UI;
using Godot;
using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;
using Vector2 = Godot.Vector2;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_2.ActionMutatesEntity;

[TestSuite]
public partial class ActionMutatesEntityTests : Steps {
    [TestCase]
    [RequireGodotRuntime]
    public async Task Given_reoccurring_decrement_when_clicking_button_it_should_reset_counter() {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .EnsureDllAccessible()
            .ProcessArchive();

        var scene = await AttachUiScene();

        // Run 10 ticks: counter decrements from 100.
        for (int i = 0; i < 10; i++) {
            RuntimeInterop.RunIteration(1);
        }
        var beforeClick = int.Parse(RuntimeInterop.ReadEntityNumberValue("obj", "counter"));
        Assertions.AssertThat(beforeClick).IsLess(100);

        // Click the button.
        var btn = scene.Window("btn")!;
        var pos = btn.GlobalPosition + new Vector2(1, 1);
        var mouseEvent = new InputEventMouseButton() {
            Position = pos,
            GlobalPosition = pos,
            ButtonIndex = MouseButton.Left,
            Pressed = true,
            ButtonMask = MouseButtonMask.Left
        };
        runner.Scene().GetViewport().PushInput(mouseEvent);
        await runner.SimulateFrames(1);

        mouseEvent = (InputEventMouseButton)mouseEvent.Duplicate();
        mouseEvent.Pressed = false;
        runner.Scene().GetViewport().PushInput(mouseEvent);

        // Run iteration to process the queued action -> effect.
        RuntimeInterop.RunIteration(1);

        // Counter should be higher than before the click (reset to 100, then ticked to 99).
        var afterClick = int.Parse(RuntimeInterop.ReadEntityNumberValue("obj", "counter"));
        Assertions.AssertThat(afterClick).IsGreater(beforeClick);
    }

    [TestCase]
    [RequireGodotRuntime]
    public async Task Given_single_click_it_should_increment_entity_score() {
        CleanupArchive();
        AddFileToArchive("module_simple/index.js", "index.js")
            .AddFileToArchive("module_simple/manifest.json", "manifest.json")
            .EnsureDllAccessible()
            .ProcessArchive();

        var scene = await AttachUiScene();
        var btn = scene.Window("btn")!;

        // Click the panel.
        var pos = btn.GlobalPosition + new Vector2(1, 1);
        var mouseEvent = new InputEventMouseButton() {
            Position = pos,
            GlobalPosition = pos,
            ButtonIndex = MouseButton.Left,
            Pressed = true,
            ButtonMask = MouseButtonMask.Left
        };
        runner.Scene().GetViewport().PushInput(mouseEvent);
        await runner.SimulateFrames(1);

        mouseEvent = (InputEventMouseButton)mouseEvent.Duplicate();
        mouseEvent.Pressed = false;
        runner.Scene().GetViewport().PushInput(mouseEvent);

        // Run the iteration to process the queued action → effect.
        RuntimeInterop.RunIteration();

        // The click triggered the action, which emitted the event,
        // which fired the effect that incremented the score.
        Assertions.AssertThat(RuntimeInterop.ReadEntityNumberValue("obj", "score"))
            .IsEqual("1");
    }
}
