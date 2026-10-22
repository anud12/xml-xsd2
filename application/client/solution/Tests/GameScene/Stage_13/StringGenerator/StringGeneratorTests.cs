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
    public void Given_generate_value_it_should_yield_a_deterministic_concrete_member()
    {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .EnsureDllAccessible()
            .ProcessArchive();

        // A composed product yields its representative member (first x first)...
        Assertions.AssertThat(RuntimeInterop.GetEntityTextMapValue("sg", "genName")).IsEqual("Al Smith");
        // ...and it is deterministic across repeated calls.
        Assertions.AssertThat(RuntimeInterop.GetEntityTextMapValue("sg", "genNameRepeat")).IsEqual("Al Smith");
        // A oneOf generator yields its first alternative.
        Assertions.AssertThat(RuntimeInterop.GetEntityTextMapValue("sg", "genFirst")).IsEqual("Al");
        // A concat of two oneOf generators composes their generated values.
        Assertions.AssertThat(RuntimeInterop.GetEntityTextMapValue("sg", "genConcat")).IsEqual("x1");
        // An empty language is absent, so orElse supplies the fallback.
        Assertions.AssertThat(RuntimeInterop.GetEntityTextMapValue("sg", "genFallback")).IsEqual("FALLBACK");
    }
}
