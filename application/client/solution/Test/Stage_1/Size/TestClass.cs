using GdUnit4.Examples.Basics.Setup.Sources.UI;
using Godot;
using NewGameProject.Tests.XUnit;
using Vector2 = Godot.Vector2;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_1.Size;

[TestSuite]
public partial class TestClass : Steps {
    [TestCategory("Step_1")]
    [TestCase(Timeout = 1_000)]
    [RequireGodotRuntime]
    public async Task Given_panel_it_should_load_the_panel_into_the_scene() {
        try {
            AddFileToArchive("module/index.js", "index.js")
                .AddFileToArchive("module/manifest.json", "manifest.json")
                .AddFileToArchive("module/texture.png", "texture.png")
                .EnsureDllAccessible()
                .ProcessArchive();

            var scene = await AttachUiScene();

            scene.AssertPanelThat("center")
                .IsPositionEqual(-50, -50)
                .ViewportIsSize(200, 200);
        }
        catch (Exception e) {
            Assertions.AssertThat(true)
                .OverrideFailureMessage($"Error: {e.Message}\n{e.StackTrace}")
                .IsFalse();
        }
    }
}
