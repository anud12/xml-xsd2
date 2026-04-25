using GdUnit4.Examples.Basics.Setup.Sources.UI;
using Godot;
using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;
using Vector2 = Godot.Vector2;

namespace GdUnit4.Examples.Basics.Setup.Test.PanelToPanelNode.Size;

[TestSuite]
public partial class TestClass : Steps
{
    [TestCase]
    [RequireGodotRuntime]
    public async Task Given_panel_it_should_load_the_panel_into_the_scene()
    {
        // I create a module from the first folder
        AddFileToArchive("modules/index.js", "index.js")
            .AddFileToArchive("modules/manifest.json", "manifest.json")
            .AddFileToArchive("modules/texture.exr", "texture.exr")
            .EnsureDllAccessible()
            .ProcessArchive();


        var scene = LoadTestScene();
        var rootNode = new Root();
        var idList = RuntimeInterop.GetPanelIds();
        foreach (var id in idList)
        {
            rootNode.AddChild(new Panel(RuntimeInterop.GetPanelById(id))
            {
                Name = id
            });
        }

        scene.AddChild(rootNode);
        rootNode.SetSize(new Vector2()
        {
            X = 1000,
            Y = 1000
        });
        rootNode.SetAnchorsPreset(Control.LayoutPreset.Center);
        await runner.SimulateFrames(1);
        
        AssertPanelThat(rootNode.GetNode<Panel>(idList[0]))
            .IsPositionEqual(-50, -50);

            
        AssertScreenshot("expected.png");
    }
}