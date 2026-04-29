using System.Runtime.ExceptionServices;
using CommandLine;
using GdUnit4.Examples.Basics.Setup.Sources.UI;
using Godot;
using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;
using Vector2 = Godot.Vector2;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_2.Children;

[TestSuite]
public class Children : Steps {
    [TestCategory("Step_2")]
    [TestCase(Timeout = 1_000)]
    [RequireGodotRuntime]
    [GodotExceptionMonitor]
    public async Task Given_panel_it_should_apply_children() {
        try {
            // I create a module from the first folder
            AddFileToArchive("module/index.js", "index.js")
                .AddFileToArchive("module/manifest.json", "manifest.json")
                .AddFileToArchive("module/texture.exr", "texture.exr")
                .AddFileToArchive("module/texture_2.exr", "texture_2.exr")
                .EnsureDllAccessible()
                .ProcessArchive();


            var scene = LoadTestScene();
            var rootNode = new Root();
            var idList = RuntimeInterop.GetPanelIds();
            foreach (var id in idList) {
                rootNode.AddChild(new Panel(RuntimeInterop.GetPanelById(id)));
            }

            scene.AddChild(rootNode);
            rootNode.SetSize(new Vector2() {
                X = 1000,
                Y = 1000
            });
            rootNode.SetAnchorsPreset(Control.LayoutPreset.Center);
            await runner.SimulateFrames(1);

            AssertPanelThat(rootNode.GetNode<Panel>(idList[0]))
                .IsNonNull()
                .IsPositionEqual(450, 450)
                .HasChildPanelNamed("child", child => child.IsPositionEqual(0, 0))
                .HasChildPanelNamed("child_2", child => child.IsPositionEqual(0, 10));
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