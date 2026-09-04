using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_8.ActionPlan.BusyReject;

[TestSuite]
public partial class TestClass : Steps {
    [TestCategory("Stage_8")]
    [TestCase]
    public void Given_active_plan_another_action_for_same_actor_is_rejected_not_queued() {
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .EnsureDllAccessible()
            .ProcessArchive();

        // Dispatch the spanning action for the actor: the plan walks to the
        // first wait and parks (resumes at 2 GTU); restStart is pending
        // until the next iteration.
        RuntimeInterop.emitActionFor("rest", "guard");
        RuntimeInterop.RunIteration(0);

        AssertRuntimeOutputContains("___busy-test rest start fired___");
        AssertRuntimeOutputContainsNot("___busy-test rest end fired___");

        // Another action for the same actor while the plan is parked:
        // rejected outright - it must neither run nor be queued.
        ClearOutput();
        RuntimeInterop.emitActionFor("dash", "guard");
        AssertRuntimeOutputContainsNot("___busy-test dash fired___");

        // Tick 1: the plan is still parked (resumes at 2); the rejected
        // action must not surface here.
        RuntimeInterop.RunIteration(1);
        AssertRuntimeOutputContainsNot("___busy-test dash fired___");
        AssertRuntimeOutputContainsNot("___busy-test rest end fired___");

        // Tick 2: the plan resumes and completes; the rejected action
        // still never runs.
        RuntimeInterop.RunIteration(1);
        AssertRuntimeOutputContains("___busy-test rest end fired___");
        AssertRuntimeOutputContainsNot("___busy-test dash fired___");

        // The actor is free again: the same action now runs.
        ClearOutput();
        RuntimeInterop.emitActionFor("dash", "guard");
        RuntimeInterop.RunIteration(0);
        AssertRuntimeOutputContains("___busy-test dash fired___");
    }
}
