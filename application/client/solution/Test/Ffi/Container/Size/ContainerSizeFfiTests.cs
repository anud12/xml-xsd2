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

        var container = ContainerInterop.GetContainerById("bag-1");
        Assertions.AssertThat(container.SizeX).IsNotNull();
        Assertions.AssertThat(container.SizeX.Value.Value).IsEqual(20.0);
        Assertions.AssertThat(container.SizeX.Value.OutOfBounds).IsEqual(OutOfBoundsRule.Clamp);
    }

    [TestCase]
    public void Given_container_it_should_return_size_y() {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .ProcessArchive();

        var container = ContainerInterop.GetContainerById("bag-1");
        Assertions.AssertThat(container.SizeY).IsNotNull();
        Assertions.AssertThat(container.SizeY.Value.Value).IsEqual(1.0);
        Assertions.AssertThat(container.SizeY.Value.OutOfBounds).IsEqual(OutOfBoundsRule.Clamp);
    }

    [TestCase]
    public void Given_grid_container_it_should_return_wrap_rule() {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .ProcessArchive();

        var container = ContainerInterop.GetContainerById("chest-grid-1");
        Assertions.AssertThat(container.SizeX).IsNotNull();
        Assertions.AssertThat(container.SizeX.Value.Value).IsEqual(6.0);
        Assertions.AssertThat(container.SizeX.Value.OutOfBounds).IsEqual(OutOfBoundsRule.Wrap);
        Assertions.AssertThat(container.SizeY.Value.Value).IsEqual(4.0);
        Assertions.AssertThat(container.SizeY.Value.OutOfBounds).IsEqual(OutOfBoundsRule.Wrap);
    }
}
