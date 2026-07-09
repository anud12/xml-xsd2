using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;

namespace GdUnit4.Examples.Basics.Setup.Test.Ffi.Container.Ids;

[TestSuite]
public class ContainerIdsFfiTests : Steps {
    [TestCase]
    public void Given_containers_it_should_return_all_ids() {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .ProcessArchive();

        var ids = ContainerInterop.GetContainerIds();
        Assertions.AssertThat(ids.Length >= 2).IsTrue();
        Assertions.AssertThat(ids.Contains("bag-1")).IsTrue();
        Assertions.AssertThat(ids.Contains("chest-grid-1")).IsTrue();
    }
}
