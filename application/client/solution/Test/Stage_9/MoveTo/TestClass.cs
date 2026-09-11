using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_9.MoveTo;

[TestSuite]
public class TestClass : Steps {
    [TestCategory("Stage_9")]
    [TestCase]
    [RequireGodotRuntime]
    public async Task Given_grid_entity_it_should_advance_one_cell_per_tick_when_march_fired() {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .EnsureDllAccessible()
            .ProcessArchive();

        var scene = await AttachUiScene();

        // Before the action, node-1 sits at (0,0).
        var before = ContainerInterop.GetContainerById("grid-1");
        Assertions.AssertThat(before.GetXForEntityId["node-1"]).IsEqual(0.0);
        // The readout panels bind node-1's column/row, so the rendered labels
        // show where the entity currently sits.
        scene.AssertPanelThat("col-1").HasContentText("0");
        scene.AssertPanelThat("row-1").HasContentText("0");

        // Fire the speed-1 move toward (5,0): it parks and advances one cell per tick.
        RuntimeInterop.emitAction("march-node-1");

        // Tick 1: one cell along.
        RuntimeInterop.RunIteration(1);
        var t1 = ContainerInterop.GetContainerById("grid-1");
        Assertions.AssertThat(t1.GetXForEntityId["node-1"]).IsEqual(1.0);
        Assertions.AssertThat(t1.GetYForEntityId["node-1"]).IsEqual(0.0);
        await runner.SimulateFrames(2);
        scene.AssertPanelThat("col-1").HasContentText("1");
        scene.AssertPanelThat("row-1").HasContentText("0");

        // Tick 2: another cell.
        RuntimeInterop.RunIteration(1);
        var t2 = ContainerInterop.GetContainerById("grid-1");
        Assertions.AssertThat(t2.GetXForEntityId["node-1"]).IsEqual(2.0);

        // The other member of the container is unaffected.
        Assertions.AssertThat(t2.GetXForEntityId["node-2"]).IsEqual(0.0);
        await runner.SimulateFrames(2);
        scene.AssertPanelThat("col-1").HasContentText("2");
        scene.AssertPanelThat("row-1").HasContentText("0");

        // Advance to the destination: at x=5 the move is exhausted and the actor is free.
        RuntimeInterop.RunIteration(3);
        var done = ContainerInterop.GetContainerById("grid-1");
        Assertions.AssertThat(done.GetXForEntityId["node-1"]).IsEqual(5.0);
        Assertions.AssertThat(done.GetYForEntityId["node-1"]).IsEqual(0.0);
        Assertions.AssertThat(RuntimeInterop.IsActorBusy("node-1")).IsFalse();
        await runner.SimulateFrames(2);
        scene.AssertPanelThat("col-1").HasContentText("5");
        scene.AssertPanelThat("row-1").HasContentText("0");
    }

    [TestCase]
    [RequireGodotRuntime]
    public async Task Given_grid_entity_it_should_cover_multiple_cells_per_tick_when_speed_greater_than_one() {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .EnsureDllAccessible()
            .ProcessArchive();

        var scene = await AttachUiScene();

        // Speed 3 toward (10,0): three cells each tick.
        RuntimeInterop.emitAction("dash-node-1");

        RuntimeInterop.RunIteration(1);
        var t1 = ContainerInterop.GetContainerById("grid-1");
        Assertions.AssertThat(t1.GetXForEntityId["node-1"]).IsEqual(3.0);
        await runner.SimulateFrames(2);
        scene.AssertPanelThat("col-1").HasContentText("3");
        scene.AssertPanelThat("row-1").HasContentText("0");

        RuntimeInterop.RunIteration(1);
        var t2 = ContainerInterop.GetContainerById("grid-1");
        Assertions.AssertThat(t2.GetXForEntityId["node-1"]).IsEqual(6.0);
        await runner.SimulateFrames(2);
        scene.AssertPanelThat("col-1").HasContentText("6");
        scene.AssertPanelThat("row-1").HasContentText("0");

        RuntimeInterop.RunIteration(1);
        var t3 = ContainerInterop.GetContainerById("grid-1");
        Assertions.AssertThat(t3.GetXForEntityId["node-1"]).IsEqual(9.0);
        await runner.SimulateFrames(2);
        scene.AssertPanelThat("col-1").HasContentText("9");
        scene.AssertPanelThat("row-1").HasContentText("0");

        // The final tick: logical position reaches 12, the written position
        // clamps at sizeX (10). The logical position never equals the target
        // (10), so the move keeps trying and the actor holds at the bound edge.
        RuntimeInterop.RunIteration(1);
        var done = ContainerInterop.GetContainerById("grid-1");
        Assertions.AssertThat(done.GetXForEntityId["node-1"]).IsEqual(10.0);
        Assertions.AssertThat(RuntimeInterop.IsActorBusy("node-1")).IsTrue();
        await runner.SimulateFrames(2);
        scene.AssertPanelThat("col-1").HasContentText("10");
        scene.AssertPanelThat("row-1").HasContentText("0");
    }

