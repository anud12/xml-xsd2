using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_13.NumberBlueprint;

[TestSuite]
public class NumberBlueprintTests : Steps
{
    [TestCategory("Stage_13")]
    [TestCase]
    public void Given_range_it_should_generate_a_value_in_interval()
    {
        Load();

        // generateValue() on a non-empty range is present, and the drawn value
        // lies within the closed interval.
        Assertions.AssertThat(RuntimeInterop.GetEntityTextMapValue("probe", "present")).IsEqual("1");
        Assertions.AssertThat(RuntimeInterop.GetEntityTextMapValue("probe", "inRange")).IsEqual("1");
        double v = double.Parse(RuntimeInterop.ReadEntityNumberValue("probe", "rangeVal"));
        Assertions.AssertThat(v >= 1 && v <= 10).IsTrue();
    }

    [TestCategory("Stage_13")]
    [TestCase]
    public void Given_union_of_ranges_it_should_keep_disjoint_pieces()
    {
        Load();

        // [1,3] and [5,7] stay two pieces; 4 (a gap) is not a member, 2 and 6 are.
        Assertions.AssertThat(RuntimeInterop.GetEntityTextMapValue("probe", "twoPieces")).IsEqual("2");
        Assertions.AssertThat(RuntimeInterop.GetEntityTextMapValue("probe", "gap")).IsEqual("0");
        Assertions.AssertThat(RuntimeInterop.GetEntityTextMapValue("probe", "inP1")).IsEqual("1");
        Assertions.AssertThat(RuntimeInterop.GetEntityTextMapValue("probe", "inP2")).IsEqual("1");
    }

    [TestCategory("Stage_13")]
    [TestCase]
    public void Given_number_blueprint_in_entity_number_map_it_should_instantiate_and_match()
    {
        Load();

        // "code" is generated from the [1,10] range and "kind" is the fixed 0.
        // The instantiated entity's generable field lands in range, the fixed
        // field is exact, and the entity is a member of its own language.
        Assertions.AssertThat(RuntimeInterop.GetEntityTextMapValue("probe", "codeInRange")).IsEqual("1");
        Assertions.AssertThat(RuntimeInterop.GetEntityTextMapValue("probe", "kindZero")).IsEqual("1");
        Assertions.AssertThat(RuntimeInterop.GetEntityTextMapValue("probe", "sampleMember")).IsEqual("1");
    }

    void Load()
    {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .EnsureDllAccessible()
            .ProcessArchive();
    }
}
