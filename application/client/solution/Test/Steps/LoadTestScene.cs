using GdUnit4;
using Godot;

namespace NewGameProject.Tests.XUnit;

public partial class Steps
{
    public ISceneRunner runner;
    
    public Node LoadTestScene()
    {
        // 1. Force the Window Size programmatically
        Vector2I targetSize = new Vector2I(1000, 1000);
        DisplayServer.WindowSetSize(targetSize);

        Game.ARCHIVE_DIR = _currentArchivePath;
        Game.RUN_RUNTIME_LOOP = false;
        Game.TEST_MODE = true;

        // Load scene once for the entire test suite with automatic cleanup
        runner = ISceneRunner.Load("res://Scenes/Game.tscn", true);
        var scene = runner.Scene();

        // Verify successful scene loading and runner initialization
        Assertions.AssertThat(runner).IsNotNull();
        Assertions.AssertThat(scene).IsNotNull();
        return scene;
    }
}