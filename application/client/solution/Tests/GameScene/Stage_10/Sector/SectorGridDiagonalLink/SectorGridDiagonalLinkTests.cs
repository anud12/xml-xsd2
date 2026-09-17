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

        scene.AssertPanelThat("caveview")
            .HasChildPanelNamed("caveview-cell-0-0", c => c.IsPositionEqual(4, 4).ViewportIsSize(32, 32))
            .HasChildPanelNamed("caveview-cell-2-2", c => c.IsPositionEqual(84, 84).ViewportIsSize(32, 32));

        // The corridor travels along the streets (the gaps between cells), not through
        // the buildings. From A's east door it steps into the street at x = 40, drops to
        // the cross-street at y = 40, runs east along it to the street at x = 80, drops
        // to B's row, then steps into B's west door. Five 6px segments:
        //   seg0: (36,17) 4x6    -- A's door into street x=40
        //   seg1: (37,20) 6x20   -- down street x=40 to cross-street y=40
        //   seg2: (40,37) 40x6   -- east along cross-street y=40 to street x=80
        //   seg3: (77,40) 6x60   -- down street x=80 to B's row
        //   seg4: (80,97) 4x6    -- into B's door
        var s0 = scene.GetWindowOrNull("caveview-portal-0");
        Assertions.AssertThat(s0).IsNotNull();
        Assertions.AssertThat(s0!.Position == new Vector2(36, 17) && s0.Size == new Vector2(4, 6)).IsTrue();
        var s1 = scene.GetWindowOrNull("caveview-portal-0-s1");
        Assertions.AssertThat(s1).IsNotNull();
        Assertions.AssertThat(s1!.Position == new Vector2(37, 20) && s1.Size == new Vector2(6, 20)).IsTrue();
        var s2 = scene.GetWindowOrNull("caveview-portal-0-s2");
        Assertions.AssertThat(s2).IsNotNull();
        Assertions.AssertThat(s2!.Position == new Vector2(40, 37) && s2.Size == new Vector2(40, 6)).IsTrue();
        var s3 = scene.GetWindowOrNull("caveview-portal-0-s3");
        Assertions.AssertThat(s3).IsNotNull();
        Assertions.AssertThat(s3!.Position == new Vector2(77, 40) && s3.Size == new Vector2(6, 60)).IsTrue();
        var s4 = scene.GetWindowOrNull("caveview-portal-0-s4");
        Assertions.AssertThat(s4).IsNotNull();
        Assertions.AssertThat(s4!.Position == new Vector2(80, 97) && s4.Size == new Vector2(4, 6)).IsTrue();

        // Both openings are linked now, so the unlinked dead-ends are gone.
        Assertions.AssertThat(scene.GetWindowOrNull("caveview-unlinked-0")).IsNull();
        Assertions.AssertThat(scene.GetWindowOrNull("caveview-unlinked-1")).IsNull();

        DebugSaveScreenshot("debug.png");
    }
}
