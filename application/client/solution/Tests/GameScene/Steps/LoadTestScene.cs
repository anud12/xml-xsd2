using GdUnit4;
using Godot;

namespace NewGameProject.Tests.XUnit;

public partial class Steps
{
    public ISceneRunner runner;

    /// <summary>
    /// Loads a scene through the deterministic test wrapper: forces the window
    /// back to windowed 1000x1000 (shared mutable state a prior test may have
    /// changed), sets the game to TEST_MODE, and parks the mouse off-screen so
    /// hover tracking starts clean. <paramref name="scenePath"/> defaults to the
    /// game scene; the menu tests pass their own scene. This keeps each test
    /// family's viewport deterministic so full-viewport screenshots don't bleed
    /// state across tests.
    /// </summary>
    public async Task<Node> LoadTestScene(string scenePath = "res://Scenes/GameScene/Game.tscn")
    {
        Vector2I targetSize = new Vector2I(1000, 1000);
        DisplayServer.WindowSetSize(targetSize);

        // ISceneRunner.Load adds each scene as a Root child and makes it the
        // SceneTree.CurrentScene but never removes the previous one, so scenes
        // accumulate in the viewport and bleed into full-viewport screenshots
        // (e.g. the main menu behind a game scene). Free the currently
        // displayed scene before loading so the tree holds exactly the new
        // test scene. Immediate Free (not QueueFree) runs before Load, so the
        // deferred-free/runner overlap that disposes shared state is avoided.
        var leftover = (Engine.GetMainLoop() as SceneTree)?.CurrentScene;
        if (leftover != null && leftover.IsInsideTree())
            leftover.Free();

        Game.ARCHIVE_DIR = _currentArchivePath;
        Game.RUN_RUNTIME_LOOP = false;
        Game.TEST_MODE = true;

        runner = ISceneRunner.Load(scenePath, true);
        var scene = runner.Scene();

        Assertions.AssertThat(runner).IsNotNull();
        Assertions.AssertThat(scene).IsNotNull();

        // The window is shared mutable state across tests. A prior Settings
        // test may have left it Fullscreen (borderless) — WindowSetSize is a
        // no-op on a fullscreen window — or at another size (e.g. 1280x720).
        // The RootNode is FullRect, so top-level windows position against this
        // size; it must be exactly 1000x1000 before any window positioning.
        // Force it back to windowed, then pump frames until the requested size
        // actually sticks (capped so a stuck resize cannot hang the run).
        DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
        await runner.SimulateFrames(1);
        for (int i = 0; i < 60 && DisplayServer.WindowGetSize() != targetSize; i++)
        {
            DisplayServer.WindowSetSize(targetSize);
            await runner.SimulateFrames(1);
        }

        var mouseOffEvent = new InputEventMouseMotion()
        {
            Position = new Vector2(-1, -1),
            GlobalPosition = new Vector2(-1, -1),
        };
        scene.GetViewport().PushInput(mouseOffEvent);

        return scene;
    }
}