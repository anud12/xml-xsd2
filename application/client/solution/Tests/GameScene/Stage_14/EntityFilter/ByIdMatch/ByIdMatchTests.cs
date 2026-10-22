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

        // Fire the pending effects (both select "second" via different id
        // filter shapes). The old first-entity fallback would have marked
        // "first" whenever a filter did not match; the evaluator must not.
        RuntimeInterop.RunIteration();

        // "second" is selected by both the exact and the substring id
        // filters, ending on the later "contains" mark.
        Assertions.AssertThat(RuntimeInterop.ReadEntityTextValue("second", "flag"))
            .IsEqual("contains");

        // "first" (created first) must be untouched — the fallback is gone.
        Assertions.AssertThat(RuntimeInterop.ReadEntityTextValue("first", "flag"))
            .IsEqual("no");
    }
}
