using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;

namespace GdUnit4.Examples.Basics.Setup.Test.Ffi.Container.Size;

[TestSuite]
public class ContainerSizeFfiTests : Steps {
    [TestCase]
    public void Given_container_it_should_return_size_x() {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .ProcessArchive();

        var container = EntityInterop.GetEntityById("bag-1")!;
        Assertions.AssertThat(container.NumberMap["sizeX"]).IsNotNull();
        Assertions.AssertThat(container.NumberMap["sizeX"]).IsEqual(20.0);
        Assertions.AssertThat(container.StringMap["outOfBoundsX"]).IsEqual("clamp");
    }

    [TestCase]
    public void Given_container_it_should_return_size_y() {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .ProcessArchive();

        var container = EntityInterop.GetEntityById("bag-1")!;
        Assertions.AssertThat(container.NumberMap["sizeY"]).IsNotNull();
        Assertions.AssertThat(container.NumberMap["sizeY"]).IsEqual(1.0);
        Assertions.AssertThat(container.StringMap["outOfBoundsY"]).IsEqual("clamp");
    }

    [TestCase]
    public void Given_grid_container_it_should_return_wrap_rule() {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .ProcessArchive();

        var container = EntityInterop.GetEntityById("chest-grid-1")!;
        Assertions.AssertThat(container.NumberMap["sizeX"]).IsNotNull();
        Assertions.AssertThat(container.NumberMap["sizeX"]).IsEqual(6.0);
        Assertions.AssertThat(container.StringMap["outOfBoundsX"]).IsEqual("wrap");
        Assertions.AssertThat(container.NumberMap["sizeY"]).IsEqual(4.0);
        Assertions.AssertThat(container.StringMap["outOfBoundsY"]).IsEqual("wrap");
    }
}
