using Godot;
using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;
using Vector2 = Godot.Vector2;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_10.Sector.SectorGridBoxCorner;

[TestSuite]
public class SectorGridBoxCornerTests : Steps
{
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

        // Cells of the same container are continuous: the three L cells extend
        // toward each other (no inset on a shared edge) so they read as one solid
        // L-shape, while the box (a different container) keeps a 4px inset on every
        // side. (0,0) touches (1,0) and (0,1); each keeps a 4px inset on its outer
        // edges and the box-adjacent edges.
        scene.AssertPanelThat("caveview")
            .ViewportIsSize(700, 400)
            .HasChildPanelNamed("caveview-cell-0-0", c => c.IsPositionEqual(4, 4).ViewportIsSize(36, 36))
            .HasChildPanelNamed("caveview-cell-1-0", c => c.IsPositionEqual(40, 4).ViewportIsSize(36, 32))
            .HasChildPanelNamed("caveview-cell-0-1", c => c.IsPositionEqual(4, 40).ViewportIsSize(32, 36))
            .HasChildPanelNamed("caveview-cell-1-1", c => c.IsPositionEqual(44, 44).ViewportIsSize(32, 32));

        // Two edge-to-edge portal lines that touch both cells (no gap, no inset):
        //   (1,0)S <-> (1,1)N (vertical gap)  -> 6x8 vertical line   at (57, 36)
        //   (0,1)E <-> (1,1)W (horizontal gap)-> 8x6 horizontal line at (36, 57)
        // Portal id order is an implementation detail, so assert as a set.
        var p0 = scene.GetWindowOrNull("caveview-portal-0");
        var p1 = scene.GetWindowOrNull("caveview-portal-1");
        Assertions.AssertThat(p0 != null && p1 != null).IsTrue();
        bool v = p0!.Position == new Vector2(57, 36) && p0.Size == new Vector2(6, 8);
        bool h = p1!.Position == new Vector2(36, 57) && p1.Size == new Vector2(8, 6);
        bool hv = p0!.Position == new Vector2(36, 57) && p0.Size == new Vector2(8, 6);
        bool vh = p1!.Position == new Vector2(57, 36) && p1.Size == new Vector2(6, 8);
        Assertions.AssertThat((v && h) || (hv && vh)).IsTrue();

        DebugSaveScreenshot("debug.png");
    }
}
