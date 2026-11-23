using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_14.EntityFilter.Members;

[TestSuite]
public class MembersTests : Steps
{
    private void RunMembersModule()
    {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .EnsureDllAccessible()
            .ProcessArchive();
        RuntimeInterop.RunIteration();
    }

    private void AssertCheck(string key)
    {
        RunMembersModule();
        Assertions.AssertThat(RuntimeInterop.ReadEntityTextValue("parent", key)).IsEqual("yes");
    }

    [TestCategory("Stage_14"), TestCase]
    public void Given_members_filters_they_compose_and_narrow() => AssertCheck("flag");

    [TestCategory("Stage_14"), TestCase]
    public void Given_populated_list_without_match_has_is_false() => AssertCheck("noMatch");

    [TestCategory("Stage_14"), TestCase]
    public void Given_dangling_ids_they_are_skipped() => AssertCheck("dangling");

    [TestCategory("Stage_14"), TestCase]
    public void Given_multiple_matches_findBy_iterates_all_in_order() => AssertCheck("projection");

    [TestCategory("Stage_14"), TestCase]
    public void Given_nested_members_has_filters_recursively() => AssertCheck("recursive");

    [TestCategory("Stage_14"), TestCase]
    public void Given_malformed_filter_both_methods_return_no_matches() => AssertCheck("malformed");

    [TestCategory("Stage_14"), TestCase]
    public void Given_matching_first_member_has_short_circuits_but_findBy_does_not() => AssertCheck("shortCircuit");

    [TestCategory("Stage_14"), TestCase]
    public void Given_missing_and_empty_lists_findBy_is_empty() => AssertCheck("emptyProjection");
}
