using GdUnit4.Examples.Basics.Setup.Sources.UI;
using Godot;
using NewGameProject.Tests.XUnit;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_0.Font.SizeChange;

[TestSuite]
public class TestClass : Steps
{
    [TestCategory("Step_0")]
    [TestCase]
    [RequireGodotRuntime]
    public async Task FontSize_small_applies_correct_size()
    {
        try
        {
            AddFileToArchive("module/index.js", "index.js")
                .AddFileToArchive("module/manifest.json", "manifest.json")
                .AddFileToArchive("module/fonts/Ubuntu-Regular.ttf", "fonts/Ubuntu-Regular.ttf")
                .EnsureDllAccessible()
                .ProcessArchive();

            var scene = await AttachUiScene();

            var assertions = scene.AssertPanelThat("small-window")
                .IsNonNull()
                .HasContentText("Small")
                .ViewportIsSize(200, 50)
                .ViewportMatches("small-text.png");

            var win = scene.Window("small-window");
            var label = win.GetNode<Label>("text");
            var fontSize = label.GetThemeFontSize("font_size");
            Assertions.AssertInt(fontSize)
                .OverrideFailureMessage($"Expected font_size 12, got {fontSize}")
                .IsEqual(12);
        }
        catch (Exception e)
        {
            Assertions.AssertThat(true)
                .OverrideFailureMessage($"Error: {e.Message}\n{e.StackTrace}")
                .IsFalse();
        }
    }

    [TestCategory("Step_0")]
    [TestCase]
    [RequireGodotRuntime]
    public async Task FontSize_large_applies_correct_size()
    {
        try
        {
            AddFileToArchive("module/index.js", "index.js")
                .AddFileToArchive("module/manifest.json", "manifest.json")
                .AddFileToArchive("module/fonts/Ubuntu-Regular.ttf", "fonts/Ubuntu-Regular.ttf")
                .EnsureDllAccessible()
                .ProcessArchive();

            var scene = await AttachUiScene();

            var assertions = scene.AssertPanelThat("large-window")
                .IsNonNull()
                .HasContentText("Large")
                .ViewportIsSize(200, 60)
                .ViewportMatches("large-text.png");

            var win = scene.Window("large-window");
            var label = win.GetNode<Label>("text");
            var fontSize = label.GetThemeFontSize("font_size");
            Assertions.AssertInt(fontSize)
                .OverrideFailureMessage($"Expected font_size 32, got {fontSize}")
                .IsEqual(32);
        }
        catch (Exception e)
        {
            Assertions.AssertThat(true)
                .OverrideFailureMessage($"Error: {e.Message}\n{e.StackTrace}")
                .IsFalse();
        }
    }
}
