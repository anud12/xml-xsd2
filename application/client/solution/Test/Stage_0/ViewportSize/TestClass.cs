using GdUnit4.Examples.Basics.Setup.Sources.UI;
using Godot;
using NewGameProject.Tests.XUnit;
using Vector2 = Godot.Vector2;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_0.ViewportSize;

[TestSuite]
public class TestClass : Steps {
    [TestCategory("Step_0")]
    [TestCase]
    [RequireGodotRuntime]
    public async Task ViewportIsSize_it_should_match_declared_window_dimensions() {
        try {
            AddFileToArchive("module/index.js", "index.js")
                .AddFileToArchive("module/manifest.json", "manifest.json")
                .EnsureDllAccessible()
                .ProcessArchive();

            var scene = await AttachUiScene();

            scene.AssertPanelThat("small")
                .ViewportIsSize(100, 100);
            scene.AssertPanelThat("wide")
                .ViewportIsSize(200, 50);
            scene.AssertPanelThat("tall")
                .ViewportIsSize(50, 200);

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
    public async Task ViewportIsSize_it_should_fail_when_size_is_wrong() {
        try {
            AddFileToArchive("module/index.js", "index.js")
                .AddFileToArchive("module/manifest.json", "manifest.json")
                .EnsureDllAccessible()
                .ProcessArchive();

            var scene = await AttachUiScene();

            // "small" is 100x100; asserting 150x150 must fail.
            bool threw = false;
            try {
                scene.AssertPanelThat("small")
                    .ViewportIsSize(150, 150);
            }
            catch (Exception) {
                threw = true;
            }
            Assertions.AssertThat(threw)
                .OverrideFailureMessage(
                    "ViewportIsSize should have failed for a wrong size")
                .IsTrue();
        }
        catch (Exception e) {
            Assertions.AssertThat(true)
                .OverrideFailureMessage($"Error: {e.Message}\n{e.StackTrace}")
                .IsFalse();
        }
    }
}
