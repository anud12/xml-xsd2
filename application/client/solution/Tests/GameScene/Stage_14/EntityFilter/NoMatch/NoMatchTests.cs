using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_14.EntityFilter.NoMatch;

[TestSuite]
public class NoMatchTests : Steps
{
    [TestCategory("Stage_14")]
    [TestCase]
    public void Given_a_filter_that_matches_nothing_it_should_mark_nothing_and_log()
    {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .EnsureDllAccessible()
            .ProcessArchive();

        RuntimeInterop.RunIteration();

        // No entity was marked: no first-entity fallback, and the degraded
        // element access does not invoke its callback.
        Assertions.AssertThat(RuntimeInterop.ReadEntityTextValue("x", "flag")).IsEqual("no");
        Assertions.AssertThat(RuntimeInterop.ReadEntityTextValue("y", "flag")).IsEqual("no");
        AssertRuntimeOutputContains("___filter-no-match");
    }
}
