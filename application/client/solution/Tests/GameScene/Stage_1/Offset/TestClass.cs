using GdUnit4.Examples.Basics.Setup.Sources.UI;
using Godot;
using NewGameProject.Tests.XUnit;
using Vector2 = Godot.Vector2;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_1.Offset;

[TestSuite]
public partial class Offset : Steps {
    [TestCategory("Step_1")]
    [TestCase]
    [RequireGodotRuntime]
    public async Task Given_panel_it_should_apply_offset_in_a_diamond_shape() {
        try {
            CleanupArchive();
            AddFileToArchive("module/index.js", "index.js")
                .AddFileToArchive("module/manifest.json", "manifest.json")
                .AddFileToArchive("module/texture.png", "texture.png")
                .EnsureDllAccessible()
                .ProcessArchive();

            var scene = await AttachUiScene();

            scene.AssertPanelThat("top")
                .IsPositionEqual(495, 395);
            scene.AssertPanelThat("left")
                .IsPositionEqual(395, 495);
            scene.AssertPanelThat("bottom")
                .IsPositionEqual(495, 595);
            scene.AssertPanelThat("right")
                .IsPositionEqual(595, 495);
        }
        catch (Exception e) {
            Assertions.AssertThat(true)
                .OverrideFailureMessage($"Error: {e.Message}\n{e.StackTrace}")
                .IsFalse();
        }
    }
}
