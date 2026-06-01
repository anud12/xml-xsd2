using System.Runtime.ExceptionServices;
using CommandLine;
using GdUnit4.Examples.Basics.Setup.Sources.UI;
using Godot;
using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;
using Vector2 = Godot.Vector2;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_2.Content.ConstantTextContent.TopLeft;

[TestSuite]
public class TestClass : Steps {
    [TestCategory("Step_2")]
    [TestCase(Timeout = 1_000)]
    [RequireGodotRuntime]
    [GodotExceptionMonitor]
    public async Task Given_panel_it_should_have_content_with_full_align() {
        // I create a module from the first folder
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .AddFileToArchive("module/texture.exr", "texture.exr")
            .EnsureDllAccessible()
            .ProcessArchive();


        var scene = LoadTestScene();
        var rootNode = scene.GetChild<RootNode>(0);
        var idList = RuntimeInterop.GetPanelIds();

        scene.AddChild(rootNode);
        rootNode.SetSize(new Vector2() {
            X = 1000,
            Y = 1000
        });
        rootNode.SetAnchorsPreset(Control.LayoutPreset.Center);
                await runner.SimulateFrames(1);

        AssertPanelThat(rootNode.GetNode<Panel>(idList[0]))
            .IsNonNull()
            .HasContentText("top-left");
        AssertScreenshot("expected.png");
    }
}
