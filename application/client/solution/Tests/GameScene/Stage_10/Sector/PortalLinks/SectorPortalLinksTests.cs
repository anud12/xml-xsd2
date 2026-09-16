using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_10.Sector.PortalLinks;

[TestSuite]
public class SectorPortalLinksTests : Steps
{
    [TestCase]
    public void Given_facing_equal_openings_it_should_form_one_full_portal()
    {
        CleanupArchive();
        AddFileToArchive("module-basic/index.js", "index.js")
            .AddFileToArchive("module-basic/manifest.json", "manifest.json")
            .ProcessArchive();

        var grid = SectorInterop.GetSectorGridById("cave");
        Assertions.AssertThat(grid).IsNotNull();
        Assertions.AssertThat(grid!.Portals.Count).IsEqual(1);

        var p = grid.Portals[0];
        // room-a (0,0) E  <->  room-b (1,0) W ; equal length 4, no centering.
        Assertions.AssertThat(p.A.CellX).IsEqual(0);
        Assertions.AssertThat(p.A.CellY).IsEqual(0);
        Assertions.AssertThat(p.A.Side).IsEqual("E");
        Assertions.AssertThat(p.A.Span).IsEqual(0);
        Assertions.AssertThat(p.A.Length).IsEqual(4);
        Assertions.AssertThat(p.B.CellX).IsEqual(1);
        Assertions.AssertThat(p.B.CellY).IsEqual(0);
        Assertions.AssertThat(p.B.Side).IsEqual("W");
        Assertions.AssertThat(p.B.Span).IsEqual(0);
        Assertions.AssertThat(p.B.Length).IsEqual(4);
    }

    [TestCase]
    public void Given_one_walled_side_it_should_form_no_portal()
    {
        CleanupArchive();
        AddFileToArchive("module-walled/index.js", "index.js")
            .AddFileToArchive("module-walled/manifest.json", "manifest.json")
            .ProcessArchive();

        var grid = SectorInterop.GetSectorGridById("cave");
        Assertions.AssertThat(grid).IsNotNull();
        // room-a opens east, but room-b declares no opening on its west edge:
        // the opening faces a walled edge and is walled entirely.
        Assertions.AssertThat(grid!.Portals.Count).IsEqual(0);
    }

    [TestCase]
    public void Given_mismatched_lengths_it_should_center_the_portal()
    {
        CleanupArchive();
        AddFileToArchive("module-mismatch/index.js", "index.js")
            .AddFileToArchive("module-mismatch/manifest.json", "manifest.json")
            .ProcessArchive();

        var grid = SectorInterop.GetSectorGridById("cave");
        Assertions.AssertThat(grid).IsNotNull();
        Assertions.AssertThat(grid!.Portals.Count).IsEqual(1);

        var p = grid.Portals[0];
        // lenA 10, lenB 3 -> L 3, oA floor((10-3)/2)=3, oB 0.
        // room-a side spans 3..6 (span 3 len 3); room-b spans 0..3 (span 0 len 3).
        Assertions.AssertThat(p.A.Side).IsEqual("E");
        Assertions.AssertThat(p.A.Length).IsEqual(3);
        Assertions.AssertThat(p.A.Span).IsEqual(3);
        Assertions.AssertThat(p.B.Side).IsEqual("W");
        Assertions.AssertThat(p.B.Length).IsEqual(3);
        Assertions.AssertThat(p.B.Span).IsEqual(0);
    }

    [TestCase]
    public void Given_a_concave_pocket_it_should_form_a_two_cycle()
    {
        CleanupArchive();
        AddFileToArchive("module-twocycle/index.js", "index.js")
            .AddFileToArchive("module-twocycle/manifest.json", "manifest.json")
            .ProcessArchive();

        var grid = SectorInterop.GetSectorGridById("cave");
        Assertions.AssertThat(grid).IsNotNull();
        Assertions.AssertThat(grid!.Portals.Count).IsEqual(2);

        // Both pocket edges of the L face the filler: (1,0)S->(1,1)N and
        // (0,1)E->(1,1)W. Two distinct portals = a 2-cycle between room-a/b.
        bool pocketSouth = grid.Portals.Any(p =>
            p.A.CellX == 1 && p.A.CellY == 0 && p.A.Side == "S" &&
            p.B.CellX == 1 && p.B.CellY == 1 && p.B.Side == "N");
        bool pocketEast = grid.Portals.Any(p =>
            p.A.CellX == 0 && p.A.CellY == 1 && p.A.Side == "E" &&
            p.B.CellX == 1 && p.B.CellY == 1 && p.B.Side == "W");
        Assertions.AssertThat(pocketSouth).IsTrue();
        Assertions.AssertThat(pocketEast).IsTrue();
    }
}
