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

        var container = EntityInterop.GetEntityById("bag-1")!;
        Assertions.AssertThat(container.Members("members")).IsNotNull();
        Assertions.AssertThat(container.Members("members").Length).IsEqual(3);
        Assertions.AssertThat(container.Members("members").Contains("sword-1")).IsTrue();
        Assertions.AssertThat(container.Members("members").Contains("potion-1")).IsTrue();
        Assertions.AssertThat(container.Members("members").Contains("shield-1")).IsTrue();
    }
}
