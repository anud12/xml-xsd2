using GdUnit4.Examples.Basics.Setup.Sources.UI;
using Godot;
using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;
using Vector2 = Godot.Vector2;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_1.Size;

[TestSuite]
public partial class TestClass : Steps {
    
    [TestCategory("Step_1")]
    [TestCase(Timeout = 1_000)]
    [RequireGodotRuntime]
    public async Task Given_panel_it_should_load_the_panel_into_the_scene() {
        try {
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

            AssertPanelThat(rootNode.GetNode<Panel>(idList[0]))
                .IsPositionEqual(-50, -50);


            AssertScreenshot("expected.png");
        }

        catch (Exception e) {
            Assertions.AssertThat(true)
                .OverrideFailureMessage($"Error: {e.Message}\n{e.StackTrace}")
                .IsFalse();
            runner.Scene()?.GetTree().Quit();
        }
    }
}