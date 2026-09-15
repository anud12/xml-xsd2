using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;

namespace GdUnit4.Examples.Basics.Setup.Test.Ffi.Container.Entities;

[TestSuite]
public class ContainerEntitiesFfiTests : Steps {
    [TestCase]
    public void Given_container_it_should_list_entities() {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .ProcessArchive();

        var container = ContainerInterop.GetContainerById("bag-1");
        Assertions.AssertThat(container.Entities).IsNotNull();
        Assertions.AssertThat(container.Entities.Length).IsEqual(3);
        Assertions.AssertThat(container.Entities.Contains("sword-1")).IsTrue();
        Assertions.AssertThat(container.Entities.Contains("potion-1")).IsTrue();
        Assertions.AssertThat(container.Entities.Contains("shield-1")).IsTrue();
    }
}
