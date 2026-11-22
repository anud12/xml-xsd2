using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_13.EntityGenerator;

[TestSuite]
public class EntityGeneratorTests : Steps
{
    [TestCategory("Stage_13")]
    [TestCase]
    public void Given_language_it_should_report_generate_value_presence()
    {
        Load();

        // Non-empty language: generateValue is present. Empty language
        // (oneOf([])): absent. And the generated entity (read back through its
        // proxy) is a member of its own language.
        Assertions.AssertThat(RuntimeInterop.GetEntityTextMapValue("probe", "present")).IsEqual("1");
        Assertions.AssertThat(RuntimeInterop.GetEntityTextMapValue("probe", "emptyAbsent")).IsEqual("1");
        Assertions.AssertThat(RuntimeInterop.GetEntityTextMapValue("probe", "sampleIsMember")).IsEqual("1");
    }

    [TestCategory("Stage_13")]
    [TestCase]
    public void Given_language_it_should_only_match_full_field_members()
    {
        Load();

        // L = { alpha/10, beta/10, static/20 }. Exact-field membership: only the
        // (name, hp) pairs in the language pass; mismatched pairs fail.
        Assertions.AssertThat(RuntimeInterop.GetEntityTextMapValue("probe", "alpha10")).IsEqual("1");
        Assertions.AssertThat(RuntimeInterop.GetEntityTextMapValue("probe", "beta10")).IsEqual("1");
        Assertions.AssertThat(RuntimeInterop.GetEntityTextMapValue("probe", "static10")).IsEqual("0");
        Assertions.AssertThat(RuntimeInterop.GetEntityTextMapValue("probe", "static20")).IsEqual("1");
        Assertions.AssertThat(RuntimeInterop.GetEntityTextMapValue("probe", "alpha20")).IsEqual("0");
        Assertions.AssertThat(RuntimeInterop.GetEntityTextMapValue("probe", "static99")).IsEqual("0");
    }

    [TestCategory("Stage_13")]
    [TestCase]
    public void Given_generate_value_it_should_instantiate_a_registered_entity()
    {
        Load();

        // generateValue() registered a new entity in the world under a fresh id
        // (captured in the probe). Read it back directly from the world.
        string sampleId = RuntimeInterop.GetEntityTextMapValue("probe", "sampleId");
        string worldName = RuntimeInterop.GetEntityTextMapValue(sampleId, "name");
        double worldHp = double.Parse(RuntimeInterop.ReadEntityNumberValue(sampleId, "hp"));

        // The instantiated entity matches what its proxy reported.
        Assertions.AssertThat(worldName).IsEqual(RuntimeInterop.GetEntityTextMapValue("probe", "genName"));
        Assertions.AssertThat(worldHp).IsEqual(double.Parse(RuntimeInterop.ReadEntityNumberValue("probe", "genHp")));

        // And it is a valid member of exactly one of the blueprints.
        if (worldName == "alpha" || worldName == "beta")
            Assertions.AssertThat(worldHp).IsEqual(10);
        else
        {
            Assertions.AssertThat(worldName).IsEqual("static");
            Assertions.AssertThat(worldHp).IsEqual(20);
        }
    }

    [TestCategory("Stage_13")]
    [TestCase]
    public void Given_seed_the_generated_entities_are_reproducible()
    {
        // With a fixed seed the LXM stream is reproducible, so the generated
        // blueprint (name + hp) is stable across runs.
        try {
            string first = RunWithSeed(0xABCD);
            string second = RunWithSeed(0xABCD);
            Assertions.AssertThat(first).IsEqual(second);
        } finally {
            RuntimeInterop.SetSeed(0x7FFFFFFF); // restore the default seed
        }
    }

    void Load()
    {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .EnsureDllAccessible()
            .ProcessArchive();
    }

    string RunWithSeed(uint seed)
    {
        CleanupArchive();
        RuntimeInterop.SetSeed(seed);
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .EnsureDllAccessible()
            .ProcessArchive();
        string name = RuntimeInterop.GetEntityTextMapValue("probe", "genName");
        double hp = double.Parse(RuntimeInterop.ReadEntityNumberValue("probe", "genHp"));
        return name + "/" + hp;
    }
}
