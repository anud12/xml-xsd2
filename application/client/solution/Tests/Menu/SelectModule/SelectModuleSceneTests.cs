using System.Linq;
using GdUnit4;
using Godot;
using NewGameProject.Tests.XUnit;

using SelectModuleScene = global::SelectModule;

namespace GdUnit4.Examples.Basics.Setup.Test.Menu.SelectModule;

[TestSuite]
public partial class SelectModuleSceneTests : Steps
{
    [BeforeTest]
    public async Task Setup()
    {
        await LoadTestScene("res://Scenes/SelectModule/SelectModule.tscn");
    }

    [AfterTest]
    public void Teardown()
    {
        ClearSimulatedMouse();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Given_modules_folder_when_picker_loaded_it_lists_test_tick_module()
    {
        var scene = runner.Scene();
        var moduleList = scene.GetNode<VBoxContainer>("Options/ModuleList");
        var texts = moduleList.GetChildren().OfType<Button>().Select(b => b.Text).ToList();
        Assertions.AssertThat(texts).Contains("TestTick");
    }

    [TestCase]
    [RequireGodotRuntime]
    public async Task Given_picker_when_test_tick_selected_it_opens_game_for_that_module()
    {
        Game.TEST_MODE = true;
        Game.SKIP_CREATE_ARCHIVE = true;
        Game.RUN_RUNTIME_LOOP = false;
        try
        {
            var scene = runner.Scene();
            string moduleDir = ProjectSettings.GlobalizePath("res://Modules/TestTick");
            // Drive the selection handler directly (a synthetic push-input click
            // deadlocks the scene-tree's input processing before the signal fires).
            ((SelectModuleScene)scene).OnSelect(moduleDir);
            await runner.SimulateFrames(3);

            Assertions.AssertThat(GetCurrentScene() is Game).IsTrue();
            Assertions.AssertThat(Game.MODULE_DIR).IsEqual(moduleDir);
        }
        finally
        {
            Game.TEST_MODE = false;
            Game.SKIP_CREATE_ARCHIVE = false;
            Game.RUN_RUNTIME_LOOP = true;
        }
    }
}
