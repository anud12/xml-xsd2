using GdUnit4.Examples.Basics.Setup.Sources.UI;
using Godot;
using NewGameProject.Tests.XUnit;
using Vector2 = Godot.Vector2;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_2.Children;

[TestSuite]
public class Children : Steps {
    [TestCategory("Step_2")]
    [TestCase(Timeout = 1_000)]
    [RequireGodotRuntime]
    [GodotExceptionMonitor]
    public async Task Given_panel_it_should_apply_children() {
        try {
            AddFileToArchive("module/index.js", "index.js")
                .AddFileToArchive("module/manifest.json", "manifest.json")
                .AddFileToArchive("module/texture.png", "texture.png")
                .AddFileToArchive("module/texture_2.png", "texture_2.png")
                .EnsureDllAccessible()
                .ProcessArchive();

            var scene = await AttachUiScene();

            scene.AssertPanelThat("center")
                .IsNonNull()
                .IsPositionEqual(500, 500)
                .HasChildPanelNamed("child", child => child.IsPositionEqual(0, 0))
                .HasChildPanelNamed("child_2", child => child.IsPositionEqual(0, 10));
        }
        catch (Exception e) {
            Assertions.AssertThat(true)
                .OverrideFailureMessage($"Error: {e.Message}\n{e.StackTrace}")
                .IsFalse();
        }
    }
}
