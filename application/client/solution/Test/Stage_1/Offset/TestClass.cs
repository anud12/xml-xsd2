using GdUnit4.Examples.Basics.Setup.Sources.UI;
using Godot;
using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;
using Vector2 = Godot.Vector2;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_1.Offset;

[TestSuite]
public partial class Offset : Steps {
    [TestCategory("Step_1")]
    [TestCase]
    [RequireGodotRuntime]
    public async Task Given_panel_it_should_apply_offset_in_a_diamond_shape() {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js");
        AddFileToArchive("module/manifest.json", "manifest.json");
        AddFileToArchive("module/texture.png", "texture.png");
        EnsureDllAccessible();
        ProcessArchive();


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
            .IsPositionEqual(495, 395);

        AssertPanelThat(rootNode.GetNode<Panel>(idList[1]))
            .IsPositionEqual(395, 495);

        AssertPanelThat(rootNode.GetNode<Panel>(idList[2]))
            .IsPositionEqual(495, 595);

        AssertPanelThat(rootNode.GetNode<Panel>(idList[3]))
            .IsPositionEqual(595, 495);

        AssertScreenshot("expected.png");
    }
}