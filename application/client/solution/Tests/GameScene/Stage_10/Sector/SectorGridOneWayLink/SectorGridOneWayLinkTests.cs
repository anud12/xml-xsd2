using Godot;
using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;
using Vector2 = Godot.Vector2;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_10.Sector.SectorGridOneWayLink;

[TestSuite]
public class SectorGridOneWayLinkTests : Steps
{
    [TestCategory("Stage_10")]
    [TestCase]
    [RequireGodotRuntime]
    public async Task One_way_opening_towards_A_does_not_link()
    {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .AddFileToArchive("module/viewbg.png", "viewbg.png")
            .AddFileToArchive("module/cell.png", "cell.png")
            .EnsureDllAccessible()
            .ProcessArchive();

        var scene = await AttachUiScene();

        // ---- Initial state: only A exists (1x1 at (0,0), no openings). ----
        var grid = SectorInterop.GetSectorGridById("cave");
        Assertions.AssertThat(grid).IsNotNull();
        Assertions.AssertThat(grid!.Cells.Count).IsEqual(1);
        Assertions.AssertThat(grid.Portals.Count).IsEqual(0);

        scene.AssertPanelThat("caveview")
            .ViewportIsSize(700, 400)
            .HasChildPanelNamed("caveview-cell-0-0", c => c.IsPositionEqual(4, 4).ViewportIsSize(32, 32));

        Assertions.AssertThat(scene.GetWindowOrNull("caveview-portal-0")).IsNull();
        Assertions.AssertThat(scene.GetWindowOrNull("caveview-unlinked-0")).IsNull();
        DebugSaveScreenshot("debug_initial.png");

        // ---- Fire the action: action -> effect -> setContainer(room-b). ---
        // room-b appears at (1,0), east of A, with a WEST opening facing A.
        RuntimeInterop.emitAction("build-b");
        RuntimeInterop.RunIteration(0);
        await runner.SimulateFrames(2);

        // ---- After: B exists and its opening is UNLINKED. -----------------
        // A has no opening facing B, so no portal forms even though B opens
        // toward A. The grid has both cells but zero portals, and B's west
        // opening is an unlinked red shaft on the shared A-B wall.
        grid = SectorInterop.GetSectorGridById("cave");
        Assertions.AssertThat(grid).IsNotNull();
        Assertions.AssertThat(grid!.Cells.Count).IsEqual(2);
        Assertions.AssertThat(grid.Portals.Count).IsEqual(0);

        // B's cell appeared at (1,0) (no same-container neighbours -> a 4px
        // inset on every side); A's cell is unchanged.
        scene.AssertPanelThat("caveview")
            .HasChildPanelNamed("caveview-cell-0-0", c => c.IsPositionEqual(4, 4).ViewportIsSize(32, 32))
            .HasChildPanelNamed("caveview-cell-1-0", c => c.IsPositionEqual(44, 4).ViewportIsSize(32, 32));

        // B's west opening is a headless (unlinked) shaft on the boundary
        // between (0,0) and (1,0): centered on x = 1*40, spanning the full
        // cell height.
        var unlinked = scene.GetWindowOrNull("caveview-unlinked-0");
        Assertions.AssertThat(unlinked).IsNotNull();
        Assertions.AssertThat(unlinked!.Position == new Vector2(37, 0) && unlinked.Size == new Vector2(6, 40)).IsTrue();

        // No portal formed: A has no opening facing B.
        Assertions.AssertThat(scene.GetWindowOrNull("caveview-portal-0")).IsNull();

        DebugSaveScreenshot("debug.png");
    }
}
