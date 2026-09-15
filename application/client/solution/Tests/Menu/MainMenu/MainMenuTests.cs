using GdUnit4;
using Godot;
using NewGameProject.Tests.XUnit;

using MainMenuScene = global::MainMenu;
using SettingsScene = global::Settings;

namespace GdUnit4.Examples.Basics.Setup.Test.Menu.MainMenu;

[TestSuite]
public partial class TestClass : Steps
{
    [BeforeTest]
    public void Setup()
    {
        LoadSceneForTest("res://Scenes/MainMenu/MainMenu.tscn");
    }

    [AfterTest]
    public void Teardown()
    {
        ClearSimulatedMouse();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Given_main_menu_it_should_show_all_four_options()
    {
        var scene = runner.Scene();
        var newGame = scene.GetNode<Button>("Options/NewGameButton");
        var loadGame = scene.GetNode<Button>("Options/LoadGameButton");
        var settings = scene.GetNode<Button>("Options/SettingsButton");
        var exit = scene.GetNode<Button>("Options/ExitButton");

        Assertions.AssertThat(newGame.Text).IsEqual("New Game");
        Assertions.AssertThat(loadGame.Text).IsEqual("Load Game");
        Assertions.AssertThat(settings.Text).IsEqual("Settings");
        Assertions.AssertThat(exit.Text).IsEqual("Exit");
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Given_main_menu_load_game_should_be_disabled()
    {
        var scene = runner.Scene();
        var loadGame = scene.GetNode<Button>("Options/LoadGameButton");
        Assertions.AssertThat(loadGame.Disabled).IsTrue();
    }

    [TestCase]
    [RequireGodotRuntime]
    public async Task Given_main_menu_clicking_settings_should_open_settings_scene()
    {
        var scene = runner.Scene();
        ClickControl("Options/SettingsButton", scene);
        await runner.SimulateFrames(3);

        Assertions.AssertThat(GetCurrentScene() is SettingsScene).IsTrue();
    }

    [TestCase]
    [RequireGodotRuntime]
    public async Task Given_main_menu_clicking_new_game_should_open_game_scene()
    {
        Game.TEST_MODE = true;
        Game.SKIP_CREATE_ARCHIVE = true;
        Game.RUN_RUNTIME_LOOP = false;
        try
        {
            var scene = runner.Scene();
            ClickControl("Options/NewGameButton", scene);
            await runner.SimulateFrames(3);

            Assertions.AssertThat(GetCurrentScene() is Game).IsTrue();
        }
        finally
        {
            Game.TEST_MODE = false;
            Game.SKIP_CREATE_ARCHIVE = false;
            Game.RUN_RUNTIME_LOOP = true;
        }
    }
}
