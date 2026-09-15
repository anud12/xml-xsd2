using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_8.Behavior.Wait;

[TestSuite]
public partial class TestClass : Steps {
    [TestCategory("Stage_8")]
    [TestCase]
    public void Given_behavior_with_wait_it_should_run_action_wait_action_in_order() {
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .EnsureDllAccessible()
            .ProcessArchive();

        // Before any iteration, nothing in the script has executed.
        AssertRuntimeOutputContainsNot("___wait-test first action fired___");
        AssertRuntimeOutputContainsNot("___wait-test second action fired___");

        // First iteration: the first action fires immediately,
        // then the script suspends on the wait step (2 units).
        RuntimeInterop.RunIteration(0);

        AssertRuntimeOutputContains("___wait-test first action fired___");
        AssertRuntimeOutputContainsNot("___wait-test second action fired___");

        // Second iteration: 1 of 2 wait units elapsed, still waiting.
        ClearOutput();
        RuntimeInterop.RunIteration(1);

        AssertRuntimeOutputContainsNot("___wait-test second action fired___");

        // Third iteration: wait completed, the second action fires.
        ClearOutput();
        RuntimeInterop.RunIteration(1);

        AssertRuntimeOutputContains("___wait-test second action fired___");
    }
}
