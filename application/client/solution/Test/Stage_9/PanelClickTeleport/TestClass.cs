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

        // The panel represents grid-1 (sizeX 10, sizeY 5) over a 300x300 board.
        // The cursor cell resolves from the container's sizeX/sizeY by the
        // proportion of the click within the window, so the layout's 3x3 tracks
        // are irrelevant to the resolved cell.
        //
        // Column 5, row 2: the board center (local 150,150) is 50% across the
        // 10-column and 5-row grid.
        ClickLocal(board, new Vector2(150, 150));
        await runner.SimulateFrames(1);
        Assertions.AssertThat(ContainerInterop.GetContainerById("grid-1").GetXForEntityId["node-1"]).IsEqual(5.0);
        Assertions.AssertThat(ContainerInterop.GetContainerById("grid-1").GetYForEntityId["node-1"]).IsEqual(2.0);

        // Column 0, row 0: the top-left of the board.
        ClickLocal(board, new Vector2(5, 5));
        await runner.SimulateFrames(1);
        Assertions.AssertThat(ContainerInterop.GetContainerById("grid-1").GetXForEntityId["node-1"]).IsEqual(0.0);
        Assertions.AssertThat(ContainerInterop.GetContainerById("grid-1").GetYForEntityId["node-1"]).IsEqual(0.0);

        // Column 9, row 4: the bottom-right corner — the last cell of the
        // sizeX x sizeY grid, regardless of the 3x3 layout.
        ClickLocal(board, new Vector2(295, 295));
        await runner.SimulateFrames(1);
        Assertions.AssertThat(ContainerInterop.GetContainerById("grid-1").GetXForEntityId["node-1"]).IsEqual(9.0);
        Assertions.AssertThat(ContainerInterop.GetContainerById("grid-1").GetYForEntityId["node-1"]).IsEqual(4.0);
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
