using GdUnit4.Examples.Basics.Setup.Sources.UI;
using Godot;
using NewGameProject.Tests.XUnit;
using Vector2 = Godot.Vector2;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_0.Background;

[TestSuite]
public class TestClass : Steps {
    [TestCategory("Step_0")]
    [TestCase]
    [RequireGodotRuntime]
    public async Task BackgroundMatches_it_should_compare_rendered_background_pixels_against_reference() {
        try {
            AddFileToArchive("module/index.js", "index.js")
                .AddFileToArchive("module/manifest.json", "manifest.json")
                .AddFileToArchive("module/texture.png", "texture.png")
                .EnsureDllAccessible()
                .ProcessArchive();

            var scene = await AttachUiScene();

            // The overlay partially covers panel, but the covered window's
            // background is still compared against the reference and passes.
            scene.AssertPanelThat("panel").BackgroundMatches("expected-background.png");
            scene.AssertPanelThat("overlay").BackgroundMatches("expected-background.png");
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
    public async Task BackgroundMatches_it_should_fail_against_a_different_reference() {
        try {
            AddFileToArchive("module/index.js", "index.js")
                .AddFileToArchive("module/manifest.json", "manifest.json")
                .AddFileToArchive("module/texture.png", "texture.png")
                .EnsureDllAccessible()
                .ProcessArchive();

            var scene = await AttachUiScene();

            // The inverted reference must not match the rendered background.
            bool threw = false;
            try {
                scene.AssertPanelThat("panel").BackgroundMatches("expected-background-wrong.png");
            }
            catch (Exception) {
                threw = true;
            }
            Assertions.AssertThat(threw)
                .OverrideFailureMessage(
                    "BackgroundMatches should have failed against a different reference image")
                .IsTrue();
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
    public async Task BackgroundMatches_it_should_fail_for_a_window_without_background() {
        try {
            AddFileToArchive("module/index.js", "index.js")
                .AddFileToArchive("module/manifest.json", "manifest.json")
                .AddFileToArchive("module/texture.png", "texture.png")
                .EnsureDllAccessible()
                .ProcessArchive();

            var scene = await AttachUiScene();

            // "plain" has no background option: no "background" TextureRect
            // is created, so the assertion must fail.
            bool threw = false;
            try {
                scene.AssertPanelThat("plain").BackgroundMatches("expected-background.png");
            }
            catch (Exception) {
                threw = true;
            }
            Assertions.AssertThat(threw)
                .OverrideFailureMessage(
                    "BackgroundMatches should have failed for a window without a background")
                .IsTrue();
        }
        catch (Exception e) {
            Assertions.AssertThat(true)
                .OverrideFailureMessage($"Error: {e.Message}\n{e.StackTrace}")
                .IsFalse();
        }
    }
}
