using GdUnit4;
using Godot;
using NewGameProject.Tests.XUnit;

using SettingsScene = global::Settings;

namespace GdUnit4.Examples.Basics.Setup.Test.Menu.GameScene;

[TestSuite]
public partial class TestClass : Steps
{
    [TestCase]
    [RequireGodotRuntime]
    public async Task Given_game_scene_it_should_show_a_settings_button_top_left()
    {
        await AttachUiScene();
        var scene = runner.Scene();
        var settingsButton = scene.GetNodeOrNull<Button>("uiLayers/coreUI/SettingsButton");
        Assertions.AssertThat(settingsButton).IsNotNull();
        if (settingsButton is null) return;

        Assertions.AssertThat(settingsButton.Text).IsEqual("Settings");
        Assertions.AssertThat(settingsButton.Position.X).IsEqual(8f);
        Assertions.AssertThat(settingsButton.Position.Y).IsEqual(8f);
        ClearSimulatedMouse();
    }

    [TestCase]
    [RequireGodotRuntime]
    public async Task Given_game_scene_clicking_settings_should_open_settings_scene()
    {
        await AttachUiScene();
        var scene = runner.Scene();
        var settingsButton = scene.GetNodeOrNull<Button>("uiLayers/coreUI/SettingsButton");
        if (settingsButton is null)
        {
            Assertions.AssertThat(true).OverrideFailureMessage("SettingsButton not found in game scene").IsFalse();
            return;
        }

        settingsButton.EmitSignal("pressed");
        await runner.SimulateFrames(3);

        Assertions.AssertThat(GetCurrentScene() is SettingsScene).IsTrue();
        ClearSimulatedMouse();
    }
}
