using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_7.EntityListBackgroundAnim;

[TestSuite]
public class ContainerEntityListBackgroundAnimFfiTests : Steps {

    [TestCategory("Stage_7")]
    [TestCase]
    public void Given_entity_list_item_with_background_animation_when_reoccurring_effect_adds_entities_the_effects_should_still_execute() {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .AddFileToArchive("../../../../Modules/TestTick/black_pixel.png", "black_pixel.png")
            .ProcessArchive();

        // First iteration: the queued tick effect fires, adding entity 1.
        RuntimeInterop.RunIteration(10);
        var container = ContainerInterop.GetContainerById("inventory");
        Assertions.AssertThat(container.Entities.Length).IsEqual(1);

        // Subsequent iterations: the reoccurring effect keeps firing.
        RuntimeInterop.RunIteration(10);
        container = ContainerInterop.GetContainerById("inventory");
        Assertions.AssertThat(container.Entities.Length).IsEqual(2);

        RuntimeInterop.RunIteration(10);
        container = ContainerInterop.GetContainerById("inventory");
        Assertions.AssertThat(container.Entities.Length).IsEqual(3);

        RuntimeInterop.RunIteration(10);
        container = ContainerInterop.GetContainerById("inventory");
        Assertions.AssertThat(container.Entities.Length).IsEqual(4);

        // The entity list should reflect all entities.
        var children = UiStateReader.GetChildren("inv-items");
        Assertions.AssertThat(children.Count).IsEqual(4);
    }
}
