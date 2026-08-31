using GdUnit4.Examples.Basics.Setup.Sources.UI;
using Godot;
using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;
using Vector2 = Godot.Vector2;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_2.SpriteMap;

[TestSuite]
public partial class TestClass : Steps {
    [TestCategory("Step_2")]
    [TestCase]
    [RequireGodotRuntime]
    public async Task Given_panel_with_spriteMapTIFF_it_should_compose_from_layer_bindings() {
        try {
            AddFileToArchive("module/index.js", "index.js")
                .AddFileToArchive("module/manifest.json", "manifest.json")
                .AddFileToArchive("module/texture/texture.png", "texture/texture.png")
                .AddFileToArchive("module/maps/idle_frame1.tiff", "maps/idle_frame1.tiff")
                .EnsureDllAccessible()
                .ProcessArchive();

            var scene = await AttachUiScene();

            var panel = scene.Window("characterPanel");
            AssertPanelThat(panel).IsNonNull();
            AssertPanelThat(panel).ViewportMatches("texture/texture.png");
        }
        catch (Exception e) {
            Assertions.AssertThat(true)
                .OverrideFailureMessage($"Error: {e.Message}\n{e.StackTrace}")
                .IsFalse();
        }
    }
}
