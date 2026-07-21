using System.Runtime.ExceptionServices;
using GdUnit4.Examples.Basics.Setup.Sources.UI;
using Godot;
using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;
using NewGameProject.UI;
using Vector2 = Godot.Vector2;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_7.ContainerListView;

[TestSuite]
public class TestClass : Steps
{
    [TestCategory("Step_7")]
    [TestCase]
    [RequireGodotRuntime]
    public async Task Given_container_list_view_it_renders_three_entity_panels()
    {
        try
        {
            CleanupArchive();
            AddFileToArchive("module/index.js", "index.js")
                .AddFileToArchive("module/manifest.json", "manifest.json")
                .AddFileToArchive("module/texture.png", "texture.png")
                .EnsureDllAccessible()
                .ProcessArchive();

            var scene = LoadTestScene();
            var rootNode = new RootNode();
            var idList = RuntimeInterop.GetPanelIds();

            scene.AddChild(rootNode);
            rootNode.SetSize(new Vector2 { X = 1000, Y = 1000 });
            rootNode.SetAnchorsPreset(Control.LayoutPreset.Center);
            await runner.SimulateFrames(1);
            var panel = rootNode.GetNode<Panel>(idList[0]);
            Assertions.AssertThat(panel).IsNotNull();

        }
        catch (Exception e)
        {
            Assertions.AssertThat(true)
                .OverrideFailureMessage($"Error: {e.Message}\n{e.StackTrace}")
                .IsFalse();
            runner.Scene()?.GetTree().Quit();
        }
    }
}
