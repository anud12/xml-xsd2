using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_10.Sector.MultiFootprint;

[TestSuite]
public class SectorMultiFootprintTests : Steps
{
    [TestCase]
    public void Given_an_l_footprint_it_should_expose_only_boundary_edges()
    {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .ProcessArchive();

        var grid = SectorInterop.GetSectorGridById("cave");
        Assertions.AssertThat(grid).IsNotNull();
        Assertions.AssertThat(grid!.Cells.Count).IsEqual(3);

        var cell00 = grid.Cells.Single(c => c.X == 0 && c.Y == 0);
        var cell10 = grid.Cells.Single(c => c.X == 1 && c.Y == 0);
        var cell01 = grid.Cells.Single(c => c.X == 0 && c.Y == 1);

        // L: (0,0) N W  |  (1,0) N E S  |  (0,1) E S W  => 8 boundary edges.
        Assertions.AssertThat(cell00.BoundaryEdges.Count).IsEqual(2);
        Assertions.AssertThat(cell10.BoundaryEdges.Count).IsEqual(3);
        Assertions.AssertThat(cell01.BoundaryEdges.Count).IsEqual(3);
        int total = grid.Cells.Sum(c => c.BoundaryEdges.Count);
        Assertions.AssertThat(total).IsEqual(8);

        // Interior edges are not addressable: (0,0) E faces (1,0), S faces (0,1)
        // (both in the footprint) so neither is a boundary edge.
        Assertions.AssertThat(cell00.BoundaryEdges.Any(e => e.Side == "E")).IsFalse();
        Assertions.AssertThat(cell00.BoundaryEdges.Any(e => e.Side == "S")).IsFalse();
        Assertions.AssertThat(cell00.BoundaryEdges.Any(e => e.Side == "N")).IsTrue();
        Assertions.AssertThat(cell00.BoundaryEdges.Any(e => e.Side == "W")).IsTrue();
    }
}
