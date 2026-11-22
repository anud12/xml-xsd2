using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_7.MutableAddEntity;

[TestSuite]
public class ContainerMutableAddEntityFfiTests : Steps {
    [TestCategory("Stage_7")]
    [TestCase]
    public void Given_empty_container_when_addEntity_via_getMutableContainersBy_it_should_contain_entity() {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .ProcessArchive();

        RuntimeInterop.emitActionFor("add-to-bag", "bag-1");
        RuntimeInterop.RunIteration();

        var container = EntityInterop.GetEntityById("bag-1")!;
        Assertions.AssertThat(container.Members("members")).IsNotNull();
        Assertions.AssertThat(container.Members("members").Length).IsEqual(1);
        Assertions.AssertThat(container.Members("members").Contains("item-3")).IsTrue();
    }
}
