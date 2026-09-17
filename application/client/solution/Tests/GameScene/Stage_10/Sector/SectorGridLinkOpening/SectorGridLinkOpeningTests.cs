using Godot;
using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;
using Vector2 = Godot.Vector2;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_10.Sector.SectorGridLinkOpening;

[TestSuite]
public class SectorGridLinkOpeningTests : Steps
{
    [TestCategory("Stage_10")]
    [TestCase]
    [RequireGodotRuntime]
    public async Task Link_opening_stretches_a_portal_line_between_two_sectors()
    {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .AddFileToArchive("module/viewbg.png", "viewbg.png")
            .AddFileToArchive("module/cell.png", "cell.png")
            .EnsureDllAccessible()
            .ProcessArchive();

        var scene = await AttachUiScene();

        // ---- Initial: two separate 1x1 sectors, each with an unlinked opening.
        // A is at (0,0) opening east; B is at (2,0) opening west, with one blank
        // square between them. Neither opening links (no facing neighbour), so
        // both are red unlinked dead-ends and no portal exists.
        var grid = SectorInterop.GetSectorGridById("cave");
        Assertions.AssertThat(grid).IsNotNull();
        Assertions.AssertThat(grid!.Cells.Count).IsEqual(2);
        Assertions.AssertThat(grid.Portals.Count).IsEqual(0);

        scene.AssertPanelThat("caveview")
            .ViewportIsSize(700, 400)
            .HasChildPanelNamed("caveview-cell-0-0", c => c.IsPositionEqual(4, 4).ViewportIsSize(32, 32))
            .HasChildPanelNamed("caveview-cell-2-0", c => c.IsPositionEqual(84, 4).ViewportIsSize(32, 32));

        // A's east dead-end is a shaft on x = (0+1)*40; B's west dead-end on x = 2*40.
        var u0 = scene.GetWindowOrNull("caveview-unlinked-0");
        Assertions.AssertThat(u0).IsNotNull();
        Assertions.AssertThat(u0!.Position == new Vector2(37, 0) && u0.Size == new Vector2(6, 40)).IsTrue();
        var u1 = scene.GetWindowOrNull("caveview-unlinked-1");
        Assertions.AssertThat(u1).IsNotNull();
        Assertions.AssertThat(u1!.Position == new Vector2(77, 0) && u1.Size == new Vector2(6, 40)).IsTrue();
        Assertions.AssertThat(scene.GetWindowOrNull("caveview-portal-0")).IsNull();
        DebugSaveScreenshot("debug_initial.png");

        // ---- Fire the action: action -> effect -> linkOpening(A.east, B.west). ---
        RuntimeInterop.emitAction("link-ab");
        RuntimeInterop.RunIteration(0);
        await runner.SimulateFrames(2);

        // ---- After: a portal links the two openings across the gap. ----------
        // The grid still has two cells but now exactly one portal, connecting A's
        // east opening (0,0) to B's west opening (2,0) even though a blank square
        // sits between them. Both dead-ends are now linked (no unlinked markers).
        grid = SectorInterop.GetSectorGridById("cave");
        Assertions.AssertThat(grid).IsNotNull();
        Assertions.AssertThat(grid!.Cells.Count).IsEqual(2);
        Assertions.AssertThat(grid.Portals.Count).IsEqual(1);
        var p = grid.Portals[0];
        Assertions.AssertThat(p.A.CellX == 0 && p.A.CellY == 0 && p.A.Side == "E").IsTrue();
        Assertions.AssertThat(p.B.CellX == 2 && p.B.CellY == 0 && p.B.Side == "W").IsTrue();

        scene.AssertPanelThat("caveview")
            .HasChildPanelNamed("caveview-cell-0-0", c => c.IsPositionEqual(4, 4).ViewportIsSize(32, 32))
            .HasChildPanelNamed("caveview-cell-2-0", c => c.IsPositionEqual(84, 4).ViewportIsSize(32, 32));

        // The portal line stretches across the blank square from A's east edge
        // (x = 40) to B's west edge (x = 80), centered on the opening (y = 20):
        // a single 40x6 line, no second shaft.
        var line = scene.GetWindowOrNull("caveview-portal-0");
        Assertions.AssertThat(line).IsNotNull();
        Assertions.AssertThat(line!.Position == new Vector2(40, 17) && line.Size == new Vector2(40, 6)).IsTrue();
        Assertions.AssertThat(scene.GetWindowOrNull("caveview-portal-0-b")).IsNull();

        // Both openings are linked now, so the unlinked dead-ends are gone.
        Assertions.AssertThat(scene.GetWindowOrNull("caveview-unlinked-0")).IsNull();
        Assertions.AssertThat(scene.GetWindowOrNull("caveview-unlinked-1")).IsNull();

        DebugSaveScreenshot("debug.png");
    }
}
