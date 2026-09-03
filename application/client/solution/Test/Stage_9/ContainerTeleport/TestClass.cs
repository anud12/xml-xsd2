using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_9.ContainerTeleport;

[TestSuite]
public class TestClass : Steps {
    [TestCategory("Stage_9")]
    [TestCase]
    public void Given_grid_entity_it_should_relocate_to_destination_when_action_fired() {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .ProcessArchive();

        // Before the action fires, node-1 sits at column=2, row=1.
        var before = ContainerInterop.GetContainerById("grid-1");
        Assertions.AssertThat(before.GetXForEntityId["node-1"]).IsEqual(2.0);
        Assertions.AssertThat(before.GetYForEntityId["node-1"]).IsEqual(1.0);

        // Firing the action runs ctx.teleportTo, relocating node-1 in both x and y.
        RuntimeInterop.emitAction("relocate-node-1");

        var after = ContainerInterop.GetContainerById("grid-1");
        Assertions.AssertThat(after.GetXForEntityId["node-1"]).IsEqual(6.0);
        Assertions.AssertThat(after.GetYForEntityId["node-1"]).IsEqual(3.0);
        // The other member of the container is unaffected.
        Assertions.AssertThat(after.GetXForEntityId["node-2"]).IsEqual(0.0);
        Assertions.AssertThat(after.GetYForEntityId["node-2"]).IsEqual(0.0);
    }

    [TestCase]
    public void Given_grid_entity_it_should_clamp_relocate_to_size_when_clamp_set() {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .ProcessArchive();

        // Destination (x=15, y=9) exceeds sizeX (10) and sizeY (5); clamp caps both.
        RuntimeInterop.emitAction("relocate-node-1-out-of-bounds");

        var after = ContainerInterop.GetContainerById("grid-1");
        Assertions.AssertThat(after.GetXForEntityId["node-1"]).IsEqual(10.0);
        Assertions.AssertThat(after.GetYForEntityId["node-1"]).IsEqual(5.0);
    }
}
