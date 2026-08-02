using GdUnit4.Examples.Basics.Setup.Sources.UI;
using Godot;
using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;
using Vector2 = Godot.Vector2;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_2.SpriteMap.layerZIndex;

[TestSuite]
public partial class TestClass : Steps {
    [TestCategory("Step_2")]
    [TestCase]
    [RequireGodotRuntime]
    public async Task Given_panel_with_two_layer_bindings_normal_order_it_should_compose_correctly() {
        try {
            AddFileToArchive("layerZIndex/Normal/module/index.js", "index.js")
                .AddFileToArchive("layerZIndex/Normal/module/manifest.json", "manifest.json")
                .AddFileToArchive("layerZIndex/Normal/module/skins/border_top.png", "skins/border_top.png")
                .AddFileToArchive("layerZIndex/Normal/module/skins/texture.png", "skins/texture.png")
                .AddFileToArchive("layerZIndex/Normal/module/maps/idle_frame1.tiff", "maps/idle_frame1.tiff")
                .EnsureDllAccessible()
                .ProcessArchive();

            var scene = LoadTestScene();
            var rootNode = new RootNode();

            scene.AddChild(rootNode);
            rootNode.SetSize(new Vector2() {
                X = 1000,
                Y = 1000
            });
            rootNode.SetAnchorsPreset(Control.LayoutPreset.Center);
            await runner.SimulateFrames(1);

            var panel = rootNode.GetNode<Panel>("characterPanel");
            AssertPanelThat(panel).IsNonNull();
            AssertPanelThat(panel).ViewportMatches("layerZIndex/Normal/module/expected.png");
        }
        catch (Exception e) {
            Assertions.AssertThat(true)
                .OverrideFailureMessage($"Error: {e.Message}\n{e.StackTrace}")
                .IsFalse();
        }
    }

    [TestCategory("Step_2")]
    [TestCase]
    [RequireGodotRuntime]
    public async Task Given_panel_with_two_layer_bindings_swapped_order_it_should_compose_correctly() {
        try {
            AddFileToArchive("layerZIndex/Swapped/module/index.js", "index.js")
                .AddFileToArchive("layerZIndex/Swapped/module/manifest.json", "manifest.json")
                .AddFileToArchive("layerZIndex/Swapped/module/skins/border_top.png", "skins/border_top.png")
                .AddFileToArchive("layerZIndex/Swapped/module/skins/texture.png", "skins/texture.png")
                .AddFileToArchive("layerZIndex/Swapped/module/maps/idle_frame1.tiff", "maps/idle_frame1.tiff")
                .EnsureDllAccessible()
                .ProcessArchive();

            var scene = LoadTestScene();
            var rootNode = new RootNode();

            scene.AddChild(rootNode);
            rootNode.SetSize(new Vector2() {
                X = 1000,
                Y = 1000
            });
            rootNode.SetAnchorsPreset(Control.LayoutPreset.Center);
            await runner.SimulateFrames(1);

            var panel = rootNode.GetNode<Panel>("characterPanel");
            AssertPanelThat(panel).IsNonNull();
            AssertPanelThat(panel).ViewportMatches("layerZIndex/Swapped/module/expected.png");
        }
        catch (Exception e) {
            Assertions.AssertThat(true)
                .OverrideFailureMessage($"Error: {e.Message}\n{e.StackTrace}")
                .IsFalse();
        }
    }
}
