using GdUnit4.Examples.Basics.Setup.Sources.UI;
using Godot;
using NewGameProject.Tests.XUnit;
using Vector2 = Godot.Vector2;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_0.DivLayout;

[TestSuite]
public class TestClass : Steps {
    [TestCategory("Step_0")]
    [TestCase]
    [RequireGodotRuntime]
    public async Task HasChildDivNamed_it_should_assert_orientation_length_and_templates() {
        try {
            AddFileToArchive("module/index.js", "index.js")
                .AddFileToArchive("module/manifest.json", "manifest.json")
                .EnsureDllAccessible()
                .ProcessArchive();

            var scene = await AttachUiScene();

            scene.AssertPanelThat("div-host")
                .HasChildDivNamed("col-div", content => {
                    content.IsVertical();
                    content.HasLength(3);
                    content.HasTemplates(
                        p => p.HasContentText("a"),
                        p => p.HasContentText("b"),
                        p => p.HasContentText("c")
                    );
                });

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
    public async Task HasChildDivNamed_it_should_fail_for_wrong_orientation_length_and_templates() {
        try {
            AddFileToArchive("module/index.js", "index.js")
                .AddFileToArchive("module/manifest.json", "manifest.json")
                .EnsureDllAccessible()
                .ProcessArchive();

            var scene = await AttachUiScene();

            // "row-div" is a row, so IsVertical must fail.
            bool orientThrew = false;
            try {
                scene.AssertPanelThat("div-host").HasChildDivNamed("row-div", c => c.IsVertical());
            }
            catch (Exception) {
                orientThrew = true;
            }
            Assertions.AssertThat(orientThrew)
                .OverrideFailureMessage(
                    "IsVertical should have failed for a row div")
                .IsTrue();

            // "col-div" has 3 children; HasLength(5) must fail.
            bool lengthThrew = false;
            try {
                scene.AssertPanelThat("div-host").HasChildDivNamed("col-div", c => c.HasLength(5));
            }
            catch (Exception) {
                lengthThrew = true;
            }
            Assertions.AssertThat(lengthThrew)
                .OverrideFailureMessage(
                    "HasLength should have failed for a wrong child count")
                .IsTrue();

            // "col-div" children are a/b/c; asserting "z" for the first must fail.
            bool templateThrew = false;
            try {
                scene.AssertPanelThat("div-host").HasChildDivNamed("col-div", c =>
                    c.HasTemplates(
                        p => p.HasContentText("z"),
                        p => p.HasContentText("b"),
                        p => p.HasContentText("c")));
            }
            catch (Exception) {
                templateThrew = true;
            }
            Assertions.AssertThat(templateThrew)
                .OverrideFailureMessage(
                    "HasTemplates should have failed for a wrong template text")
                .IsTrue();
        }
        catch (Exception e) {
            Assertions.AssertThat(true)
                .OverrideFailureMessage($"Error: {e.Message}\n{e.StackTrace}")
                .IsFalse();
        }
    }
}
