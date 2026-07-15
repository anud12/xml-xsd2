using GdUnit4;
using Godot;

namespace NewGameProject.Tests.XUnit;

public partial class Steps
{
    public ISceneRunner runner;
    
    public Node LoadTestScene()
    {
        Vector2I targetSize = new Vector2I(1000, 1000);
        DisplayServer.WindowSetSize(targetSize);

        Game.ARCHIVE_DIR = _currentArchivePath;
        Game.RUN_RUNTIME_LOOP = false;
        Game.TEST_MODE = true;

        runner = ISceneRunner.Load("res://Scenes/Game.tscn", true);
        var scene = runner.Scene();

        Assertions.AssertThat(runner).IsNotNull();
        Assertions.AssertThat(scene).IsNotNull();

        var mouseOffEvent = new InputEventMouseMotion()
        {
            Position = new Vector2(-1, -1),
            GlobalPosition = new Vector2(-1, -1),
        };
        scene.GetViewport().PushInput(mouseOffEvent);

        return scene;
    }
}