using Godot;
using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;
using Vector2 = Godot.Vector2;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_10.Sector.SectorGridUnlinked;

[TestSuite]
public class SectorGridUnlinkedTests : Steps
{
    [TestCategory("Stage_10")]
    [TestCase]
    [RequireGodotRuntime]
    public async Task Single_sector_renders_its_unlinked_openings()
    {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .AddFileToArchive("module/viewbg.png", "viewbg.png")
            .AddFileToArchive("module/cell.png", "cell.png")
            .EnsureDllAccessible()
            .ProcessArchive();

        var scene = await AttachUiScene();

        // One single-cell sector at [1,1]. With no neighbouring sector, none of
        // its four openings can link, so the grid has no portals and every
        // opening is an unlinked dead-end.
        var grid = SectorInterop.GetSectorGridById("cave");
        Assertions.AssertThat(grid).IsNotNull();
        Assertions.AssertThat(grid!.Cells.Count).IsEqual(1);
        Assertions.AssertThat(grid.Portals.Count).IsEqual(0);

        // The lone cell has no same-container neighbours, so it keeps a 4px
        // inset on every side (local 44..76, size 32x32).
        scene.AssertPanelThat("caveview")
            .ViewportIsSize(700, 400)
            .HasChildPanelNamed("caveview-cell-1-1", c => c.IsPositionEqual(44, 44).ViewportIsSize(32, 32));

        // Four unlinked dead-end shafts, one per boundary edge, centered on the
        // cell's boundary edges:
        //   N/S -> horizontal shafts 40x6 at y=37 / y=77
        //   W/E -> vertical shafts   6x40 at x=37 / x=77
        // Node id order is an implementation detail, so match as a set.
        var nodes = new[]
        {
            scene.GetWindowOrNull("caveview-unlinked-0"),
            scene.GetWindowOrNull("caveview-unlinked-1"),
            scene.GetWindowOrNull("caveview-unlinked-2"),
            scene.GetWindowOrNull("caveview-unlinked-3"),
        };
        bool allExist = true;
        foreach (var n in nodes) if (n == null) allExist = false;
        Assertions.AssertThat(allExist).IsTrue();

        var expected = new (Vector2 pos, Vector2 size)[]
        {
            (new Vector2(40, 37), new Vector2(40, 6)),
            (new Vector2(40, 77), new Vector2(40, 6)),
            (new Vector2(37, 40), new Vector2(6, 40)),
            (new Vector2(77, 40), new Vector2(6, 40)),
        };
        int matches = 0;
        foreach (var n in nodes)
        {
            foreach (var e in expected)
            {
                if (n!.Position == e.pos && n!.Size == e.size) matches++;
            }
        }
        Assertions.AssertThat(matches).IsEqual(4);

        // Reference screenshot of the whole view: the dark background, the lone
        // inset cell, and the four red unlinked dead-end shafts on its boundary
        // edges (no portals). Generated on first run, compared after.
        scene.AssertPanelThat("caveview").ViewportMatches("expected.png");

        DebugSaveScreenshot("debug.png");
    }
}
