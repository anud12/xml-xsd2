using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_11.Area.FindEntitiesInside;

[TestSuite]
public class AreaFindEntitiesInsideTests : Steps
{
    [TestCategory("Stage_11")]
    [TestCase]
    public void Given_room_with_area_it_should_find_entities_inside()
    {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .EnsureDllAccessible()
            .ProcessArchive();

        // The room's "floor" area spans (10,10)..(110,110) in the container's
        // world space. The crate (overlap) and npc-in (point) are inside;
        // npc-out (500,500) is far outside.
        var inside = RuntimeInterop.GetEntitiesInsideArea("room", "floor");
        Assertions.AssertThat(inside.Length).IsEqual(2);
        Assertions.AssertThat(inside.Contains("crate")).IsTrue();
        Assertions.AssertThat(inside.Contains("npc-in")).IsTrue();
        Assertions.AssertThat(inside.Contains("npc-out")).IsFalse();

        // Deterministic: entity id ascending.
        Assertions.AssertThat(inside[0]).IsEqual("crate");
        Assertions.AssertThat(inside[1]).IsEqual("npc-in");
    }
}
