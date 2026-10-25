using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_7.DynamicEntityList;

[TestSuite]
public class ContainerDynamicEntityListFfiTests : Steps {

    [TestCategory("Stage_7")]
    [TestCase]
    public void Given_empty_container_when_entities_added_via_effect_the_entityList_panel_should_dynamically_show_them() {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .ProcessArchive();

        // Initially the container is empty and the entityList has no item children.
        var container = ContainerInterop.GetContainerById("inventory");
        Assertions.AssertThat(container.Entities.Length).IsEqual(0);

        var initialChildren = UiStateReader.GetChildren("inv-items");
        Assertions.AssertThat(initialChildren.Count).IsEqual(0);

        // Fire the first action: adds "sword-1" to the inventory container.
        RuntimeInterop.emitActionFor("add-sword", "");
        RuntimeInterop.RunIteration();

        // The container now has the entity.
        container = ContainerInterop.GetContainerById("inventory");
        Assertions.AssertThat(container.Entities.Length).IsEqual(1);
        Assertions.AssertThat(container.Entities.Contains("sword-1")).IsTrue();

        // The entityList panel should dynamically reflect the new entity.
        var children = UiStateReader.GetChildren("inv-items");
        Assertions.AssertThat(children.Count).IsEqual(1);
        Assertions.AssertThat(children[0]).IsEqual("sword-1");

        // Fire the second action: adds "shield-1" to the inventory container.
        RuntimeInterop.emitActionFor("add-shield", "");
        RuntimeInterop.RunIteration();

        // The container now has two entities.
        container = ContainerInterop.GetContainerById("inventory");
        Assertions.AssertThat(container.Entities.Length).IsEqual(2);
        Assertions.AssertThat(container.Entities.Contains("shield-1")).IsTrue();

        // The entityList panel should dynamically reflect both entities.
        children = UiStateReader.GetChildren("inv-items");
        Assertions.AssertThat(children.Count).IsEqual(2);
        Assertions.AssertThat(children[0]).IsEqual("sword-1");
        Assertions.AssertThat(children[1]).IsEqual("shield-1");
    }
}
