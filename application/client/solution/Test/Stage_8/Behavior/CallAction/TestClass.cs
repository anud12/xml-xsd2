using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_8.Behavior.CallAction;

[TestSuite]
public partial class TestClass : Steps {
    [TestCategory("Stage_8")]
    [TestCase]
    public void Given_behavior_module_it_should_call_action_when_triggered() {
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .EnsureDllAccessible()
            .ProcessArchive();

        // Extraction: the runtime must have extracted the behavior
        // definition from the module source and recorded the attachment.
        AssertRuntimeOutputContains("behavior registered: guard-behavior");
        AssertRuntimeOutputContains("behavior attached: guard -> guard-behavior");

        // The rule's do traced a step script referencing the patrol action;
        // the action itself must not fire until it is called.
        AssertRuntimeOutputContainsNot("___From module patrol action fired___");

        RuntimeInterop.RunIteration(1);

        // Calling the action fires its handler (debug log).
        AssertRuntimeOutputContains("___From module patrol action fired___");
    }
}
