using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_11.Area;

[TestSuite]
public partial class TestClass : Steps
{
    [TestCategory("Stage_11")]
    [TestCase]
    public void Given_room_with_area_it_should_find_entities_inside()
    {
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .EnsureDllAccessible()
            .ProcessArchive();

        // The room's area spans (10,10)..(110,110) in the container's world
        // space. The crate (overlap) and npc-in (point) are inside; npc-out
        // (500,500) is far outside.
        var inside = RuntimeInterop.GetEntitiesInsideArea("room");
        Assertions.AssertThat(inside.Length).IsEqual(2);
        Assertions.AssertThat(inside.Contains("crate")).IsTrue();
        Assertions.AssertThat(inside.Contains("npc-in")).IsTrue();
        Assertions.AssertThat(inside.Contains("npc-out")).IsFalse();

        // Deterministic: entity id ascending.
        Assertions.AssertThat(inside[0]).IsEqual("crate");
        Assertions.AssertThat(inside[1]).IsEqual("npc-in");
    }

    [TestCategory("Stage_11")]
    [TestCase]
    public void Given_entity_without_area_it_should_yield_no_presence()
    {
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .EnsureDllAccessible()
            .ProcessArchive();

        // npc-in has no declared area, so it has no presence: nothing is
        // "inside" it.
        var inside = RuntimeInterop.GetEntitiesInsideArea("npc-in");
        Assertions.AssertThat(inside.Length).IsEqual(0);
    }
}
