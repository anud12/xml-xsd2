using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;

namespace GdUnit4.Examples.Basics.Setup.Test.Ffi.Container.TextNumberMap;

[TestSuite]
public class ContainerTextNumberMapFfiTests : Steps {
    [TestCase]
    public void Given_container_it_should_return_text_map_values() {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .ProcessArchive();

        var container = EntityInterop.GetEntityById("bag-1")!;
        Assertions.AssertThat(container.StringMap).IsNotNull();
        Assertions.AssertThat(container.StringMap["label"]).IsEqual("Main Bag");
    }

    [TestCase]
    public void Given_container_it_should_return_number_map_values() {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .ProcessArchive();

        var container = EntityInterop.GetEntityById("bag-1")!;
        Assertions.AssertThat(container.NumberMap).IsNotNull();
        Assertions.AssertThat(container.NumberMap["capacity"]).IsEqual(20.0);
    }
}
