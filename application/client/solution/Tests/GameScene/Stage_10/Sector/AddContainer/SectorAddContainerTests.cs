using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_10.Sector.AddContainer;

[TestSuite]
public class SectorAddContainerTests : Steps
{
    [TestCase]
    public void Given_two_adjacent_containers_it_should_discover_the_neighbour()
    {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .ProcessArchive();

        var grid = SectorInterop.GetSectorGridById("cave");
        Assertions.AssertThat(grid).IsNotNull();
        Assertions.AssertThat(grid!.Cells.Count).IsEqual(2);

        var cellA = grid.Cells.Single(c => c.X == 0 && c.Y == 0);
        var cellB = grid.Cells.Single(c => c.X == 1 && c.Y == 0);
        Assertions.AssertThat(cellA.Container).IsEqual("room-a");
        Assertions.AssertThat(cellB.Container).IsEqual("room-b");

        // Neighbour discovery: (0,0) exposes an E edge facing (1,0); (1,0)
        // exposes a W edge facing back at (0,0).
        Assertions.AssertThat(cellA.BoundaryEdges.Any(e => e.Side == "E")).IsTrue();
        Assertions.AssertThat(cellB.BoundaryEdges.Any(e => e.Side == "W")).IsTrue();

        // No openings declared, so the shared edge is walled: no portal.
        Assertions.AssertThat(grid.Portals.Count).IsEqual(0);
    }
}