    [TestCase]
    [RequireGodotRuntime]
    public async Task Given_target_beyond_size_it_should_stop_early_at_bound_when_march_fired() {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .EnsureDllAccessible()
            .ProcessArchive();

        var scene = await AttachUiScene();

        // Target (20,0) exceeds sizeX (10): the written position clamps at 10
        // while the logical position keeps tracking toward 20, so the move
        // never "lands" — the actor holds at the bound edge and stays busy.
        RuntimeInterop.emitAction("march-node-1-out-of-bounds");

        for (int i = 0; i < 10; i++) {
            RuntimeInterop.RunIteration(1);
        }
        var atBound = ContainerInterop.GetContainerById("grid-1");
        Assertions.AssertThat(atBound.GetXForEntityId["node-1"]).IsEqual(10.0);
        Assertions.AssertThat(RuntimeInterop.IsActorBusy("node-1")).IsTrue();
        await runner.SimulateFrames(2);
        scene.AssertPanelThat("col-1").HasContentText("10");
        scene.AssertPanelThat("row-1").HasContentText("0");

        // A further tick does not push it past the bound.
        RuntimeInterop.RunIteration(1);
        var still = ContainerInterop.GetContainerById("grid-1");
        Assertions.AssertThat(still.GetXForEntityId["node-1"]).IsEqual(10.0);
        Assertions.AssertThat(RuntimeInterop.IsActorBusy("node-1")).IsTrue();
        await runner.SimulateFrames(2);
        scene.AssertPanelThat("col-1").HasContentText("10");
        scene.AssertPanelThat("row-1").HasContentText("0");
    }

    [TestCase]
    [RequireGodotRuntime]
    public async Task Given_non_interruptible_move_it_should_reject_new_action_when_march_in_progress() {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .EnsureDllAccessible()
            .ProcessArchive();

        var scene = await AttachUiScene();

        // denyInterrupt before moveTo: the parked move is non-interruptible.
        RuntimeInterop.emitAction("hold-march-node-1");
        RuntimeInterop.RunIteration(1);
        var mid = ContainerInterop.GetContainerById("grid-1");
        Assertions.AssertThat(mid.GetXForEntityId["node-1"]).IsEqual(1.0);
        Assertions.AssertThat(RuntimeInterop.IsActorBusy("node-1")).IsTrue();
        Assertions.AssertThat(RuntimeInterop.IsActorInterruptible("node-1")).IsFalse();
        await runner.SimulateFrames(2);
        scene.AssertPanelThat("col-1").HasContentText("1");
        scene.AssertPanelThat("row-1").HasContentText("0");

        // A new action for the same actor is dropped: node-1 keeps marching on
        // its original path — the readout shows the march position, not the
        // rejected teleport destination (6,3).
        RuntimeInterop.emitActionFor("relocate-node-1", "node-1");
        RuntimeInterop.RunIteration(1);
        var afterReject = ContainerInterop.GetContainerById("grid-1");
        Assertions.AssertThat(afterReject.GetXForEntityId["node-1"]).IsEqual(2.0);
        Assertions.AssertThat(afterReject.GetYForEntityId["node-1"]).IsEqual(0.0);
        Assertions.AssertThat(RuntimeInterop.GetActorActiveAction("node-1")).IsEqual("hold-march-node-1");
        await runner.SimulateFrames(2);
        scene.AssertPanelThat("col-1").HasContentText("2");
        scene.AssertPanelThat("row-1").HasContentText("0");
    }

