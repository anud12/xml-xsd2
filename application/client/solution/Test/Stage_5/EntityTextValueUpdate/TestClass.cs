using GdUnit4.Examples.Basics.Setup.Sources.UI;
using Godot;
using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;
using Vector2 = Godot.Vector2;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_5.EntityTextValueUpdate;

// [TestSuite]
public partial class TestClass : Steps {
    [TestCategory("Step_5")]
    [TestCase]
    [RequireGodotRuntime]
    public async Task Given_panel_it_should_emit_action_on_clic() {
        // I create a module from the first folder
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .AddFileToArchive("module/texture.exr", "texture.exr")
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
        rootNode.SetAnchorsPreset(Control.LayoutPreset.Center);
        await runner.SimulateFrames(1);

        var assertions = AssertPanelThat(rootNode.GetNode<Panel>(idList[0]))
            .HasContentText("0");

        
        //Run iteration with 1 unit elapsed time
        RuntimeInterop.RunIteration(1);
        await runner.SimulateFrames(1);
        Assertions.AssertThat(RuntimeInterop.GetElapsedTimeUnits()).IsEqual(1L);
        assertions.HasContentText("1");
        
        
        //Run iteration with 1 unit elapsed time
        RuntimeInterop.RunIteration(1);
        await runner.SimulateFrames(1);
        Assertions.AssertThat(RuntimeInterop.GetElapsedTimeUnits()).IsEqual(2L);
        assertions.HasContentText("2");
        
        
        //Run iteration with 2 units elapsed time
        RuntimeInterop.RunIteration(2);
        await runner.SimulateFrames(1);
        Assertions.AssertThat(RuntimeInterop.GetElapsedTimeUnits()).IsEqual(4L);
        
        assertions.HasContentText("4");
    }
}