using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;
using static GdUnit4.Assertions;

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
            .EnsureDllAccessible()
            .ProcessArchive();


        // Load scene once for the entire test suite with automatic cleanup
        runner = ISceneRunner.Load("res://Scenes/Test.tscn", true);
        // We maximize the view to bring the window to foreground to see what actually happened in the scene.

        // Verify successful scene loading and runner initialization
        AssertThat(runner).IsNotNull();
        AssertThat(runner.Scene()).IsNotNull();

        var idList = RuntimeInterop.GetPanelIds();
        foreach (var id in idList)
        {
            runner.Scene().AddChild(new Panel(RuntimeInterop.GetPanelById(id)));
        }
    }
}