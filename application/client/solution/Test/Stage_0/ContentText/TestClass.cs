using GdUnit4.Examples.Basics.Setup.Sources.UI;
using Godot;
using NewGameProject.Tests.XUnit;
using Vector2 = Godot.Vector2;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_0.ContentText;

[TestSuite]
public class TestClass : Steps {
    [TestCategory("Step_0")]
    [TestCase]
    [RequireGodotRuntime]
    public async Task HasContentText_it_should_match_text_nodes_and_resolved_field_values() {
        try {
            AddFileToArchive("module/index.js", "index.js")
                .AddFileToArchive("module/manifest.json", "manifest.json")
                .EnsureDllAccessible()
                .ProcessArchive();

            var scene = await AttachUiScene();

            scene.AssertPanelThat("text-host")
                .HasContentText("hello");
            scene.AssertPanelThat("field-host")
                .HasContentText("textValue");
            scene.AssertPanelThat("number-host")
                .HasContentText("42");

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
    public async Task HasContentText_it_should_fail_when_text_is_wrong() {
        try {
            AddFileToArchive("module/index.js", "index.js")
                .AddFileToArchive("module/manifest.json", "manifest.json")
                .EnsureDllAccessible()
                .ProcessArchive();

            var scene = await AttachUiScene();

            // "text-host" contains "hello"; asserting "goodbye" must fail.
            bool threw = false;
            try {
                scene.AssertPanelThat("text-host")
                    .HasContentText("goodbye");
            }
            catch (Exception) {
                threw = true;
            }
            Assertions.AssertThat(threw)
                .OverrideFailureMessage(
                    "HasContentText should have failed for a wrong text value")
                .IsTrue();
        }
        catch (Exception e) {
            Assertions.AssertThat(true)
                .OverrideFailureMessage($"Error: {e.Message}\n{e.StackTrace}")
                .IsFalse();
        }
    }
}
