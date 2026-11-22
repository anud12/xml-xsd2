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

        var container = EntityInterop.GetEntityById("bag-1")!;
        Assertions.AssertThat(EntityInterop.GetEntityById("sword-1")!.X).IsEqual(3.0);
        Assertions.AssertThat(EntityInterop.GetEntityById("potion-1")!.X).IsEqual(0.0);
        Assertions.AssertThat(EntityInterop.GetEntityById("shield-1")!.X).IsEqual(5.0);
    }

    [TestCase]
    public void Given_container_it_should_return_y_for_entity_id() {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .ProcessArchive();

        var container = EntityInterop.GetEntityById("bag-1")!;
        Assertions.AssertThat(EntityInterop.GetEntityById("sword-1")!.Y).IsEqual(0.0);
        Assertions.AssertThat(EntityInterop.GetEntityById("potion-1")!.Y).IsEqual(0.0);
    }

    [TestCase]
    public void Given_container_it_should_return_span_x_for_entity_id() {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .ProcessArchive();

        var container = EntityInterop.GetEntityById("bag-1")!;
        Assertions.AssertThat(EntityInterop.GetEntityById("sword-1")!.SpanX).IsEqual(2.0);
        Assertions.AssertThat(EntityInterop.GetEntityById("potion-1")!.SpanX).IsEqual(1.0);
    }

    [TestCase]
    public void Given_container_it_should_return_span_y_for_entity_id() {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .ProcessArchive();

        var container = EntityInterop.GetEntityById("bag-1")!;
        Assertions.AssertThat(EntityInterop.GetEntityById("sword-1")!.SpanY).IsEqual(1.0);
    }
}
