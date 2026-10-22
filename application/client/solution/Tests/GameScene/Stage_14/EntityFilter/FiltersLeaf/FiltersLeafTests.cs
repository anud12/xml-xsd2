using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_14.EntityFilter.FiltersLeaf;

[TestSuite]
public class FiltersLeafTests : Steps
{
    [TestCategory("Stage_14")]
    [TestCase]
    public void Given_a_filters_leaf_it_should_match_entities_satisfying_every_lambda()
    {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .EnsureDllAccessible()
            .ProcessArchive();

        RuntimeInterop.RunIteration();

        // Only e-smith satisfies both the text and the number lambda.
        Assertions.AssertThat(RuntimeInterop.ReadEntityTextValue("e-smith", "flag")).IsEqual("leaf");
        Assertions.AssertThat(RuntimeInterop.ReadEntityTextValue("e-smith2", "flag")).IsEqual("eq");
        Assertions.AssertThat(RuntimeInterop.ReadEntityTextValue("e-healer", "flag")).IsEqual("no");
    }
}
