using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_11.Area.NoPresence;

[TestSuite]
public class AreaNoPresenceTests : Steps
{
    [TestCategory("Stage_11")]
    [TestCase]
    public void Given_entity_without_area_it_should_yield_no_presence()
    {
        CleanupArchive();
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
