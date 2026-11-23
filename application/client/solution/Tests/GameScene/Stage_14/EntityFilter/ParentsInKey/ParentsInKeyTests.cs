using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_14.EntityFilter.ParentsInKey;

[TestSuite]
public class ParentsInKeyTests : Steps
{
    private void AssertCheck(string key)
    {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .EnsureDllAccessible()
            .ProcessArchive();
        RuntimeInterop.RunIteration();
        Assertions.AssertThat(RuntimeInterop.ReadEntityTextValue(key == "unlisted" ? "unlisted" : "candidate", key)).IsEqual("yes");
    }

    [TestCategory("Stage_14"), TestCase] public void Key_match() => AssertCheck("match");
    [TestCategory("Stage_14"), TestCase] public void Key_no_match() => AssertCheck("noMatch");
    [TestCategory("Stage_14"), TestCase] public void Unlisted_candidate() => AssertCheck("unlisted");
    [TestCategory("Stage_14"), TestCase] public void Any_parent() => AssertCheck("any");
    [TestCategory("Stage_14"), TestCase] public void Deduplication() => AssertCheck("dedup");
    [TestCategory("Stage_14"), TestCase] public void Ordering() => AssertCheck("ordering");
    [TestCategory("Stage_14"), TestCase] public void Chained_hasEntity() => AssertCheck("has");
    [TestCategory("Stage_14"), TestCase] public void Chained_findBy() => AssertCheck("find");
    [TestCategory("Stage_14"), TestCase] public void Malformed_and_throwing_key_tests() => AssertCheck("malformed");
    [TestCategory("Stage_14"), TestCase] public void Composition() => AssertCheck("composition");
}
