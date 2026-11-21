using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_13.StringGenerator;

[TestSuite]
public class StringGeneratorTests : Steps
{
    [TestCategory("Stage_13")]
    [TestCase]
    public void Given_name_generator_it_should_generate_every_product_member()
    {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .EnsureDllAccessible()
            .ProcessArchive();

        // <first> x <last> is an ordered product: every combination is a member.
        Assertions.AssertThat(RuntimeInterop.GetEntityTextMapValue("sg", "alSmith")).IsEqual("1");
        Assertions.AssertThat(RuntimeInterop.GetEntityTextMapValue("sg", "alJones")).IsEqual("1");
        Assertions.AssertThat(RuntimeInterop.GetEntityTextMapValue("sg", "boSmith")).IsEqual("1");
        Assertions.AssertThat(RuntimeInterop.GetEntityTextMapValue("sg", "boJones")).IsEqual("1");
    }

    [TestCategory("Stage_13")]
    [TestCase]
    public void Given_name_generator_it_should_only_match_the_full_string()
    {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .EnsureDllAccessible()
            .ProcessArchive();

        // isGeneratedBy is full-match membership, not a substring/prefix test,
        // it is case-sensitive, and it rejects out-of-vocabulary strings.
        Assertions.AssertThat(RuntimeInterop.GetEntityTextMapValue("sg", "substring")).IsEqual("0");
        Assertions.AssertThat(RuntimeInterop.GetEntityTextMapValue("sg", "prefix")).IsEqual("0");
        Assertions.AssertThat(RuntimeInterop.GetEntityTextMapValue("sg", "notInVocab")).IsEqual("0");
        Assertions.AssertThat(RuntimeInterop.GetEntityTextMapValue("sg", "caseSensitive")).IsEqual("0");
    }

    [TestCategory("Stage_13")]
    [TestCase]
    public void Given_language_it_should_report_generate_value_presence()
    {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .EnsureDllAccessible()
            .ProcessArchive();

        // Non-empty language: generateValue is present. Empty language: absent.
        // And a generated grammar is a member of its own language.
        Assertions.AssertThat(RuntimeInterop.GetEntityTextMapValue("sg", "present")).IsEqual("1");
        Assertions.AssertThat(RuntimeInterop.GetEntityTextMapValue("sg", "emptyAbsent")).IsEqual("1");
        Assertions.AssertThat(RuntimeInterop.GetEntityTextMapValue("sg", "sampleIsMember")).IsEqual("1");
    }

    [TestCategory("Stage_13")]
    [TestCase]
    public void Given_generate_value_it_should_yield_a_valid_language_member()
    {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .EnsureDllAccessible()
            .ProcessArchive();

        // generateValue() now picks a RANDOM member (seeded LXM). The exact
        // string is not fixed, but each result must be a valid member of its
        // own language (isGeneratedBy)...
        Assertions.AssertThat(RuntimeInterop.GetEntityTextMapValue("sg", "genNameIsMember")).IsEqual("1");
        Assertions.AssertThat(RuntimeInterop.GetEntityTextMapValue("sg", "genFirstIsMember")).IsEqual("1");
        Assertions.AssertThat(RuntimeInterop.GetEntityTextMapValue("sg", "genConcatIsMember")).IsEqual("1");
        // ...and an empty language is absent, so orElse supplies the fallback.
        Assertions.AssertThat(RuntimeInterop.GetEntityTextMapValue("sg", "genFallback")).IsEqual("FALLBACK");
    }

    [TestCategory("Stage_13")]
    [TestCase]
    public void Given_seed_the_generated_values_are_reproducible()
    {
        // With a fixed seed the LXM stream is reproducible, so the random picks
        // are stable across runs.
        try {
            CleanupArchive();
            RuntimeInterop.SetSeed(0xABCD);
            AddFileToArchive("module/index.js", "index.js")
                .AddFileToArchive("module/manifest.json", "manifest.json")
                .EnsureDllAccessible()
                .ProcessArchive();
            string firstRun = RuntimeInterop.GetEntityTextMapValue("sg", "genName");

            CleanupArchive();
            RuntimeInterop.SetSeed(0xABCD);
            AddFileToArchive("module/index.js", "index.js")
                .AddFileToArchive("module/manifest.json", "manifest.json")
                .EnsureDllAccessible()
                .ProcessArchive();
            string secondRun = RuntimeInterop.GetEntityTextMapValue("sg", "genName");

            Assertions.AssertThat(firstRun).IsEqual(secondRun);
            // and the picked name is a valid member of the name language
            Assertions.AssertThat(RuntimeInterop.GetEntityTextMapValue("sg", "genNameIsMember")).IsEqual("1");
        } finally {
            RuntimeInterop.SetSeed(0x7FFFFFFF); // restore the default seed
        }
    }
}
