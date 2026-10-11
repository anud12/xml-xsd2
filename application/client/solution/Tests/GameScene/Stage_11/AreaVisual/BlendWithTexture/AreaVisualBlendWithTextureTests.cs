using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;
using NewGameProject.UI;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_11.AreaVisual.BlendWithTexture;

[TestSuite]
public class AreaVisualBlendWithTextureTests : Steps
{
    [TestCategory("Stage_11")]
    [TestCase]
    [RequireGodotRuntime]
    public async Task Given_container_view_item_with_texture_the_outline_should_render_over_the_texture()
    {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .AddFileToArchive("module/item.png", "item.png")
            .EnsureDllAccessible()
            .ProcessArchive();

        var scene = await AttachUiScene();

        // The view is a 700x500 window over a 100x100 container (cell_w = 7,
        // cell_h = 5). The room item is stamped at view-local (70, 50) with a
        // 30x40 span -> its rect spans view-local (70,50)..(280,250). The item
        // paints a solid bright-yellow texture as its background, stretched
        // across that rect; the red outline + translucent green body must
        // render OVER that texture.
        scene.AssertPanelThat("world")
            .ViewportIsSize(700, 500)
            .HasChildPanelNamed("room", c => c.IsPositionEqual(70, 50).ViewportIsSize(210, 200));

        // Full reference screenshot: yellow item texture with a red outline and
        // translucent green body blended over it.
        scene.AssertPanelThat("world").ViewportMatches("expected.png");

        var world = scene.Window("world");
        var full = world.GetViewport().GetTexture().GetImage();
        var gr = world.GetGlobalRect();
        var ox = (int)gr.Position.X;
        var oy = (int)gr.Position.Y;
        Godot.Color At(int vx, int vy) => full.GetPixel(ox + vx, oy + vy);

        // The 10x10 item texture (a 5x5 transparent center over yellow) is
        // stretched to the 210x200 item. Its yellow border maps to the outer
        // ring of the item rect; the transparent center maps to the middle.
        // The area (30x20) covers only the TOP half of the item (view y 50..150).
        // The outline is 50% transparent red, the body 50% transparent green
        // (the C# renderer further scales the body alpha by 0.35).

        // 1. Opaque yellow texture border inside the area (view (150, 60), just
        //    below the top outline): the translucent green body blends over the
        //    yellow border -> green + red elevated, B low. The red component
        //    proves the yellow texture is present (not the transparent center).
        var borderInArea = At(150, 60);
        Assertions.AssertThat(
            borderInArea.R > 0.2f && borderInArea.G > 0.4f && borderInArea.B < 0.5f
        ).IsTrue();

        // 2. The 50%-transparent red OUTLINE edge (view y=50) sits directly on
        //    the yellow texture. Because the outline is semi-transparent, the
        //    yellow texture bleeds through: the pixel is red-dominant (R > G)
        //    but carries some green from the texture. If the outline rendered
        //    UNDER the texture, R and G would be equal (pure yellow). The R > G
        //    dominance proves the outline is drawn OVER the texture.
        var outlineEdge = At(150, 50);
        Assertions.AssertThat(
            outlineEdge.R > 0.5f && outlineEdge.R > outlineEdge.G
                && outlineEdge.B < 0.3f
        ).IsTrue();

        // 3. The transparent CENTER of the texture inside the area (view
        //    (150, 100)): the texture is transparent here, so only the
        //    translucent green body renders over the dark background -> green
        //    elevated, red LOW (no yellow), B low. This proves the 5x5
        //    transparent center lets the non-texture background show through.
        var transparentCenter = At(150, 100);
        Assertions.AssertThat(
            transparentCenter.G > 0.2f && transparentCenter.R < 0.25f
                && transparentCenter.B < 0.3f
        ).IsTrue();

        // 4. The BOTTOM half of the item (view (150, 200)) is inside the item
        //    but OUTSIDE the area (the area only covers the top half), so it
        //    shows the bare yellow texture: R and G equal (yellow), B low, and
        //    no green body fill. This proves the polygon only partially covers
        //    the texture.
        var bareTexture = At(150, 200);
        Assertions.AssertThat(
            bareTexture.R > 0.3f && bareTexture.G > 0.3f
                && Math.Abs(bareTexture.R - bareTexture.G) < 0.15f
                && bareTexture.B < 0.5f
        ).IsTrue();

        // 5. Just above the item's top edge (view y=40) is outside the item and
        //    outside the area: background, NOT yellow, NOT red, NOT green.
        var aboveItem = At(150, 40);
        Assertions.AssertThat(aboveItem.R < 0.3f && aboveItem.G < 0.3f).IsTrue();

        full.Dispose();
    }
}
