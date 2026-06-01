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
        //
        // // Ensure the window isn't minimized and is visible for the capture
        // DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
        
        
        Game.ARCHIVE_DIR = _currentArchivePath;
        Game.RUN_RUNTIME_LOOP = false;
        
        // Load scene once for the entire test suite with automatic cleanup
        runner = ISceneRunner.Load("res://Scenes/Game.tscn", true);
        var scene = runner.Scene();
        // We maximize the view to bring the window to foreground to see what actually happened in the scene.

        // Verify successful scene loading and runner initialization
        Assertions.AssertThat(runner).IsNotNull();
        Assertions.AssertThat(scene).IsNotNull();
        return scene;
    }
}