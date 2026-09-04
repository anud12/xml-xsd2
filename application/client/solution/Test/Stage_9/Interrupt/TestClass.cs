using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_9.Interrupt;

[TestSuite]
public class TestClass : Steps {
    [TestCategory("Stage_9")]
    [TestCase]
    public void Given_interruptible_plan_it_should_overwrite_not_queue_when_action_repeatedly_emitted() {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .ProcessArchive();

        // First begin-task: runs its opening operation, then parks (interruptible).
        RuntimeInterop.emitAction("begin-task", "worker-1");
        RuntimeInterop.RunIteration(0);
        AssertRuntimeOutputContains("task start fired");
        AssertRuntimeOutputContainsNot("task step one");
        AssertRuntimeOutputContainsNot("task step two");
        Assertions.AssertThat(RuntimeInterop.IsActorInterruptible("worker-1")).IsTrue();
        // The parked plan is the begin-task action.
        Assertions.AssertThat(RuntimeInterop.GetActorActiveAction("worker-1")).IsEqual("begin-task");

        // Second begin-task: the parked plan is interruptible, so it is dropped
        // and replaced (overwritten), not queued. Its opening operation re-runs.
        ClearOutput();
        RuntimeInterop.emitAction("begin-task", "worker-1");
        RuntimeInterop.RunIteration(0);
        AssertRuntimeOutputContains("task start fired");
        AssertRuntimeOutputContainsNot("task step one");
        AssertRuntimeOutputContainsNot("task step two");
        // Overwrite, not queue: still a single begin-task plan, no backlog.
        Assertions.AssertThat(RuntimeInterop.GetActorActiveAction("worker-1")).IsEqual("begin-task");
        Assertions.AssertThat(RuntimeInterop.IsActorBusy("worker-1")).IsTrue();

        // Third begin-task: overwrites the parked plan again.
        ClearOutput();
        RuntimeInterop.emitAction("begin-task", "worker-1");
        RuntimeInterop.RunIteration(0);
        AssertRuntimeOutputContains("task start fired");
        AssertRuntimeOutputContainsNot("task step one");
        AssertRuntimeOutputContainsNot("task step two");
        Assertions.AssertThat(RuntimeInterop.GetActorActiveAction("worker-1")).IsEqual("begin-task");

        // The instant action overwrites the parked plan and runs on its own.
        ClearOutput();
        RuntimeInterop.emitAction("instant-task", "worker-1");
        RuntimeInterop.RunIteration(0);
        AssertRuntimeOutputContains("instant fired");
        AssertRuntimeOutputContainsNot("task step one");
        AssertRuntimeOutputContainsNot("task step two");
        // The instant action did not park: there is no longer an active action.
        Assertions.AssertThat(RuntimeInterop.GetActorActiveAction("worker-1")).IsEqual("");

        // Nothing is left queued: the actor is free.
        Assertions.AssertThat(RuntimeInterop.IsActorBusy("worker-1")).IsFalse();

        // And advancing well past the waits surfaces none of the dropped
        // operations, so the overwritten plans were never queued.
        ClearOutput();
        RuntimeInterop.RunIteration(100);
        AssertRuntimeOutputContainsNot("task step one");
        AssertRuntimeOutputContainsNot("task step two");
        Assertions.AssertThat(RuntimeInterop.GetActorActiveAction("worker-1")).IsEqual("");
    }
}
