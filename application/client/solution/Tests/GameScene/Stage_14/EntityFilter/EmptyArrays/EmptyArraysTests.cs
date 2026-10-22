using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_14.EntityFilter.EmptyArrays;

[TestSuite]
public class EmptyArraysTests : Steps
{
    [TestCategory("Stage_14")]
    [TestCase]
    public void Given_empty_filter_arrays_it_should_match_everything()
    {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .EnsureDllAccessible()
            .ProcessArchive();

        RuntimeInterop.RunIteration();

        // filters:[] / and:[] / or:[] / not:[] are all vacuous truths.
        for (int i = 1; i <= 4; i++)
        {
            string flag = "f" + i;
            Assertions.AssertThat(RuntimeInterop.ReadEntityTextValue("p", flag)).IsEqual("yes");
            Assertions.AssertThat(RuntimeInterop.ReadEntityTextValue("q", flag)).IsEqual("yes");
        }
    }
}
