using GdUnit4.Examples.Basics.Setup.Sources.UI;
using Godot;
using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;
using Vector2 = Godot.Vector2;

namespace GdUnit4.Examples.Basics.Setup.Test.Step_1.Anchor;

[TestSuite]
public partial class Anchor : Steps {
    [TestCategory("Step_1")]
    [TestCase]
    [RequireGodotRuntime]
    public async Task Given_panel_it_should_apply_anchors() {
        // I create a module from the first folder
        CleanupArchive();
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
            .IsPositionEqual(450, 450);


        AssertScreenshot("expected.png");
    }
}
