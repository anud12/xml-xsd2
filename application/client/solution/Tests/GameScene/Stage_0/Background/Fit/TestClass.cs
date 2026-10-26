using NewGameProject.Tests.XUnit;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_0.Background.Fit;

[TestSuite]
public class TestClass : Steps {
    [TestCategory("Step_0")]
    [TestCase]
    [RequireGodotRuntime]
    public async Task Given_panel_with_fit_fill_it_should_resolve_and_render_contain() {
        try {
            AddFileToArchive("module/index.js", "index.js")
                .AddFileToArchive("module/manifest.json", "manifest.json")
                .AddFileToArchive("module/background.png", "background.png")
                .EnsureDllAccessible()
                .ProcessArchive();

            var scene = await AttachUiScene();

            scene.AssertPanelThat("fitted")
                .HasBackgroundTexture()
                .BackgroundPaint(
                    tileWidth: false, tileHeight: false,
                    keepAspect: false,
                    fitWidth: true, fitHeight: true,
                    sliceWidth: 0, sliceHeight: 0);

            DebugSaveScreenshot("result.png");
        }
        catch (Exception e) {
            Assertions.AssertThat(true)
                .OverrideFailureMessage($"Error: {e.Message}\n{e.StackTrace}")
                .IsFalse();
        }
    }
}
