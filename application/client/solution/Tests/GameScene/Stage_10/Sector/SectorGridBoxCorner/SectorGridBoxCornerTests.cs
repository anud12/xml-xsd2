using Godot;
using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;
using Vector2 = Godot.Vector2;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_10.Sector.SectorGridBoxCorner;

[TestSuite]
public class SectorGridBoxCornerTests : Steps
{
    // cell (x, y) sits at local (x*40 + 4, y*40 + 4) within the view (4px inset
    // from the 8px cell gap).
    static Vector2 CellPos(int x, int y) => new(x * 40f + 4f, y * 40f + 4f);

    [TestCategory("Stage_10")]
    [TestCase]
    [RequireGodotRuntime]
    public async Task Box_at_1_1_links_to_L_on_1_0_and_0_1()
    {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .AddFileToArchive("module/viewbg.png", "viewbg.png")
            .AddFileToArchive("module/cell.png", "cell.png")
            .EnsureDllAccessible()
            .ProcessArchive();

        var scene = await AttachUiScene();

        // The "cave" grid: an L sector at [0,0] over cells (0,0),(1,0),(0,1) plus a
        // single-cell "box" sector at [1,1]. Four unique cells, two portals.
        var grid = SectorInterop.GetSectorGridById("cave");
        Assertions.AssertThat(grid).IsNotNull();
        Assertions.AssertThat(grid!.Cells.Count).IsEqual(4);
        Assertions.AssertThat(grid.Portals.Count).IsEqual(2);

        // Portal 1: L(1,0) S <-> box(1,1) N  (vertical adjacency  -> horizontal shaft).
        // Portal 2: L(0,1) E <-> box(1,1) W  (horizontal adjacency -> vertical shaft).
        // A/B and portal ordering are implementation details, so match as a set.
        bool IsNpS(Portal p)
        {
            bool ns = p.A.CellX == 1 && p.A.CellY == 0 && p.A.Side == "S"
                   && p.B.CellX == 1 && p.B.CellY == 1 && p.B.Side == "N";
            bool sn = p.A.CellX == 1 && p.A.CellY == 1 && p.A.Side == "N"
                   && p.B.CellX == 1 && p.B.CellY == 0 && p.B.Side == "S";
            return ns || sn;
        }
        bool IsEpW(Portal p)
        {
            bool ew = p.A.CellX == 0 && p.A.CellY == 1 && p.A.Side == "E"
                   && p.B.CellX == 1 && p.B.CellY == 1 && p.B.Side == "W";
            bool we = p.A.CellX == 1 && p.A.CellY == 1 && p.A.Side == "W"
                   && p.B.CellX == 0 && p.B.CellY == 1 && p.B.Side == "E";
            return ew || we;
        }
        var ps = grid.Portals;
        Assertions.AssertThat(
            (IsNpS(ps[0]) && IsEpW(ps[1])) || (IsEpW(ps[0]) && IsNpS(ps[1]))).IsTrue();

        // Four cell windows, one per footprint square, each inset 4px and spaced 40px.
        scene.AssertPanelThat("caveview")
            .ViewportIsSize(700, 400)
            .HasChildPanelNamed("caveview-cell-0-0", c => c.IsPositionEqual(CellPos(0, 0).X, CellPos(0, 0).Y))
            .HasChildPanelNamed("caveview-cell-1-0", c => c.IsPositionEqual(CellPos(1, 0).X, CellPos(1, 0).Y))
            .HasChildPanelNamed("caveview-cell-0-1", c => c.IsPositionEqual(CellPos(0, 1).X, CellPos(0, 1).Y))
            .HasChildPanelNamed("caveview-cell-1-1", c => c.IsPositionEqual(CellPos(1, 1).X, CellPos(1, 1).Y));

        // Two engine-drawn headless shafts centered on the shared boundary edges:
        //   vertical adjacency  -> horizontal shaft 40x6 at (40, 37)
        //   horizontal adjacency-> vertical shaft   6x40 at (37, 40)
        // Portal id order is an implementation detail, so assert as a set.
        var p0 = scene.GetWindowOrNull("caveview-portal-0");
        var p1 = scene.GetWindowOrNull("caveview-portal-1");
        Assertions.AssertThat(p0 != null && p1 != null).IsTrue();
        bool h = p0!.Position == new Vector2(40, 37) && p0.Size == new Vector2(40, 6);
        bool v = p1!.Position == new Vector2(37, 40) && p1.Size == new Vector2(6, 40);
        bool hv = p0!.Position == new Vector2(37, 40) && p0.Size == new Vector2(6, 40);
        bool vh = p1!.Position == new Vector2(40, 37) && p1.Size == new Vector2(40, 6);
        Assertions.AssertThat((h && v) || (hv && vh)).IsTrue();
    }
}
