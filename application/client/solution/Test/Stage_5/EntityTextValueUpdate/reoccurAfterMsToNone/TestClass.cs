using GdUnit4.Examples.Basics.Setup.Sources.UI;
using Godot;
using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;
using Vector2 = Godot.Vector2;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_5.EntityTextValueUpdate.reoccurAfterMsToNone;

[TestSuite]
public partial class TestClass : Steps {
    [TestCategory("Step_5")]
    [TestCase]
    [RequireGodotRuntime]
    public async Task Given_panel_it_should_emit_action_on_clic() {
        // I create a module from the first folder
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .AddFileToArchive("module/texture.png", "texture.png")
            .EnsureDllAccessible()
            .ProcessArchive();


        var scene = await AttachUiScene();

        var assertions = scene.AssertPanelThat("center")
            .HasContentText("0");


        //Run iteration with not enough time to trigger a new reoccurance
        RuntimeInterop.RunIteration(9);
        await runner.SimulateFrames(1);
        Assertions.AssertThat(RuntimeInterop.GetElapsedTimeUnits()).IsEqual(9L);
        assertions.HasContentText("1");

        //Run iteration with enough time to trigger a new reoccurance
        RuntimeInterop.RunIteration(1);
        await runner.SimulateFrames(1);
        Assertions.AssertThat(RuntimeInterop.GetElapsedTimeUnits()).IsEqual(10L);
        assertions.HasContentText("1");


        //Run iteration with not enough time to trigger a new reoccurance
        RuntimeInterop.RunIteration(9);
        await runner.SimulateFrames(1);
        Assertions.AssertThat(RuntimeInterop.GetElapsedTimeUnits()).IsEqual(19L);
        assertions.HasContentText("1");

        //Run iteration with not enough time to trigger a new reoccurance
        RuntimeInterop.RunIteration(1);
        await runner.SimulateFrames(1);
        Assertions.AssertThat(RuntimeInterop.GetElapsedTimeUnits()).IsEqual(20L);
        assertions.HasContentText("1");


        //Run iteration with exact time to trigger a new reoccurance
        RuntimeInterop.RunIteration(10);
        await runner.SimulateFrames(1);
        Assertions.AssertThat(RuntimeInterop.GetElapsedTimeUnits()).IsEqual(30L);
        assertions.HasContentText("1");


        //Run iteration with exact time to trigger a new reoccurance twice
        RuntimeInterop.RunIteration(20);
        await runner.SimulateFrames(1);
        Assertions.AssertThat(RuntimeInterop.GetElapsedTimeUnits()).IsEqual(50L);

        assertions.HasContentText("1");
    }
}