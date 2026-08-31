using GdUnit4.Examples.Basics.Setup.Sources.UI;
using Godot;
using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;
using Vector2 = Godot.Vector2;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_2.EmitActionOnClick;

[TestSuite]
public partial class TestClass : Steps {
    [TestCategory("Step_3")]
    [TestCase]
    [RequireGodotRuntime]
    public async Task Given_panel_it_should_emit_action_on_click() {
        try {
            // I create a module from the first folder
            AddFileToArchive("module/index.js", "index.js")
                .AddFileToArchive("module/manifest.json", "manifest.json")
                .AddFileToArchive("module/texture.png", "texture.png")
                .AddFileToArchive("module/texture_2.png", "texture_2.png")
                .EnsureDllAccessible()
                .ProcessArchive();


            var scene = await AttachUiScene();

            var center = scene.Window("center");

            var child = scene.Window("child");
            var childPos = child.GlobalPosition;

            var mouseEvent = new InputEventMouseButton() {
                Position = center.GlobalPosition + new Vector2(1, 1),
                GlobalPosition = center.GlobalPosition + new Vector2(1, 1),
                ButtonIndex = MouseButton.Left,
                Pressed = true,
                ButtonMask = MouseButtonMask.Left
            };
            runner.Scene().GetViewport().PushInput(mouseEvent);
            mouseEvent = (InputEventMouseButton)mouseEvent.Duplicate();
            mouseEvent.Pressed = false;
            runner.Scene().GetViewport().PushInput(mouseEvent);
            await runner.SimulateFrames(1);

            AssertRuntimeOutputContains("___From module action fired line___");

            var childClick = new InputEventMouseButton() {
                Position = childPos + new Vector2(1, 1),
                GlobalPosition = childPos + new Vector2(1, 1),
                ButtonIndex = MouseButton.Left,
                Pressed = true,
                ButtonMask = MouseButtonMask.Left
            };
            runner.Scene().GetViewport().PushInput(childClick);
            childClick = (InputEventMouseButton)childClick.Duplicate();
            childClick.Pressed = false;
            runner.Scene().GetViewport().PushInput(childClick);
            await runner.SimulateFrames(1);

            AssertRuntimeOutputContains("___From module childAction fired line___");
        }
        catch (Exception e) {
            Assertions.AssertThat(true)
                .OverrideFailureMessage($"Error: {e.Message}\n{e.StackTrace}")
                .IsFalse();
        }
    }
}