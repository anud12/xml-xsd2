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

        // Advance past the first wait: the parked segment now runs step one and
        // then denies interruption (denyInterrupt before the second wait).
        ClearOutput();
        RuntimeInterop.RunIteration(100);
        AssertRuntimeOutputContains("task step one");
        AssertRuntimeOutputContainsNot("task step two");
        // Parked again, now non-interruptible.
        Assertions.AssertThat(RuntimeInterop.IsActorInterruptible("worker-1")).IsFalse();
        Assertions.AssertThat(RuntimeInterop.GetActorActiveAction("worker-1")).IsEqual("begin-task");

        // A new action while non-interruptible: it is dropped, the parked plan
        // is neither interrupted nor queued behind it.
        ClearOutput();
        RuntimeInterop.emitAction("begin-task", "worker-1");
        RuntimeInterop.RunIteration(0);
        AssertRuntimeOutputContainsNot("task start fired");
        AssertRuntimeOutputContainsNot("task step two");
        // The original plan is still the active one.
        Assertions.AssertThat(RuntimeInterop.GetActorActiveAction("worker-1")).IsEqual("begin-task");
        Assertions.AssertThat(RuntimeInterop.IsActorBusy("worker-1")).IsTrue();

        // An instant action is dropped too while non-interruptible.
        ClearOutput();
        RuntimeInterop.emitAction("instant-task", "worker-1");
        RuntimeInterop.RunIteration(0);
        AssertRuntimeOutputContainsNot("instant fired");
        Assertions.AssertThat(RuntimeInterop.GetActorActiveAction("worker-1")).IsEqual("begin-task");

        // Advancing past the final wait: the surviving plan finishes its step
        // two and the actor becomes free; the dropped actions never ran.
        ClearOutput();
        RuntimeInterop.RunIteration(100);
        AssertRuntimeOutputContains("task step two");
        AssertRuntimeOutputContainsNot("instant fired");
        AssertRuntimeOutputContainsNot("task start fired");
        Assertions.AssertThat(RuntimeInterop.IsActorBusy("worker-1")).IsFalse();
        Assertions.AssertThat(RuntimeInterop.GetActorActiveAction("worker-1")).IsEqual("");
    }
}
