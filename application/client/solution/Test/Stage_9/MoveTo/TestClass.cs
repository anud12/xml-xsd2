using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_9.MoveTo;

[TestSuite]
public class TestClass : Steps {
    [TestCategory("Stage_9")]
    [TestCase]
    public void Given_grid_entity_it_should_advance_one_cell_per_tick_when_march_fired() {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .ProcessArchive();

        // Before the action, node-1 sits at (0,0).
        var before = ContainerInterop.GetContainerById("grid-1");
        Assertions.AssertThat(before.GetXForEntityId["node-1"]).IsEqual(0.0);

        // Fire the speed-1 move toward (5,0): it parks and advances one cell per tick.
        RuntimeInterop.emitAction("march-node-1");

        // Tick 1: one cell along.
        RuntimeInterop.RunIteration(1);
        var t1 = ContainerInterop.GetContainerById("grid-1");
        Assertions.AssertThat(t1.GetXForEntityId["node-1"]).IsEqual(1.0);
        Assertions.AssertThat(t1.GetYForEntityId["node-1"]).IsEqual(0.0);

        // Tick 2: another cell.
        RuntimeInterop.RunIteration(1);
        var t2 = ContainerInterop.GetContainerById("grid-1");
        Assertions.AssertThat(t2.GetXForEntityId["node-1"]).IsEqual(2.0);

        // The other member of the container is unaffected.
        Assertions.AssertThat(t2.GetXForEntityId["node-2"]).IsEqual(0.0);

        // Advance to the destination: at x=5 the move is exhausted and the actor is free.
        RuntimeInterop.RunIteration(3);
        var done = ContainerInterop.GetContainerById("grid-1");
        Assertions.AssertThat(done.GetXForEntityId["node-1"]).IsEqual(5.0);
        Assertions.AssertThat(done.GetYForEntityId["node-1"]).IsEqual(0.0);
        Assertions.AssertThat(RuntimeInterop.IsActorBusy("node-1")).IsFalse();
    }

    [TestCase]
    public void Given_grid_entity_it_should_cover_multiple_cells_per_tick_when_speed_greater_than_one() {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .ProcessArchive();

        // Speed 3 toward (10,0): three cells each tick.
        RuntimeInterop.emitAction("dash-node-1");

        RuntimeInterop.RunIteration(1);
        var t1 = ContainerInterop.GetContainerById("grid-1");
        Assertions.AssertThat(t1.GetXForEntityId["node-1"]).IsEqual(3.0);

        RuntimeInterop.RunIteration(1);
        var t2 = ContainerInterop.GetContainerById("grid-1");
        Assertions.AssertThat(t2.GetXForEntityId["node-1"]).IsEqual(6.0);

        RuntimeInterop.RunIteration(1);
        var t3 = ContainerInterop.GetContainerById("grid-1");
        Assertions.AssertThat(t3.GetXForEntityId["node-1"]).IsEqual(9.0);

        // The final tick lands on the target (overshoot snaps onto it).
        RuntimeInterop.RunIteration(1);
        var done = ContainerInterop.GetContainerById("grid-1");
        Assertions.AssertThat(done.GetXForEntityId["node-1"]).IsEqual(10.0);
        Assertions.AssertThat(RuntimeInterop.IsActorBusy("node-1")).IsFalse();
    }

    [TestCase]
    public void Given_target_beyond_size_it_should_stop_early_at_bound_when_march_fired() {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .ProcessArchive();

        // Target (20,0) exceeds sizeX (10): walks to the bound edge and stops.
        RuntimeInterop.emitAction("march-node-1-out-of-bounds");

        for (int i = 0; i < 10; i++) {
            RuntimeInterop.RunIteration(1);
        }
        var atBound = ContainerInterop.GetContainerById("grid-1");
        Assertions.AssertThat(atBound.GetXForEntityId["node-1"]).IsEqual(10.0);
        Assertions.AssertThat(RuntimeInterop.IsActorBusy("node-1")).IsFalse();

        // A further tick does not push it past the bound.
        RuntimeInterop.RunIteration(1);
        var still = ContainerInterop.GetContainerById("grid-1");
        Assertions.AssertThat(still.GetXForEntityId["node-1"]).IsEqual(10.0);
    }

