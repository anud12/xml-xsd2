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
        // I create a module from the first folder
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .AddFileToArchive("module/texture.exr", "texture.exr")
            .EnsureDllAccessible()
            .ProcessArchive();


        var scene = LoadTestScene();
        var rootNode = new Root();
        var idList = RuntimeInterop.GetPanelIds();
        foreach (var id in idList) {
            rootNode.AddChild(new Panel(RuntimeInterop.GetPanelById(id)) {
                Name = id
            });
        }

        scene.AddChild(rootNode);
        rootNode.SetSize(new Vector2() {
            X = 1000,
            Y = 1000
        });
        rootNode.SetAnchorsPreset(Control.LayoutPreset.Center);
        await runner.SimulateFrames(1);


        AssertPanelThat(rootNode.GetNode<Panel>(idList[0]))
            .IsPositionEqual(0, 0);

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
        AssertRuntimeOutputContainsNot("___From module effect prepare fired line___");
        AssertRuntimeOutputContainsNot("___From module effect fired line___");
        
        
        RuntimeInterop.SimulateIterations(1);
        
        AssertRuntimeOutputContains("___From module effect prepare fired line___");
        AssertRuntimeOutputContains("___From module effect fired line___");

        ClearOutput();
        
        RuntimeInterop.SimulateIterations(1);
        
        
        AssertRuntimeOutputContainsNot("___From module effect prepare fired line___");
        AssertRuntimeOutputContainsNot("___From module effect fired line___");
    }
}