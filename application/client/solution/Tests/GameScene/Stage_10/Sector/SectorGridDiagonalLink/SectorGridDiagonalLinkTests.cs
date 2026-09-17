using Godot;
using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;
using Vector2 = Godot.Vector2;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_10.Sector.SectorGridDiagonalLink;

[TestSuite]
public class SectorGridDiagonalLinkTests : Steps
{
    [TestCategory("Stage_10")]
    [TestCase]
    [RequireGodotRuntime]
    public async Task Diagonal_link_routes_a_manhattan_corridor_through_the_gaps()
    {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .AddFileToArchive("module/viewbg.png", "viewbg.png")
            .AddFileToArchive("module/cell.png", "cell.png")
            .EnsureDllAccessible()
            .ProcessArchive();

        var scene = await AttachUiScene();

        // ---- Initial: two 1x1 sectors on the diagonal — A at (0,0) opening east,
        // B at (2,2) opening west. The openings are NOT facing (different rows), so
        // nothing links by adjacency: both are red unlinked dead-ends, no portal.
        var grid = SectorInterop.GetSectorGridById("cave");
        Assertions.AssertThat(grid).IsNotNull();
        Assertions.AssertThat(grid!.Cells.Count).IsEqual(2);
        Assertions.AssertThat(grid.Portals.Count).IsEqual(0);

        scene.AssertPanelThat("caveview")
            .ViewportIsSize(700, 400)
            .HasChildPanelNamed("caveview-cell-0-0", c => c.IsPositionEqual(4, 4).ViewportIsSize(32, 32))
            .HasChildPanelNamed("caveview-cell-2-2", c => c.IsPositionEqual(84, 84).ViewportIsSize(32, 32));

        // A's east dead-end shaft on x = (0+1)*40; B's west dead-end on x = 2*40, y = 2*40.
        var u0 = scene.GetWindowOrNull("caveview-unlinked-0");
        Assertions.AssertThat(u0).IsNotNull();
        Assertions.AssertThat(u0!.Position == new Vector2(37, 0) && u0.Size == new Vector2(6, 40)).IsTrue();
        var u1 = scene.GetWindowOrNull("caveview-unlinked-1");
        Assertions.AssertThat(u1).IsNotNull();
        Assertions.AssertThat(u1!.Position == new Vector2(77, 80) && u1.Size == new Vector2(6, 40)).IsTrue();
        Assertions.AssertThat(scene.GetWindowOrNull("caveview-portal-0")).IsNull();
        DebugSaveScreenshot("debug_initial.png");

        // ---- Fire the action: action -> effect -> linkOpening(A.east, B.west). ----
        RuntimeInterop.emitAction("link-ab");
        RuntimeInterop.RunIteration(0);
        await runner.SimulateFrames(2);

        // ---- After: exactly one portal joins A's east (0,0) to B's west (2,2). ---
        // It is a diagonal link (not facing), so it must render as a Manhattan path,
        // not a straight diagonal, and both dead-ends disappear.
        grid = SectorInterop.GetSectorGridById("cave");
        Assertions.AssertThat(grid).IsNotNull();
        Assertions.AssertThat(grid!.Cells.Count).IsEqual(2);
        Assertions.AssertThat(grid.Portals.Count).IsEqual(1);
        var p = grid.Portals[0];
        Assertions.AssertThat(p.A.CellX == 0 && p.A.CellY == 0 && p.A.Side == "E").IsTrue();
        Assertions.AssertThat(p.B.CellX == 2 && p.B.CellY == 2 && p.B.Side == "W").IsTrue();

        // The corridor travels along the streets (the gaps between cells), not through
        // the buildings. From A's east door it steps into the street at x = 40, drops to
        // the cross-street at y = 40, runs east along it to the street at x = 80, drops
        // to B's row, then steps into B's west door. Five 6px segments:
        //   seg0: (36,17) 4x6    -- A's door into street x=40
        //   seg1: (37,20) 6x20   -- down street x=40 to cross-street y=40
        //   seg2: (40,37) 40x6   -- east along cross-street y=40 to street x=80
        //   seg3: (77,40) 6x60   -- down street x=80 to B's row
        //   seg4: (80,97) 4x6    -- into B's door
        // Plus a 6x6 square at each internal corner so the joints are smooth rectangles,
        // not notched: at the four turn points (40,20), (40,40), (80,40), (80,100).
        scene.AssertPanelThat("caveview")
            .HasChildPanelNamed("caveview-cell-0-0", c => c.IsPositionEqual(4, 4).ViewportIsSize(32, 32))
            .HasChildPanelNamed("caveview-cell-2-2", c => c.IsPositionEqual(84, 84).ViewportIsSize(32, 32))
            .HasChildPanelNamed("caveview-portal-0", c => c.IsPositionEqual(36, 17).ViewportIsSize(4, 6))
            .HasChildPanelNamed("caveview-portal-0-s1", c => c.IsPositionEqual(37, 20).ViewportIsSize(6, 20))
            .HasChildPanelNamed("caveview-portal-0-s2", c => c.IsPositionEqual(40, 37).ViewportIsSize(40, 6))
            .HasChildPanelNamed("caveview-portal-0-s3", c => c.IsPositionEqual(77, 40).ViewportIsSize(6, 60))
            .HasChildPanelNamed("caveview-portal-0-s4", c => c.IsPositionEqual(80, 97).ViewportIsSize(4, 6))
            .HasChildPanelNamed("caveview-portal-0-c1", c => c.IsPositionEqual(37, 17).ViewportIsSize(6, 6))
            .HasChildPanelNamed("caveview-portal-0-c2", c => c.IsPositionEqual(37, 37).ViewportIsSize(6, 6))
            .HasChildPanelNamed("caveview-portal-0-c3", c => c.IsPositionEqual(77, 37).ViewportIsSize(6, 6))
            .HasChildPanelNamed("caveview-portal-0-c4", c => c.IsPositionEqual(77, 97).ViewportIsSize(6, 6));

        // Both openings are linked now, so the unlinked dead-ends are gone.
        Assertions.AssertThat(scene.GetWindowOrNull("caveview-unlinked-0")).IsNull();
        Assertions.AssertThat(scene.GetWindowOrNull("caveview-unlinked-1")).IsNull();

        // Reference screenshot of the whole view: the dark background, the two
        // isolated 32x32 buildings at (0,0) and (2,2), and the orange Manhattan
        // corridor (street-routed, smooth-cornered) joining them. Generated on
        // first run, compared after.
        scene.AssertPanelThat("caveview").ViewportMatches("expected.png");

        DebugSaveScreenshot("debug.png");
    }
}