    [TestCase]
    public void Given_non_interruptible_move_it_should_reject_new_action_when_march_in_progress() {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .ProcessArchive();

        // denyInterrupt before moveTo: the parked move is non-interruptible.
        RuntimeInterop.emitAction("hold-march-node-1");
        RuntimeInterop.RunIteration(1);
        var mid = ContainerInterop.GetContainerById("grid-1");
        Assertions.AssertThat(mid.GetXForEntityId["node-1"]).IsEqual(1.0);
        Assertions.AssertThat(RuntimeInterop.IsActorBusy("node-1")).IsTrue();
        Assertions.AssertThat(RuntimeInterop.IsActorInterruptible("node-1")).IsFalse();

        // A new action for the same actor is dropped: node-1 keeps marching on
        // its original path.
        RuntimeInterop.emitAction("relocate-node-1", "node-1");
        RuntimeInterop.RunIteration(1);
        var afterReject = ContainerInterop.GetContainerById("grid-1");
        Assertions.AssertThat(afterReject.GetXForEntityId["node-1"]).IsEqual(2.0);
        Assertions.AssertThat(afterReject.GetYForEntityId["node-1"]).IsEqual(0.0);
        Assertions.AssertThat(RuntimeInterop.GetActorActiveAction("node-1")).IsEqual("hold-march-node-1");
    }

    [TestCase]
    public void Given_entity_in_negative_x_it_should_advance_toward_origin_when_march_fired() {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .ProcessArchive();

        // node-3 starts at (5,0); a speed-1 move toward (0,0) walks it west.
        var before = ContainerInterop.GetContainerById("grid-1");
        Assertions.AssertThat(before.GetXForEntityId["node-3"]).IsEqual(5.0);

        RuntimeInterop.emitAction("march-node-3-west");

        RuntimeInterop.RunIteration(1);
        var t1 = ContainerInterop.GetContainerById("grid-1");
        Assertions.AssertThat(t1.GetXForEntityId["node-3"]).IsEqual(4.0);

        RuntimeInterop.RunIteration(1);
        var t2 = ContainerInterop.GetContainerById("grid-1");
        Assertions.AssertThat(t2.GetXForEntityId["node-3"]).IsEqual(3.0);

        // Arrive at the origin after 5 ticks; the actor is then free.
        for (int i = 0; i < 3; i++) {
            RuntimeInterop.RunIteration(1);
        }
        var done = ContainerInterop.GetContainerById("grid-1");
        Assertions.AssertThat(done.GetXForEntityId["node-3"]).IsEqual(0.0);
        Assertions.AssertThat(done.GetYForEntityId["node-3"]).IsEqual(0.0);
        Assertions.AssertThat(RuntimeInterop.IsActorBusy("node-3")).IsFalse();
    }

    [TestCase]
    public void Given_entity_in_negative_diagonal_it_should_stop_when_shorter_axis_reaches_target() {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .ProcessArchive();

        // node-4 starts at (5,3); a speed-1 move toward (0,0) walks both axes
        // west and south. The shorter axis (y, 3) exhausts first and ends the
        // move, leaving x at its partial cell.
        RuntimeInterop.emitAction("march-node-4-to-origin");

        // Tick 1: (4,2).
        RuntimeInterop.RunIteration(1);
        var t1 = ContainerInterop.GetContainerById("grid-1");
        Assertions.AssertThat(t1.GetXForEntityId["node-4"]).IsEqual(4.0);
        Assertions.AssertThat(t1.GetYForEntityId["node-4"]).IsEqual(2.0);

        // Tick 2: (3,1).
        RuntimeInterop.RunIteration(1);
        var t2 = ContainerInterop.GetContainerById("grid-1");
        Assertions.AssertThat(t2.GetXForEntityId["node-4"]).IsEqual(3.0);
        Assertions.AssertThat(t2.GetYForEntityId["node-4"]).IsEqual(1.0);

        // Tick 3: y reaches 0 (exhausted) and the move ends; x is at 2.
        RuntimeInterop.RunIteration(1);
        var t3 = ContainerInterop.GetContainerById("grid-1");
        Assertions.AssertThat(t3.GetXForEntityId["node-4"]).IsEqual(2.0);
        Assertions.AssertThat(t3.GetYForEntityId["node-4"]).IsEqual(0.0);
        Assertions.AssertThat(RuntimeInterop.IsActorBusy("node-4")).IsFalse();

        // A further tick does not resume the abandoned x progress.
        RuntimeInterop.RunIteration(1);
        var t4 = ContainerInterop.GetContainerById("grid-1");
        Assertions.AssertThat(t4.GetXForEntityId["node-4"]).IsEqual(2.0);
        Assertions.AssertThat(t4.GetYForEntityId["node-4"]).IsEqual(0.0);
    }
}
