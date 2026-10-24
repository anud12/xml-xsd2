using GdUnit4.Examples.Basics.Setup.Sources.UI;
using Godot;
using NewGameProject.Tests.XUnit;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_0.Font.ColorChange;

[TestSuite]
public class TestClass : Steps
{
    [TestCategory("Step_0")]
    [TestCase]
    [RequireGodotRuntime]
    public async Task FontColor_green_applies_correct_color()
    {
        try
        {
            AddFileToArchive("module/index.js", "index.js")
                .AddFileToArchive("module/manifest.json", "manifest.json")
                .AddFileToArchive("module/fonts/Ubuntu-Regular.ttf", "fonts/Ubuntu-Regular.ttf")
                .EnsureDllAccessible()
                .ProcessArchive();

            var scene = await AttachUiScene();

            var assertions = scene.AssertPanelThat("green-window")
                .IsNonNull()
                .HasContentText("Green")
                .ViewportIsSize(200, 50)
                .ViewportMatches("green-text.png");

            var win = scene.Window("green-window");
            var label = win.GetNode<Label>("text");
            var color = label.GetThemeColor("font_color");
            Assertions.AssertThat(color)
                .OverrideFailureMessage($"Expected green (0,1,0,1), got {color}")
                .IsEqual(new Color(0f, 1f, 0f, 1f));
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
    public async Task FontColor_blue_applies_correct_color()
    {
        try
        {
            AddFileToArchive("module/index.js", "index.js")
                .AddFileToArchive("module/manifest.json", "manifest.json")
                .AddFileToArchive("module/fonts/Ubuntu-Regular.ttf", "fonts/Ubuntu-Regular.ttf")
                .EnsureDllAccessible()
                .ProcessArchive();

            var scene = await AttachUiScene();

            var assertions = scene.AssertPanelThat("blue-window")
                .IsNonNull()
                .HasContentText("Blue")
                .ViewportIsSize(200, 50)
                .ViewportMatches("blue-text.png");

            var win = scene.Window("blue-window");
            var label = win.GetNode<Label>("text");
            var color = label.GetThemeColor("font_color");
            Assertions.AssertThat(color)
                .OverrideFailureMessage($"Expected blue (0,0,1,1), got {color}")
                .IsEqual(new Color(0f, 0f, 1f, 1f));
        }
        catch (Exception e)
        {
            Assertions.AssertThat(true)
                .OverrideFailureMessage($"Error: {e.Message}\n{e.StackTrace}")
                .IsFalse();
        }
    }
}
