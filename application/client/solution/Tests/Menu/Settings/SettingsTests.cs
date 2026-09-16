using GdUnit4;
using Godot;
using NewGameProject.Tests.XUnit;

using SettingsScene = global::Settings;
using MainMenuScene = global::MainMenu;

namespace GdUnit4.Examples.Basics.Setup.Test.Menu.Settings;

[TestSuite]
public partial class TestClass : Steps
{
    [BeforeTest]
    public async Task Setup()
    {
        await LoadTestScene("res://Scenes/Settings/Settings.tscn");
    }

    [AfterTest]
    public void Teardown()
    {
        ClearSimulatedMouse();
        SettingsStore.Resolution = Vector2I.Zero;
        SettingsStore.Borderless = false;
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Given_settings_page_it_should_show_resolution_and_borderless_options()
    {
        var scene = runner.Scene();
        var options = scene.GetNode<OptionButton>("Content/ResolutionRow/ResolutionOption");
        var borderless = scene.GetNode<CheckButton>("Content/BorderlessRow/BorderlessOption");

        Assertions.AssertThat(options.ItemCount).IsGreater(0);
        Assertions.AssertThat(borderless.Text).Contains("Borderless");
    }

    [TestCase]
    [RequireGodotRuntime]
    public async Task Given_settings_page_changing_resolution_should_update_store()
    {
        var scene = runner.Scene();
        var settings = (SettingsScene)scene;

        // 2560x1440 is preset index 1.
        settings.OnResolutionSelected(1L);
        await runner.SimulateFrames(1);

        Assertions.AssertThat(SettingsStore.Resolution).IsEqual(new Vector2I(2560, 1440));
    }

    [TestCase]
    [RequireGodotRuntime]
    public async Task Given_settings_page_toggling_borderless_should_update_store()
    {
        var scene = runner.Scene();
        var settings = (SettingsScene)scene;

        settings.OnBorderlessToggled(true);
        await runner.SimulateFrames(1);

        Assertions.AssertThat(SettingsStore.Borderless).IsTrue();
    }

    [TestCase]
    [RequireGodotRuntime]
    public async Task Given_settings_page_clicking_back_should_return_to_main_menu()
    {
        var scene = runner.Scene();
        // Drive the back handler directly; the scene change is deferred.
        ((SettingsScene)scene).OnBack();
        await runner.SimulateFrames(5);

        Assertions.AssertThat(GetCurrentScene() is MainMenuScene).IsTrue();
    }
}
