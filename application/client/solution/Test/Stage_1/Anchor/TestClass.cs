using GdUnit4.Examples.Basics.Setup.Sources.UI;
using Godot;
using NewGameProject.Tests.XUnit;
using Vector2 = Godot.Vector2;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_1.Anchor;

[TestSuite]
public partial class Anchor : Steps {
    [TestCategory("Step_1")]
    [TestCase]
    [RequireGodotRuntime]
    public async Task Given_panel_it_should_apply_anchors() {
        try {
            CleanupArchive();
            AddFileToArchive("module/index.js", "index.js")
                .AddFileToArchive("module/manifest.json", "manifest.json")
                .AddFileToArchive("module/texture.png", "texture.png")
                .EnsureDllAccessible()
                .ProcessArchive();

            var scene = await AttachUiScene();

            // No x/y: the window's top-left sits at the parent's center
            // (default anchor), so a 100x100 window in a 1000x1000 viewport
            // lands at (500, 500).
            scene.AssertPanelThat("center")
                .IsPositionEqual(500, 500)
                .ViewportIsSize(100, 100);
        }
        catch (Exception e) {
            Assertions.AssertThat(true)
                .OverrideFailureMessage($"Error: {e.Message}\n{e.StackTrace}")
                .IsFalse();
        }
    }
}
