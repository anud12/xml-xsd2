using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_8.Autonomy.ActionExists;

[TestSuite]
public partial class TestClass : Steps {
    [TestCategory("Stage_8")]
    [TestCase]
    public void Given_autonomy_referencing_unregistered_action_it_should_fail_at_load() {
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .EnsureDllAccessible()
            .ProcessArchive();

        // The rule references the unregistered action "missing";
        // load-time validation must reject the definition.
        AssertRuntimeOutputContains("autonomy: action missing not registered in sequence");

        // The behavior was never registered as a result.
        AssertRuntimeOutputContainsNot("autonomy registered: action-exists-behavior");
    }
}
