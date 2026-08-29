using GdUnit4.Examples.Basics.Setup.Sources.UI;
using Godot;
using NewGameProject.Tests.XUnit;
using Vector2 = Godot.Vector2;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_0.Positioning;

[TestSuite]
public class TestClass : Steps {
    [TestCategory("Step_0")]
    [TestCase]
    [RequireGodotRuntime]
    public async Task IsPositionEqual_it_should_match_explicit_xy_and_anchor_positions() {
        try {
            AddFileToArchive("module/index.js", "index.js")
                .AddFileToArchive("module/manifest.json", "manifest.json")
                .EnsureDllAccessible()
                .ProcessArchive();

            var scene = await AttachUiScene();

            // No x/y: the window's top-left sits at the anchor point of the
            // parent rect (default anchor center).
            scene.AssertPanelThat("base")
                .IsPositionEqual(500, 500);
            // With x/y: top-left coordinates in parent space.
            scene.AssertPanelThat("offset")
                .IsPositionEqual(20, 30);
            scene.AssertPanelThat("tl")
                .IsPositionEqual(0, 0);
            scene.AssertPanelThat("br")
                .IsPositionEqual(1000, 1000);
            scene.AssertPanelThat("bl")
                .IsPositionEqual(0, 1000);
            scene.AssertPanelThat("tr")
                .IsPositionEqual(1000, 0);

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
    public async Task IsPositionEqual_it_should_fail_when_position_is_wrong() {
        try {
            AddFileToArchive("module/index.js", "index.js")
                .AddFileToArchive("module/manifest.json", "manifest.json")
                .EnsureDllAccessible()
                .ProcessArchive();

            var scene = await AttachUiScene();

            // "base" is at (500, 500); asserting (100, 100) must fail.
            bool threw = false;
            try {
                scene.AssertPanelThat("base")
                    .IsPositionEqual(100, 100);
            }
            catch (Exception) {
                threw = true;
            }
            Assertions.AssertThat(threw)
                .OverrideFailureMessage(
                    "IsPositionEqual should have failed for a wrong position")
                .IsTrue();
        }
        catch (Exception e) {
            Assertions.AssertThat(true)
                .OverrideFailureMessage($"Error: {e.Message}\n{e.StackTrace}")
                .IsFalse();
        }
    }
}
