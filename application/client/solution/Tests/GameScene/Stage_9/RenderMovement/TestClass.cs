using Godot;
using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;
using Vector2 = Godot.Vector2;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_9.RenderMovement;

[TestSuite]
public class TestClass : Steps {
    // The 700x500 plane at x:10, y:80 with a 10x5 grid: each cell is 70 wide
    // and 100 tall, so cell (col, row) sits at local (col*70, row*100).
    static Vector2 CellPos(int col, int row) => new(col * 70f, row * 100f);

    [TestCategory("Stage_9")]
    [TestCase]
    [RequireGodotRuntime]
    public async Task Given_grid_entity_it_should_render_moving_cell_on_plane_when_march_fired() {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .AddFileToArchive("module/marker.png", "marker.png")
            .EnsureDllAccessible()
            .ProcessArchive();

        var scene = await AttachUiScene();

        // The 700x500 view exists and materialized the entity as a single
        // positioned child placed on its current cell.
        scene.AssertPanelThat("plane").ViewportIsSize(700, 500)
            .HasChildPanelNamed("node-1", c => c.HasLength(1));

        // Before the move, node-1 sits at (0,0): the top-left cell.
        var c0 = scene.Window("node-1");
        Assertions.AssertThat(c0).IsNotNull();
        scene.AssertPanelThat("node-1")
            .IsPositionEqual(CellPos(0, 0).X, CellPos(0, 0).Y);
        scene.AssertPanelThat("col").HasContentText("0");
        scene.AssertPanelThat("row").HasContentText("0");

        
        
        // Fire the speed-1 move toward (3,2). Bresenham: dist = isqrt(3²+2²)
        // = 3, so x advances 1 GTU/tick and y advances 2/3 GTU/tick (whole
        // units on ticks 2 and 3).
        RuntimeInterop.emitAction("march-node-1");

        // Tick 1: (1,0).
        RuntimeInterop.RunIteration(1);
        await runner.SimulateFrames(2);
        Assertions.AssertThat(ContainerInterop.GetContainerById("grid-1").GetXForEntityId["node-1"]).IsEqual(1.0);
        Assertions.AssertThat(ContainerInterop.GetContainerById("grid-1").GetYForEntityId["node-1"]).IsEqual(0.0);
        scene.AssertPanelThat("node-1").IsPositionEqual(CellPos(1, 0).X, CellPos(1, 0).Y);
        scene.AssertPanelThat("col").HasContentText("1");
        scene.AssertPanelThat("row").HasContentText("0");

        
        // Tick 2: (2,1).
        RuntimeInterop.RunIteration(1);
        await runner.SimulateFrames(2);
        scene.AssertPanelThat("node-1").IsPositionEqual(CellPos(2, 1).X, CellPos(2, 1).Y);
        scene.AssertPanelThat("col").HasContentText("2");
        scene.AssertPanelThat("row").HasContentText("1");

        // Tick 3: (3,2) — the destination is reached and the move ends.
        RuntimeInterop.RunIteration(1);
        await runner.SimulateFrames(2);
        var done = ContainerInterop.GetContainerById("grid-1");
        Assertions.AssertThat(done.GetXForEntityId["node-1"]).IsEqual(3.0);
        Assertions.AssertThat(done.GetYForEntityId["node-1"]).IsEqual(2.0);
        Assertions.AssertThat(RuntimeInterop.IsActorBusy("node-1")).IsFalse();
        scene.AssertPanelThat("node-1").IsPositionEqual(CellPos(3, 2).X, CellPos(3, 2).Y);
        scene.AssertPanelThat("col").HasContentText("3");
        scene.AssertPanelThat("row").HasContentText("2");

        // A further tick does not resume the finished move.
        RuntimeInterop.RunIteration(1);
        await runner.SimulateFrames(2);
        scene.AssertPanelThat("node-1").IsPositionEqual(CellPos(3, 2).X, CellPos(3, 2).Y);
        scene.AssertPanelThat("col").HasContentText("3");
        scene.AssertPanelThat("row").HasContentText("2");
    }
}
