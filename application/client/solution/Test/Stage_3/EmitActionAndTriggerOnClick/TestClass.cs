using GdUnit4.Examples.Basics.Setup.Sources.UI;
using Godot;
using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;
using Vector2 = Godot.Vector2;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_3.EmitActionAndTriggerOnClick;

[TestSuite]
public partial class TestClass : Steps {
    [TestCategory("Step_3")]
    [TestCase]
    [RequireGodotRuntime]
    public async Task Given_panel_it_should_emit_action_on_click_and_trigger_effect() {
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .AddFileToArchive("module/texture.png", "texture.png")
            .EnsureDllAccessible()
            .ProcessArchive();

        var scene = await AttachUiScene();

        var center = scene.Window("center");
        Assertions.AssertThat(center).IsNotNull();

        var pos = center.GlobalPosition + new Vector2(1, 1);
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

        AssertRuntimeOutputContains("___From module action fired line___");
        AssertRuntimeOutputContainsNot("___From module effect prepare fired line___");
        AssertRuntimeOutputContainsNot("___From module effect fired line___");


        RuntimeInterop.RunIteration();

        AssertRuntimeOutputContains("___From module effect prepare fired line___");
        AssertRuntimeOutputContains("___From module effect fired line___");

        ClearOutput();

        RuntimeInterop.RunIteration();


        AssertRuntimeOutputContainsNot("___From module effect prepare fired line___");
        AssertRuntimeOutputContainsNot("___From module effect fired line___");

    }
}
