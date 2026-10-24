using GdUnit4.Examples.Basics.Setup.Sources.UI;
using Godot;
using NewGameProject.Tests.XUnit;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_0.Font;

[TestSuite]
public class TestClass : Steps {
    [TestCategory("Step_0")]
    [TestCase]
    [RequireGodotRuntime]
    public async Task FontText_applies_font_size_and_color() {
        try {
            AddFileToArchive("module/index.js", "index.js")
                .AddFileToArchive("module/manifest.json", "manifest.json")
                .AddFileToArchive("module/fonts/Ubuntu-Regular.ttf", "fonts/Ubuntu-Regular.ttf")
                .EnsureDllAccessible()
                .ProcessArchive();

            var scene = await AttachUiScene();

            var panel = scene.AssertPanelThat("font-window");
            panel.IsNonNull().HasContentText("Hello");

            var win = scene.Window("font-window");
            var label = win.GetNode<Label>("text");
            var fontSize = label.GetThemeFontSize("font_size");
            Assertions.AssertInt(fontSize)
                .OverrideFailureMessage($"Expected font_size 24, got {fontSize}")
                .IsEqual(24);
            var color = label.GetThemeColor("font_color");
            Assertions.AssertThat(color)
                .OverrideFailureMessage($"Expected font_color (1,0,0,1), got {color}")
                .IsEqual(new Color(1f, 0f, 0f, 1f));
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
    public async Task DefaultFont_applies_size_16_white() {
        try {
            AddFileToArchive("module/index.js", "index.js")
                .AddFileToArchive("module/manifest.json", "manifest.json")
                .AddFileToArchive("module/fonts/Ubuntu-Regular.ttf", "fonts/Ubuntu-Regular.ttf")
                .EnsureDllAccessible()
                .ProcessArchive();

            var scene = await AttachUiScene();

            var panel = scene.AssertPanelThat("plain-window");
            panel.IsNonNull().HasContentText("World");

            var win = scene.Window("plain-window");
            var label = win.GetNode<Label>("text");
            var fontSize = label.GetThemeFontSize("font_size");
            Assertions.AssertInt(fontSize)
                .OverrideFailureMessage($"Expected font_size 16, got {fontSize}")
                .IsEqual(16);
            var color = label.GetThemeColor("font_color");
            Assertions.AssertThat(color)
                .OverrideFailureMessage($"Expected font_color (1,1,1,1), got {color}")
                .IsEqual(new Color(1f, 1f, 1f, 1f));
        }
        catch (Exception e) {
            Assertions.AssertThat(true)
                .OverrideFailureMessage($"Error: {e.Message}\n{e.StackTrace}")
                .IsFalse();
        }
    }
}
