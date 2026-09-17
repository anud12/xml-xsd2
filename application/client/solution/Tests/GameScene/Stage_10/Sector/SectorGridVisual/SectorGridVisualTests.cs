using Godot;
using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;
using Vector2 = Godot.Vector2;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_10.Sector.SectorGridVisual;

[TestSuite]
public class SectorGridVisualTests : Steps
{
    // The view (3 columns x 2 rows at 40px cells, 8px gap between cells): each
    // cell is inset 4px on every side, so cell (x, y) sits at local
    // (x*40 + 4, y*40 + 4) within the view.
    static Vector2 CellPos(int x, int y) => new(x * 40f + 4f, y * 40f + 4f);

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

        // The view materialized one cell window per footprint square, each
        // inset 4px (from the 8px cell gap) — the three L cells plus the box.
        scene.AssertPanelThat("caveview")
            .ViewportIsSize(700, 400)
            .HasChildPanelNamed("caveview-cell-0-0", c => c.IsPositionEqual(CellPos(0, 0).X, CellPos(0, 0).Y))
            .HasChildPanelNamed("caveview-cell-1-0", c => c.IsPositionEqual(CellPos(1, 0).X, CellPos(1, 0).Y))
            .HasChildPanelNamed("caveview-cell-0-1", c => c.IsPositionEqual(CellPos(0, 1).X, CellPos(0, 1).Y))
            .HasChildPanelNamed("caveview-cell-2-0", c => c.IsPositionEqual(CellPos(2, 0).X, CellPos(2, 0).Y));

        // The portal renders as an engine-drawn headless arrow (a solid shaft)
        // centered on the shared edge at x=80 (the right edge of cell (1,0)):
        // 80 - 6/2 = 77, one cell tall.
        scene.AssertPanelThat("caveview")
            .HasChildPanelNamed("caveview-portal-0", c => c.IsPositionEqual(77, 0));

        // Reference screenshot of the whole view: the dark background, the three
        // blue L cells + the blue box cell (8px gaps between them), and the
        // orange headless-arrow shaft the engine draws across the gap between
        // the L's arm and the box. Generated on first run, compared after.
        // scene.AssertPanelThat("caveview").ViewportMatches("expected.png");
    }
    
}
