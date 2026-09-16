using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_10.Sector.GridCreate;

[TestSuite]
public class SectorGridCreateTests : Steps
{
    [TestCase]
    public void Given_a_container_with_a_sector_it_should_place_a_cell_with_openings()
    {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .ProcessArchive();

        var grid = SectorInterop.GetSectorGridById("cave");
        Assertions.AssertThat(grid).IsNotNull();
        Assertions.AssertThat(grid!.Cells.Count).IsEqual(1);

        var cell = grid.Cells.Single(c => c.X == 0 && c.Y == 0);
        Assertions.AssertThat(cell.Container).IsEqual("room-a");
        // Isolated cell: all four sides are boundary edges.
        Assertions.AssertThat(cell.BoundaryEdges.Count).IsEqual(4);

        var opening = cell.Openings.Single(o => o.Side == "N");
        Assertions.AssertThat(opening.CellX).IsEqual(0);
        Assertions.AssertThat(opening.CellY).IsEqual(0);
        Assertions.AssertThat(opening.Start).IsEqual(13);
        Assertions.AssertThat(opening.Length).IsEqual(4);

        // No neighbour, so no portals yet.
        Assertions.AssertThat(grid.Portals.Count).IsEqual(0);
    }
}
