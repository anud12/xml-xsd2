using Godot;
using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;
using Vector2 = Godot.Vector2;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_10.Sector.SectorGridDynamic;

[TestSuite]
public class SectorGridDynamicTests : Steps
{
    [TestCategory("Stage_10")]
    [TestCase]
    [RequireGodotRuntime]
    public async Task New_sector_created_by_effect_links_and_updates_view()
    {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .AddFileToArchive("module/viewbg.png", "viewbg.png")
            .AddFileToArchive("module/cell.png", "cell.png")
            .EnsureDllAccessible()
            .ProcessArchive();

        var scene = await AttachUiScene();

        // ---- Initial state: only the L sector exists. --------------------
        // L occupies (0,0),(1,0),(0,1). Its (1,0) arm declares a south opening
        // facing the (empty) square (1,1); with no neighbour there it is an
        // unlinked dead-end (a red shaft), and there are no portals.
        var grid = SectorInterop.GetSectorGridById("cave");
        Assertions.AssertThat(grid).IsNotNull();
        Assertions.AssertThat(grid!.Cells.Count).IsEqual(3);
        Assertions.AssertThat(grid.Portals.Count).IsEqual(0);

        scene.AssertPanelThat("caveview")
            .ViewportIsSize(700, 400)
            .HasChildPanelNamed("caveview-cell-0-0", c => c.IsPositionEqual(4, 4).ViewportIsSize(36, 36))
            .HasChildPanelNamed("caveview-cell-1-0", c => c.IsPositionEqual(40, 4).ViewportIsSize(36, 32))
            .HasChildPanelNamed("caveview-cell-0-1", c => c.IsPositionEqual(4, 40).ViewportIsSize(32, 36));

        // The L's unlinked doorway on the (1,0) south edge: a headless shaft on
        // the boundary between (1,0) and (1,1). No linked portal yet.
        var unlinked = scene.GetWindowOrNull("caveview-unlinked-0");
        Assertions.AssertThat(unlinked).IsNotNull();
        Assertions.AssertThat(unlinked!.Position == new Vector2(40, 37) && unlinked.Size == new Vector2(40, 6)).IsTrue();
        Assertions.AssertThat(scene.GetWindowOrNull("caveview-portal-0")).IsNull();
        DebugSaveScreenshot("debug_initial.png");

        // ---- Fire the action: action -> effect -> setContainer(room-b). ---
        RuntimeInterop.emitAction("build-room");
        RuntimeInterop.RunIteration(0);
        await runner.SimulateFrames(2);

        // ---- After: a new sector exists at (1,1) and links to the L. ------
        // The grid now has the 3 L cells plus room-b's cell, and exactly one
        // portal: L(1,0) S <-> room-b(1,1) N (vertical adjacency -> horizontal
        // shaft). A/B order is an implementation detail, so match as a set.
        grid = SectorInterop.GetSectorGridById("cave");
        Assertions.AssertThat(grid).IsNotNull();
        Assertions.AssertThat(grid!.Cells.Count).IsEqual(4);
        Assertions.AssertThat(grid.Portals.Count).IsEqual(1);
        bool IsLink(Portal p)
        {
            bool nS = p.A.CellX == 1 && p.A.CellY == 1 && p.A.Side == "N"
                   && p.B.CellX == 1 && p.B.CellY == 0 && p.B.Side == "S";
            bool sN = p.A.CellX == 1 && p.A.CellY == 0 && p.A.Side == "S"
                   && p.B.CellX == 1 && p.B.CellY == 1 && p.B.Side == "N";
            return nS || sN;
        }
        Assertions.AssertThat(IsLink(grid.Portals[0])).IsTrue();

        // The view re-rendered: the new room-b cell appeared at (1,1) (no
        // same-container neighbours -> a 4px inset on every side), the L's cells
        // are unchanged, the previously-unlinked doorway is gone, and a linked
        // portal line now spans edge-to-edge from (1,0)'s visible south edge
        // (y = 40 - 4 inset = 36) to (1,1)'s visible north edge (y = 44): a 6x8
        // vertical line touching both cells.
        scene.AssertPanelThat("caveview")
            .HasChildPanelNamed("caveview-cell-1-1", c => c.IsPositionEqual(44, 44).ViewportIsSize(32, 32))
            .HasChildPanelNamed("caveview-cell-1-0", c => c.IsPositionEqual(40, 4).ViewportIsSize(36, 32));

        var portal = scene.GetWindowOrNull("caveview-portal-0");
        Assertions.AssertThat(portal).IsNotNull();
        Assertions.AssertThat(portal!.Position == new Vector2(57, 36) && portal.Size == new Vector2(6, 8)).IsTrue();
        Assertions.AssertThat(scene.GetWindowOrNull("caveview-unlinked-0")).IsNull();

        // Reference screenshot of the whole view after the build: the three L
        // cells, the new inset room-b cell at (1,1), and the orange edge-to-edge
        // portal line linking them (the unlinked dead-end is gone). Generated on
        // first run, compared after.
        scene.AssertPanelThat("caveview").ViewportMatches("expected.png");

        DebugSaveScreenshot("debug.png");
    }
}
