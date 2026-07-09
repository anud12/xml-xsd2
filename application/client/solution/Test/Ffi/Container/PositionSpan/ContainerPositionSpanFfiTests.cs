using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;

namespace GdUnit4.Examples.Basics.Setup.Test.Ffi.Container.PositionSpan;

[TestSuite]
public class ContainerPositionSpanFfiTests : Steps {
    [TestCase]
    public void Given_container_it_should_return_x_for_entity_id() {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .ProcessArchive();

        var container = ContainerInterop.GetContainerById("bag-1");
        Assertions.AssertThat(container.GetXForEntityId).IsNotNull();
        Assertions.AssertThat(container.GetXForEntityId["sword-1"]).IsEqual(3.0);
        Assertions.AssertThat(container.GetXForEntityId["potion-1"]).IsEqual(0.0);
        Assertions.AssertThat(container.GetXForEntityId["shield-1"]).IsEqual(5.0);
    }

    [TestCase]
    public void Given_container_it_should_return_y_for_entity_id() {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .ProcessArchive();

        var container = ContainerInterop.GetContainerById("bag-1");
        Assertions.AssertThat(container.GetYForEntityId).IsNotNull();
        Assertions.AssertThat(container.GetYForEntityId["sword-1"]).IsEqual(0.0);
        Assertions.AssertThat(container.GetYForEntityId["potion-1"]).IsEqual(0.0);
    }

    [TestCase]
    public void Given_container_it_should_return_span_x_for_entity_id() {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .ProcessArchive();

        var container = ContainerInterop.GetContainerById("bag-1");
        Assertions.AssertThat(container.GetSpanXForEntityId).IsNotNull();
        Assertions.AssertThat(container.GetSpanXForEntityId["sword-1"]).IsEqual(2.0);
        Assertions.AssertThat(container.GetSpanXForEntityId["potion-1"]).IsEqual(1.0);
    }

    [TestCase]
    public void Given_container_it_should_return_span_y_for_entity_id() {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .ProcessArchive();

        var container = ContainerInterop.GetContainerById("bag-1");
        Assertions.AssertThat(container.GetSpanYForEntityId).IsNotNull();
        Assertions.AssertThat(container.GetSpanYForEntityId["sword-1"]).IsEqual(1.0);
    }
}
