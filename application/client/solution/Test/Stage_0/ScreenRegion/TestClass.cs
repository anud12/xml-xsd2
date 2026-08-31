using GdUnit4.Examples.Basics.Setup.Sources.UI;
using Godot;
using NewGameProject.Tests.XUnit;
using Vector2 = Godot.Vector2;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_0.ScreenRegion;

[TestSuite]
public class TestClass : Steps {
    [TestCategory("Step_0")]
    [TestCase]
    [RequireGodotRuntime]
    public async Task ScreenRegionMatches_it_should_match_screen_subrectangles_against_reference() {
        try {
            AddFileToArchive("module/index.js", "index.js")
                .AddFileToArchive("module/manifest.json", "manifest.json")
                .AddFileToArchive("module/texture.png", "texture.png")
                .EnsureDllAccessible()
                .ProcessArchive();

            var scene = await AttachUiScene();

            // The full panel region (100x100 at the viewport center).
            scene.AssertPanelThat("panel")
                .ScreenRegionMatches(new Rect2I(500, 500, 100, 100), "expected-region-panel.png");
            // The overlay occupies the panel's top-left 50x50 corner; that
            // sub-rect must match the overlay's rendered background.
            scene.AssertPanelThat("panel")
                .ScreenRegionMatches(new Rect2I(500, 500, 50, 50), "expected-region-overlay.png");

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
    public async Task ScreenRegionMatches_it_should_fail_against_a_different_reference() {
        try {
            AddFileToArchive("module/index.js", "index.js")
                .AddFileToArchive("module/manifest.json", "manifest.json")
                .AddFileToArchive("module/texture.png", "texture.png")
                .EnsureDllAccessible()
                .ProcessArchive();

            var scene = await AttachUiScene();

            // The 50x50 overlay reference must not match the full 100x100
            // region (dimension mismatch), and the inverted reference must
            // not match the 50x50 region (pixel mismatch).
            bool dimMismatchThrew = false;
            try {
                scene.AssertPanelThat("panel")
                    .ScreenRegionMatches(new Rect2I(500, 500, 100, 100), "expected-region-overlay.png");
            }
            catch (Exception) {
                dimMismatchThrew = true;
            }
            Assertions.AssertThat(dimMismatchThrew)
                .OverrideFailureMessage(
                    "ScreenRegionMatches should have failed when the reference dimensions differ")
                .IsTrue();

            bool pixelMismatchThrew = false;
            try {
                scene.AssertPanelThat("panel")
                    .ScreenRegionMatches(new Rect2I(500, 500, 50, 50), "expected-region-wrong.png");
            }
            catch (Exception) {
                pixelMismatchThrew = true;
            }
            Assertions.AssertThat(pixelMismatchThrew)
                .OverrideFailureMessage(
                    "ScreenRegionMatches should have failed against a different reference image")
                .IsTrue();
        }
        catch (Exception e) {
            Assertions.AssertThat(true)
                .OverrideFailureMessage($"Error: {e.Message}\n{e.StackTrace}")
                .IsFalse();
        }
    }
}
