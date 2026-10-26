using NewGameProject.Tests.XUnit;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_0.Background.PaintMode;

[TestSuite]
public class TestClass : Steps {
    [TestCategory("Step_0")]
    [TestCase]
    [RequireGodotRuntime]
    public async Task Given_panel_with_explicit_paint_it_should_resolve_and_render_each_mode() {
        try {
            AddFileToArchive("module/index.js", "index.js")
                .AddFileToArchive("module/manifest.json", "manifest.json")
                .AddFileToArchive("module/background.png", "background.png")
                .AddFileToArchive("module/border.png", "border.png")
                .EnsureDllAccessible()
                .ProcessArchive();

            var scene = await AttachUiScene();

            // The explicit per-texture fills flow through the whole pipeline
            // (module -> host.js -> runtime ABI -> slab -> reader) and override
            // the slot defaults:
            //   background  fill stretch          (default: cover / KeepAspect)
            //   border      fill stretch + 9s 3px (default: 9-slice frame)
            //   hover       fill stretch + 9s 2px (default: 9-slice frame)
            scene.AssertPanelThat("painted")
                .HasBackgroundTexture()
                .BackgroundPaint(
                    tileWidth: false, tileHeight: false,
                    keepAspect: false,
                    sliceWidth: 0, sliceHeight: 0)
                .BorderPaint(
                    tileWidth: false, tileHeight: false,
                    keepAspect: false,
                    sliceWidth: 3, sliceHeight: 3)
                .HoverPaint(
                    tileWidth: false, tileHeight: false,
                    keepAspect: false,
                    sliceWidth: 2, sliceHeight: 2)
                .ReferenceBackground("module/expected-stretch.png", 120, 80);

            DebugSaveScreenshot("result.png");
        }
        catch (Exception e) {
            Assertions.AssertThat(true)
                .OverrideFailureMessage($"Error: {e.Message}\n{e.StackTrace}")
                .IsFalse();
        }
    }
}
