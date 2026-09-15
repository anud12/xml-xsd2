using GdUnit4.Examples.Basics.Setup.Sources.UI;
using Godot;
using NewGameProject.Tests.XUnit;
using Vector2 = Godot.Vector2;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_0.ChildPanels;

[TestSuite]
public class TestClass : Steps {
    [TestCategory("Step_0")]
    [TestCase]
    [RequireGodotRuntime]
    public async Task HasChildPanelNamed_it_should_find_nested_windows_and_invoke_child_assertions() {
        try {
            AddFileToArchive("module/index.js", "index.js")
                .AddFileToArchive("module/manifest.json", "manifest.json")
                .EnsureDllAccessible()
                .ProcessArchive();

            var scene = await AttachUiScene();

            scene.AssertPanelThat("parent")
                .IsPositionEqual(500, 500)
                .HasChildPanelNamed("child", child =>
                    child.IsPositionEqual(10, 10))
                .HasChildPanelNamed("child_2", child =>
                    child.IsPositionEqual(30, 30));

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
    public async Task HasChildPanelNamed_it_should_fail_for_a_missing_child_and_wrong_child_position() {
        try {
            AddFileToArchive("module/index.js", "index.js")
                .AddFileToArchive("module/manifest.json", "manifest.json")
                .EnsureDllAccessible()
                .ProcessArchive();

            var scene = await AttachUiScene();

            // "missing-child" is not declared as a child of "parent".
            bool missingThrew = false;
            try {
                scene.AssertPanelThat("parent").HasChildPanelNamed("missing-child");
            }
            catch (Exception) {
                missingThrew = true;
            }
            Assertions.AssertThat(missingThrew)
                .OverrideFailureMessage(
                    "HasChildPanelNamed should have failed for a missing child")
                .IsTrue();

            // "child" is at (10, 10); asserting (30, 30) must fail.
            bool wrongPosThrew = false;
            try {
                scene.AssertPanelThat("parent").HasChildPanelNamed("child", child =>
                    child.IsPositionEqual(30, 30));
            }
            catch (Exception) {
                wrongPosThrew = true;
            }
            Assertions.AssertThat(wrongPosThrew)
                .OverrideFailureMessage(
                    "HasChildPanelNamed should have failed for a wrong child position")
                .IsTrue();
        }
        catch (Exception e) {
            Assertions.AssertThat(true)
                .OverrideFailureMessage($"Error: {e.Message}\n{e.StackTrace}")
                .IsFalse();
        }
    }
}
