using GdUnit4.Examples.Basics.Setup.Sources.UI;
using Godot;
using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;
using Vector2 = Godot.Vector2;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_6.EmitChildEvents;

[TestSuite]
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

        var numberAssertions = AssertPanelThat(rootNode.GetNode<Panel>(idList[0]))
            .HasContentText("0");
        var textAssertions = AssertPanelThat(rootNode.GetNode<Panel>(idList[0]))
            .HasContentText("No");


        //Run iteration with not enough time to trigger a new reoccurance
        RuntimeInterop.RunIteration(9);
        await runner.SimulateFrames(1);
        Assertions.AssertThat(RuntimeInterop.GetElapsedTimeUnits()).IsEqual(9L);
        //Value is 3 = (previous 0) + 1 (+ 2 from key-modify-if-par)
        numberAssertions.HasContentText("3");
        textAssertions.HasContentText("Yes");

        //Run iteration with enough time to trigger a new reoccurance
        RuntimeInterop.RunIteration(1);
        await runner.SimulateFrames(1);
        Assertions.AssertThat(RuntimeInterop.GetElapsedTimeUnits()).IsEqual(10L);
        //Value is 4 = (previous 3) + 1
        numberAssertions.HasContentText("4");
        textAssertions.HasContentText("No");


        //Run iteration with not enough time to trigger a new reoccurance
        RuntimeInterop.RunIteration(9);
        await runner.SimulateFrames(1);
        Assertions.AssertThat(RuntimeInterop.GetElapsedTimeUnits()).IsEqual(19L);
        numberAssertions.HasContentText("4");
        textAssertions.HasContentText("No");

        //Run iteration with not enough time to trigger a new reoccurance
        RuntimeInterop.RunIteration(1);
        await runner.SimulateFrames(1);
        Assertions.AssertThat(RuntimeInterop.GetElapsedTimeUnits()).IsEqual(20L);
        //Value is 7 = (previous 4) + 1 (+ 2 from key-modify-if-par)
        numberAssertions.HasContentText("7");
        textAssertions.HasContentText("Yes");


        //Run iteration with exact time to trigger a new reoccurance
        RuntimeInterop.RunIteration(10);
        await runner.SimulateFrames(1);
        Assertions.AssertThat(RuntimeInterop.GetElapsedTimeUnits()).IsEqual(30L);
        //Value is 8 = (previous 7) + 1
        numberAssertions.HasContentText("8");
        textAssertions.HasContentText("No");


        //Run iteration with exact time to trigger a new reoccurance twice
        RuntimeInterop.RunIteration(20);
        await runner.SimulateFrames(1);
        Assertions.AssertThat(RuntimeInterop.GetElapsedTimeUnits()).IsEqual(50L);

        //Value is 11 = (previous 8) + 1 (+ 2 from key-modify-if-par)
        //Value is 12 = (previous 11) + 1
        numberAssertions.HasContentText("12");
        textAssertions.HasContentText("No");
    }
}