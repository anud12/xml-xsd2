using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;

namespace GdUnit4.Examples.Basics.Setup.Test.Ffi.Container.Id;

[TestSuite]
public class ContainerIdFfiTests : Steps {
    [TestCase]
    public void Given_container_it_should_return_id() {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .ProcessArchive();

        var container = ContainerInterop.GetContainerById("bag-1");
        Assertions.AssertThat(container.Id).IsEqual("bag-1");
    }
}
