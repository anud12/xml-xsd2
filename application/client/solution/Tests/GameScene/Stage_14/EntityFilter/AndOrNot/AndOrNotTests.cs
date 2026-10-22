using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_14.EntityFilter.AndOrNot;

[TestSuite]
public class AndOrNotTests : Steps
{
    [TestCategory("Stage_14")]
    [TestCase]
    public void Given_nested_and_or_not_it_should_match_only_a_and_b()
    {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .EnsureDllAccessible()
            .ProcessArchive();

        RuntimeInterop.RunIteration();

        Assertions.AssertThat(RuntimeInterop.ReadEntityTextValue("a", "flag")).IsEqual("yes");
        Assertions.AssertThat(RuntimeInterop.ReadEntityTextValue("b", "flag")).IsEqual("yes");
        Assertions.AssertThat(RuntimeInterop.ReadEntityTextValue("c", "flag")).IsEqual("no");
        Assertions.AssertThat(RuntimeInterop.ReadEntityTextValue("d", "flag")).IsEqual("no");
    }
}
