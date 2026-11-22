using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_14.EntityFilter.ByIdMatch;

[TestSuite]
public class ByIdMatchTests : Steps
{
    [TestCategory("Stage_14")]
    [TestCase]
    public void Given_multiple_entities_it_should_select_the_matched_id_not_the_first()
    {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .EnsureDllAccessible()
            .ProcessArchive();

        // Fire pending effects. The id substring filter has two matches and
        // exactlyOne() therefore skips its update; exact-id and stringMap
        // filters still select "second".
        RuntimeInterop.RunIteration();

        // Exact-id and stringMap selection both mark "second"; substring
        // matching remains covered as a two-match cardinality case.
        Assertions.AssertThat(RuntimeInterop.ReadEntityTextValue("second", "flag"))
            .IsEqual("exact");

        // Neither exact-id nor stringMap predicate selects "first".
        Assertions.AssertThat(RuntimeInterop.ReadEntityTextValue("first", "flag"))
            .IsEqual("no");
    }
}
