using GdUnit4.Examples.Basics.Setup.Sources.UI;
using Godot;
using NewGameProject.Tests.XUnit;
using Vector2 = Godot.Vector2;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_0.OnClickCursor;

[TestSuite]
public class TestClass : Steps {
    [TestCategory("Step_0")]
    [TestCase]
    [RequireGodotRuntime]
    public async Task Given_grid_panel_it_should_emit_action_with_cursor_cell_on_click() {
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .EnsureDllAccessible()
            .ProcessArchive();

        var scene = await AttachUiScene();

        var board = scene.Window("board");
        Assertions.AssertThat(board).IsNotNull();

        // Click the center cell of a 3x3 grid (local 150,150 -> col 1, row 1).
        ClickLocal(board, new Vector2(150, 150));
        await runner.SimulateFrames(1);
        AssertRuntimeOutputContains("___move fired x=1 y=1___");

        // Click the top-left cell (local 5,5 -> col 0, row 0).
        ClearOutput();
        ClickLocal(board, new Vector2(5, 5));
        await runner.SimulateFrames(1);
        AssertRuntimeOutputContains("___move fired x=0 y=0___");

        // Click the bottom-right cell (local 295,295 -> col 2, row 2).
        ClearOutput();
        ClickLocal(board, new Vector2(295, 295));
        await runner.SimulateFrames(1);
        AssertRuntimeOutputContains("___move fired x=2 y=2___");
    }

    void ClickLocal(UiWindow window, Vector2 local) {
        var globalPos = window.GlobalPosition + local;
        var press = new InputEventMouseButton {
            Position = globalPos,
            GlobalPosition = globalPos,
            ButtonIndex = MouseButton.Left,
            Pressed = true,
            ButtonMask = MouseButtonMask.Left
        };
        runner.Scene().GetViewport().PushInput(press);
        var release = (InputEventMouseButton)press.Duplicate();
        release.Pressed = false;
        runner.Scene().GetViewport().PushInput(release);
    }
}
