using Godot;
using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_10.Sector.SectorGridVisual;

[TestSuite]
public class SectorGridVisualTests : Steps
{
    [TestCategory("Stage_10")]
    [TestCase]
    [RequireGodotRuntime]
    public async Task Given_an_L_and_a_box_sector_it_should_render_their_cells_and_portal()
    {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .AddFileToArchive("module/viewbg.png", "viewbg.png")
            .AddFileToArchive("module/cell.png", "cell.png")
            .EnsureDllAccessible()
            .ProcessArchive();

        var scene = await AttachUiScene();

        // The computed grid: 4 footprint cells (3 for the L, 1 for the box)
        // and exactly one portal between the L's (1,0) arm and the box.
        var grid = SectorInterop.GetSectorGridById("cave");
        DebugSaveScreenshot("debug.png");
        Assertions.AssertThat(grid).IsNotNull();
        Assertions.AssertThat(grid!.Cells.Count).IsEqual(4);
        Assertions.AssertThat(grid.Portals.Count).IsEqual(1);
        var portal = grid.Portals[0];
        // The portal joins the L's (1,0) arm (side E) to the box at (2,0)
        // (side W); equal length 1, no centering. A/B ordering is an
        // implementation detail, so assert the pair order-independently.
        bool lIsA = portal.A.CellX == 1 && portal.A.CellY == 0 && portal.A.Side == "E"
                 && portal.B.CellX == 2 && portal.B.CellY == 0 && portal.B.Side == "W";
        bool lIsB = portal.B.CellX == 1 && portal.B.CellY == 0 && portal.B.Side == "E"
                 && portal.A.CellX == 2 && portal.A.CellY == 0 && portal.A.Side == "W";
        Assertions.AssertThat(lIsA || lIsB).IsTrue();
        Assertions.AssertThat(portal.A.Length).IsEqual(1);
        Assertions.AssertThat(portal.B.Length).IsEqual(1);

        // The view materialized one cell window per footprint square. The three L
        // cells (same container) are continuous — (0,0) extends right + bottom to
        // touch (1,0) and (0,1) — while the box (a different container) keeps a 4px
        // inset on every side.
        scene.AssertPanelThat("caveview")
            .ViewportIsSize(700, 400)
            .HasChildPanelNamed("caveview-cell-0-0", c => c.IsPositionEqual(4, 4).ViewportIsSize(36, 36))
            .HasChildPanelNamed("caveview-cell-1-0", c => c.IsPositionEqual(40, 4).ViewportIsSize(36, 32))
            .HasChildPanelNamed("caveview-cell-0-1", c => c.IsPositionEqual(4, 40).ViewportIsSize(32, 36))
            .HasChildPanelNamed("caveview-cell-2-0", c => c.IsPositionEqual(84, 4).ViewportIsSize(32, 32));

        // The portal is an edge-to-edge line from the L's (1,0) visible east edge
        // (x = 80 - 4 inset = 76) to the box's (2,0) visible west edge
        // (x = 80 + 4 inset = 84), centered on the opening (y = 20): an 8x6 line
        // that touches both cells with no gap and no inset.
        scene.AssertPanelThat("caveview")
            .HasChildPanelNamed("caveview-portal-0", c => c.IsPositionEqual(76, 17).ViewportIsSize(8, 6));

        // Reference screenshot of the whole view: the dark background, the three
        // blue L cells + the blue box cell (8px gaps between them), and the
        // orange headless-arrow shaft the engine draws across the gap between
        // the L's arm and the box. Generated on first run, compared after.
        scene.AssertPanelThat("caveview").ViewportMatches("expected.png");
    }
    
}
