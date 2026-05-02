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
                .AddFileToArchive("module/texture.exr", "texture.exr")
                .AddFileToArchive("module/texture_2.exr", "texture_2.exr")
                .EnsureDllAccessible()
                .ProcessArchive();


            var scene = LoadTestScene();
            var rootNode = new RootNode();
            var idList = RuntimeInterop.GetPanelIds();

            scene.AddChild(rootNode);
            rootNode.SetSize(new Vector2() {
                X = 1000,
                Y = 1000
            });
            await runner.SimulateFrames(1);


            AssertPanelThat(rootNode.GetNode<Panel>(idList[0]))
                .IsPositionEqual(0, 0);

            DebugSaveScreenshot("expected.png");

            var mouseEvent = new InputEventMouseButton() {
                Position = new Vector2(51, 51),
                GlobalPosition = new Vector2(51, 51),
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

            var mouseEvent2 = new InputEventMouseButton() {
                Position = new Vector2(0, 0),
                GlobalPosition = new Vector2(0, 0),
                ButtonIndex = MouseButton.Left,
                Pressed = true,
                ButtonMask = MouseButtonMask.Left
            };
            runner.Scene().GetViewport().PushInput(mouseEvent2);
            mouseEvent2 = (InputEventMouseButton)mouseEvent2.Duplicate();
            mouseEvent2.Pressed = false;
            runner.Scene().GetViewport().PushInput(mouseEvent2);


            AssertRuntimeOutputContains("___From module childAction fired line___");
            await runner.SimulateFrames(1);
        }
        catch (Exception e) {
            Assertions.AssertThat(true)
                .OverrideFailureMessage($"Error: {e.Message}\n{e.StackTrace}")
                .IsFalse();
        }
    }
}