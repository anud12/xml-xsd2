using NewGameProject.Tests.XUnit;

namespace GdUnit4.Examples.Basics.Setup.Test.Ffi.Module.CrossModule;

[TestSuite]
public class CrossModuleFfiTests : Steps {
    [TestCase]
    public void Given_cross_module_it_should_log_greeting() {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/helpers.js", "helpers.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .ProcessArchive();

        AssertRuntimeOutputContains("Hello World");
    }

    [TestCase]
    public void Given_cross_module_it_should_log_math_result() {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/helpers.js", "helpers.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .ProcessArchive();

        AssertRuntimeOutputContains("2 + 3 = 5");
    }

    [TestCase]
    public void Given_cross_module_it_should_log_both_lines() {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/helpers.js", "helpers.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .ProcessArchive();

        AssertRuntimeOutputContains("Hello World");
        AssertRuntimeOutputContains("2 + 3 = 5");
    }
}
