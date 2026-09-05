using GdUnit4.Examples.Basics.Setup.Sources.UI;
using Godot;
using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;
using Vector2 = Godot.Vector2;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_9.PanelClickTeleport;

[TestSuite]
public class TestClass : Steps {
    [TestCategory("Stage_9")]
    [TestCase]
    [RequireGodotRuntime]
    public async Task Given_grid_panel_it_should_teleport_node_to_clicked_cell() {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .EnsureDllAccessible()
            .ProcessArchive();

        var scene = await AttachUiScene();

        var board = scene.Window("board");
        Assertions.AssertThat(board).IsNotNull();

        // Before any click, node-1 sits at column=2, row=1.
        var before = ContainerInterop.GetContainerById("grid-1");
        Assertions.AssertThat(before.GetXForEntityId["node-1"]).IsEqual(2.0);
        Assertions.AssertThat(before.GetYForEntityId["node-1"]).IsEqual(1.0);

        // Click the center cell of a 3x3 grid (local 150,150 -> col 1, row 1):
        // the node teleports onto that cell.
        ClickLocal(board, new Vector2(150, 150));
        await runner.SimulateFrames(1);
        var center = ContainerInterop.GetContainerById("grid-1");
        Assertions.AssertThat(center.GetXForEntityId["node-1"]).IsEqual(1.0);
        Assertions.AssertThat(center.GetYForEntityId["node-1"]).IsEqual(1.0);

        // Click the top-left cell (local 5,5 -> col 0, row 0).
        ClickLocal(board, new Vector2(5, 5));
        await runner.SimulateFrames(1);
        var topLeft = ContainerInterop.GetContainerById("grid-1");
        Assertions.AssertThat(topLeft.GetXForEntityId["node-1"]).IsEqual(0.0);
        Assertions.AssertThat(topLeft.GetYForEntityId["node-1"]).IsEqual(0.0);

        // Click the bottom-right cell (local 295,295 -> col 2, row 2).
        ClickLocal(board, new Vector2(295, 295));
        await runner.SimulateFrames(1);
        var bottomRight = ContainerInterop.GetContainerById("grid-1");
        Assertions.AssertThat(bottomRight.GetXForEntityId["node-1"]).IsEqual(2.0);
        Assertions.AssertThat(bottomRight.GetYForEntityId["node-1"]).IsEqual(2.0);
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