    [TestCase]
    [RequireGodotRuntime]
    public async Task Given_entity_in_negative_x_it_should_advance_toward_origin_when_march_fired() {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .EnsureDllAccessible()
            .ProcessArchive();

        var scene = await AttachUiScene();

        // node-3 starts at (5,0); a speed-1 move toward (0,0) walks it west.
        var before = ContainerInterop.GetContainerById("grid-1");
        Assertions.AssertThat(before.GetXForEntityId["node-3"]).IsEqual(5.0);
        scene.AssertPanelThat("col-3").HasContentText("5");
        scene.AssertPanelThat("row-3").HasContentText("0");

        RuntimeInterop.emitAction("march-node-3-west");

        RuntimeInterop.RunIteration(1);
        var t1 = ContainerInterop.GetContainerById("grid-1");
        Assertions.AssertThat(t1.GetXForEntityId["node-3"]).IsEqual(4.0);
        await runner.SimulateFrames(2);
        scene.AssertPanelThat("col-3").HasContentText("4");
        scene.AssertPanelThat("row-3").HasContentText("0");

        RuntimeInterop.RunIteration(1);
        var t2 = ContainerInterop.GetContainerById("grid-1");
        Assertions.AssertThat(t2.GetXForEntityId["node-3"]).IsEqual(3.0);
        await runner.SimulateFrames(2);
        scene.AssertPanelThat("col-3").HasContentText("3");
        scene.AssertPanelThat("row-3").HasContentText("0");

        // Arrive at the origin after 5 ticks; the actor is then free.
        for (int i = 0; i < 3; i++) {
            RuntimeInterop.RunIteration(1);
        }
        var done = ContainerInterop.GetContainerById("grid-1");
        Assertions.AssertThat(done.GetXForEntityId["node-3"]).IsEqual(0.0);
        Assertions.AssertThat(done.GetYForEntityId["node-3"]).IsEqual(0.0);
        Assertions.AssertThat(RuntimeInterop.IsActorBusy("node-3")).IsFalse();
        await runner.SimulateFrames(2);
        scene.AssertPanelThat("col-3").HasContentText("0");
        scene.AssertPanelThat("row-3").HasContentText("0");
    }

    [TestCase]
    [RequireGodotRuntime]
    public async Task Given_entity_in_negative_diagonal_it_should_stop_when_destination_reached() {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .EnsureDllAccessible()
            .ProcessArchive();

        var scene = await AttachUiScene();

        // node-4 starts at (5,3); a speed-1 move toward (0,0) walks a straight
        // Bresenham line. dist = isqrt(5²+3²) = 5, so x advances 5/5 = 1 GTU
        // per tick and y advances 3/5 = 0.6 GTU per tick (moving whole units on
        // ticks 2, 4 and 5). The move lands exactly on (0,0) at tick 5.
        RuntimeInterop.emitAction("march-node-4-to-origin");

        // Tick 1: (4,3).
        RuntimeInterop.RunIteration(1);
        var t1 = ContainerInterop.GetContainerById("grid-1");
        Assertions.AssertThat(t1.GetXForEntityId["node-4"]).IsEqual(4.0);
        Assertions.AssertThat(t1.GetYForEntityId["node-4"]).IsEqual(3.0);
        await runner.SimulateFrames(2);
        scene.AssertPanelThat("col-4").HasContentText("4");
        scene.AssertPanelThat("row-4").HasContentText("3");

        // Tick 2: (3,2).
        RuntimeInterop.RunIteration(1);
        var t2 = ContainerInterop.GetContainerById("grid-1");
        Assertions.AssertThat(t2.GetXForEntityId["node-4"]).IsEqual(3.0);
        Assertions.AssertThat(t2.GetYForEntityId["node-4"]).IsEqual(2.0);
        await runner.SimulateFrames(2);
        scene.AssertPanelThat("col-4").HasContentText("3");
        scene.AssertPanelThat("row-4").HasContentText("2");

        // Tick 3: (2,2) — y holds while its fraction pools.
        RuntimeInterop.RunIteration(1);
        var t3 = ContainerInterop.GetContainerById("grid-1");
        Assertions.AssertThat(t3.GetXForEntityId["node-4"]).IsEqual(2.0);
        Assertions.AssertThat(t3.GetYForEntityId["node-4"]).IsEqual(2.0);
        await runner.SimulateFrames(2);
        scene.AssertPanelThat("col-4").HasContentText("2");
        scene.AssertPanelThat("row-4").HasContentText("2");

        // Tick 4: (1,1).
        RuntimeInterop.RunIteration(1);
        var t4 = ContainerInterop.GetContainerById("grid-1");
        Assertions.AssertThat(t4.GetXForEntityId["node-4"]).IsEqual(1.0);
        Assertions.AssertThat(t4.GetYForEntityId["node-4"]).IsEqual(1.0);
        await runner.SimulateFrames(2);
        scene.AssertPanelThat("col-4").HasContentText("1");
        scene.AssertPanelThat("row-4").HasContentText("1");

        // Tick 5: (0,0) — the destination is reached and the move stops.
        RuntimeInterop.RunIteration(1);
        var done = ContainerInterop.GetContainerById("grid-1");
        Assertions.AssertThat(done.GetXForEntityId["node-4"]).IsEqual(0.0);
        Assertions.AssertThat(done.GetYForEntityId["node-4"]).IsEqual(0.0);
        Assertions.AssertThat(RuntimeInterop.IsActorBusy("node-4")).IsFalse();
        await runner.SimulateFrames(2);
        scene.AssertPanelThat("col-4").HasContentText("0");
        scene.AssertPanelThat("row-4").HasContentText("0");

        // A further tick does not resume the finished move.
        RuntimeInterop.RunIteration(1);
        var t6 = ContainerInterop.GetContainerById("grid-1");
        Assertions.AssertThat(t6.GetXForEntityId["node-4"]).IsEqual(0.0);
        Assertions.AssertThat(t6.GetYForEntityId["node-4"]).IsEqual(0.0);
        await runner.SimulateFrames(2);
        scene.AssertPanelThat("col-4").HasContentText("0");
        scene.AssertPanelThat("row-4").HasContentText("0");
    }

