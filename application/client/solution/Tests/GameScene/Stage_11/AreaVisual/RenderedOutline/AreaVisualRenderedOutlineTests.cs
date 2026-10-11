using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;
using NewGameProject.UI;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_11.AreaVisual.RenderedOutline;

[TestSuite]
public class AreaVisualRenderedOutlineTests : Steps
{
    [TestCategory("Stage_11")]
    [TestCase]
    [RequireGodotRuntime]
    public async Task Given_container_view_the_items_should_position_and_render_the_area_outline()
    {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .EnsureDllAccessible()
            .ProcessArchive();

        var scene = await AttachUiScene();

        // The view is a 700x500 window over a 100x100 container: cell_w = 7,
        // cell_h = 5. Each item is stamped with view-local x/y/width/height,
        // so the rendered controls land at those coordinates (span 1x1 -> 7x5).
        scene.AssertPanelThat("world")
            .ViewportIsSize(700, 500)
            // room's world pos (10,10) -> view-local (70,50); span 1x1 -> 7x5.
            .HasChildPanelNamed("room", c => c.IsPositionEqual(70, 50).ViewportIsSize(7, 5))
            // crate's world pos (40,40) -> view-local (280,200); span 1x1 -> 7x5.
            .HasChildPanelNamed("crate", c => c.IsPositionEqual(280, 200).ViewportIsSize(7, 5));

        // The room's area spans view-local (70,50)..(770,550): a solid red
        // outline plus a translucent green body fill over the whole view; the
        // crate renders its 1x1 fallback at (280,200).
        scene.AssertPanelThat("world").ViewportMatches("expected.png");

        // Assert the area colors actually rendered. The room's area is a 5-point
        // polygon (60x80 box with the top-right corner cut). Sample:
        //  - a point on the flat top edge (view y=50, between [0,0] and [40,0]):
        //    the red outline.
        //  - a point inside the body (view (200,300)): the translucent green fill.
        //  - a point OUTSIDE the cut corner: the diagonal [40,0]->[60,20] maps to
        //    view (350,50)->(490,150); view (450,90) lies above that diagonal
        //    (at x=450 the diagonal is at y≈120), so it is outside the polygon
        //    -> background, NOT green.
        //  - a point inside near the cut corner (view (450,160)): below the
        //    diagonal -> interior green.
        var world = scene.Window("world");
        var full = world.GetViewport().GetTexture().GetImage();
        // Crop origin = the window's global rect position, matching
        // ViewportMatches' crop. Map view-local (vx, vy) to image coords.
        var gr = world.GetGlobalRect();
        var ox = (int)gr.Position.X; var oy = (int)gr.Position.Y;
        Godot.Color At(int vx, int vy) => full.GetPixel(ox + vx, oy + vy);

        // Top flat edge (view y=50), well inside the [0,0]-[40,0] segment.
        var topEdge = At(200, 50);
        Assertions.AssertThat(topEdge.R > 0.6f && topEdge.G < 0.4f && topEdge.B < 0.4f).IsTrue();
        // Interior of the body.
        var body = At(200, 300);
        Assertions.AssertThat(body.G > 0.4f && body.R < 0.4f && body.B < 0.4f).IsTrue();
        // Outside the cut corner: background (not green).
        var outsideCut = At(450, 90);
        Assertions.AssertThat(outsideCut.G < 0.4f).IsTrue();
        // Inside near the cut corner: interior green.
        var insideNearCut = At(450, 160);
        Assertions.AssertThat(insideNearCut.G > 0.4f).IsTrue();
        full.Dispose();
    }
}
