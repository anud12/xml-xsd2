using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_14.EntityFilter.Malformed;

[TestSuite]
public class MalformedTests : Steps
{
    [TestCategory("Stage_14")]
    [TestCase]
    public void Given_malformed_filters_it_should_mark_nothing_and_log()
    {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .EnsureDllAccessible()
            .ProcessArchive();

        RuntimeInterop.RunIteration();

        // Two keys, an unknown key, and null are all malformed: nothing is
        // selected and the deterministic log line is emitted.
        Assertions.AssertThat(RuntimeInterop.ReadEntityTextValue("m", "flag")).IsEqual("no");
        AssertRuntimeOutputContains("___filter-malformed");
    }
}