    [TestCase]
    [RequireGodotRuntime]
    public async Task Given_odd_angle_target_it_should_walk_straight_staircase_when_march_fired() {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .EnsureDllAccessible()
            .ProcessArchive();

        var scene = await AttachUiScene();

        // node-5 starts at (0,0); a speed-1 move toward (10,3) walks a straight
        // ~16.7° line (neither 45° nor axis-aligned). dist = isqrt(10²+3²) = 10,
        // so x advances 10/10 = 1 GTU per tick and y advances 3/10 = 0.3 GTU per
        // tick (moving whole units on ticks 4, 7 and 10). The move lands exactly
        // on (10,3) at tick 10.
        RuntimeInterop.emitAction("march-node-5-odd-angle");

        // Tick 1: (1,0).
        RuntimeInterop.RunIteration(1);
        var t1 = ContainerInterop.GetContainerById("grid-1");
        Assertions.AssertThat(t1.GetXForEntityId["node-5"]).IsEqual(1.0);
        Assertions.AssertThat(t1.GetYForEntityId["node-5"]).IsEqual(0.0);
        await runner.SimulateFrames(2);
        scene.AssertPanelThat("col-5").HasContentText("1");
        scene.AssertPanelThat("row-5").HasContentText("0");

        // Tick 4: (4,1) — y's first whole unit.
        RuntimeInterop.RunIteration(3);
        var t4 = ContainerInterop.GetContainerById("grid-1");
        Assertions.AssertThat(t4.GetXForEntityId["node-5"]).IsEqual(4.0);
        Assertions.AssertThat(t4.GetYForEntityId["node-5"]).IsEqual(1.0);
        await runner.SimulateFrames(2);
        scene.AssertPanelThat("col-5").HasContentText("4");
        scene.AssertPanelThat("row-5").HasContentText("1");

        // Tick 7: (7,2) — y's second whole unit.
        RuntimeInterop.RunIteration(3);
        var t7 = ContainerInterop.GetContainerById("grid-1");
        Assertions.AssertThat(t7.GetXForEntityId["node-5"]).IsEqual(7.0);
        Assertions.AssertThat(t7.GetYForEntityId["node-5"]).IsEqual(2.0);
        await runner.SimulateFrames(2);
        scene.AssertPanelThat("col-5").HasContentText("7");
        scene.AssertPanelThat("row-5").HasContentText("2");

        // Tick 10: (10,3) — the destination is reached and the move stops.
        RuntimeInterop.RunIteration(3);
        var done = ContainerInterop.GetContainerById("grid-1");
        Assertions.AssertThat(done.GetXForEntityId["node-5"]).IsEqual(10.0);
        Assertions.AssertThat(done.GetYForEntityId["node-5"]).IsEqual(3.0);
        Assertions.AssertThat(RuntimeInterop.IsActorBusy("node-5")).IsFalse();
        await runner.SimulateFrames(2);
        scene.AssertPanelThat("col-5").HasContentText("10");
        scene.AssertPanelThat("row-5").HasContentText("3");
    }
}
