using GdUnit4.Examples.Basics.Setup.Sources.UI;
using Godot;
using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;
using static GdUnit4.Assertions;
using Vector2 = Godot.Vector2;

namespace GdUnit4.Examples.Basics.Setup.Test;

[TestSuite]
public partial class TestClass : Steps
{
    // ReSharper disable once NullableWarningSuppressionIsUsed
    private ISceneRunner runner = null!;


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


        // Load scene once for the entire test suite with automatic cleanup
        runner = ISceneRunner.Load("res://Scenes/Test.tscn", true);
        var scene = runner.Scene();
        // We maximize the view to bring the window to foreground to see what actually happened in the scene.

        // Verify successful scene loading and runner initialization
        AssertThat(runner).IsNotNull();
        AssertThat(scene).IsNotNull();

        var rootNode = new Root();
        var idList = RuntimeInterop.GetPanelIds();
        foreach (var id in idList)
        {
            rootNode.AddChild(new Panel(RuntimeInterop.GetPanelById(id)));
        }
        scene.AddChild(rootNode);
        rootNode.SetSize(new Vector2()
        {
            X = 300,
            Y = 300
        });
        rootNode.SetAnchorsPreset(Control.LayoutPreset.Center);
        await runner.SimulateFrames(1);
        runner.MaximizeView();
        foreach (var panel in rootNode.GetChildren().Cast<Panel>())
        {
            AssertPanelThat(panel).IsPositionEqual(150,150);
        }
    }
}