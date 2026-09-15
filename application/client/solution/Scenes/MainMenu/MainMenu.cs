using Godot;

public partial class MainMenu : Control
{
    public override void _Ready()
    {
        GetNode<Button>("Options/NewGameButton").Pressed += OnNewGame;
        GetNode<Button>("Options/SettingsButton").Pressed += OnSettings;
        GetNode<Button>("Options/ExitButton").Pressed += OnExit;
    }

    void OnNewGame()
    {
        GetTree().ChangeSceneToFile("res://Scenes/GameScene/Game.tscn");
    }

    void OnSettings()
    {
        GetTree().ChangeSceneToFile("res://Scenes/Settings/Settings.tscn");
    }

    void OnExit()
    {
        GetTree().Quit();
    }
}
