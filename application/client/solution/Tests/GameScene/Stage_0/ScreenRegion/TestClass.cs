using NewGameProject.Tests.XUnit;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_0.ScreenRegion;

[TestSuite]
public class TestClass : Steps {
    [TestCategory("Step_0")]
    [TestCase]
    [RequireGodotRuntime]
    public async Task ViewportMatches_it_should_match_panel_and_overlay_against_reference() {
        try {
            AddFileToArchive("module/index.js", "index.js")
                .AddFileToArchive("module/manifest.json", "manifest.json")
                .AddFileToArchive("module/texture.png", "texture.png")
                .EnsureDllAccessible()
                .ProcessArchive();

            var scene = await AttachUiScene();

            // The full panel (100x100 at the viewport center).
            scene.AssertPanelThat("panel")
                .ViewportMatches("expected-region-panel.png");
            // The overlay (50x50) occupies the panel's top-left corner.
            scene.AssertPanelThat("overlay")
                .ViewportMatches("expected-region-overlay.png");

            DebugSaveScreenshot("result.png");
        }
        catch (Exception e) {
            Assertions.AssertThat(true)
                .OverrideFailureMessage($"Error: {e.Message}\n{e.StackTrace}")
                .IsFalse();
        }
    }

    [TestCategory("Step_0")]
    [TestCase]
    [RequireGodotRuntime]
    public async Task ViewportMatches_it_should_fail_against_a_different_reference() {
        try {
            AddFileToArchive("module/index.js", "index.js")
                .AddFileToArchive("module/manifest.json", "manifest.json")
                .AddFileToArchive("module/texture.png", "texture.png")
                .EnsureDllAccessible()
                .ProcessArchive();

            var scene = await AttachUiScene();

            // The 50x50 overlay reference must not match the 100x100 panel
            // (dimension mismatch), and the inverted reference must not
            // match the overlay (pixel mismatch).
            bool dimMismatchThrew = false;
            try {
                scene.AssertPanelThat("panel")
                    .ViewportMatches("expected-region-overlay.png");
            }
            catch (Exception) {
                dimMismatchThrew = true;
            }
            Assertions.AssertThat(dimMismatchThrew)
                .OverrideFailureMessage(
                    "ViewportMatches should have failed when the reference dimensions differ")
                .IsTrue();

            bool pixelMismatchThrew = false;
            try {
                scene.AssertPanelThat("overlay")
                    .ViewportMatches("expected-region-wrong.png");
            }
            catch (Exception) {
                pixelMismatchThrew = true;
            }
            Assertions.AssertThat(pixelMismatchThrew)
                .OverrideFailureMessage(
                    "ViewportMatches should have failed against a different reference image")
                .IsTrue();
        }
        catch (Exception e) {
            Assertions.AssertThat(true)
                .OverrideFailureMessage($"Error: {e.Message}\n{e.StackTrace}")
                .IsFalse();
        }
    }
}
