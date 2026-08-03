using GdUnit4.Examples.Basics.Setup.Sources.UI;
using Godot;
using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;
using Vector2 = Godot.Vector2;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_2.SpriteMap.Validation.MissingFile;

[TestSuite]
public partial class TestClass : Steps {
    [TestCategory("Step_2")]
    [TestCase]
    [RequireGodotRuntime]
    public async Task Given_spriteMapTIFF_file_missing_it_should_report_human_readable_error() {
        try {
            AddFileToArchive("module/index.js", "index.js")
                .AddFileToArchive("module/manifest.json", "manifest.json")
                .AddFileToArchive("module/texture/texture.png", "texture/texture.png")
                .EnsureDllAccessible()
                .ProcessArchive();

            var scene = LoadTestScene();
            var rootNode = new RootNode();

            scene.AddChild(rootNode);
            rootNode.SetSize(new Vector2() {
                X = 1000,
                Y = 1000
            });
            rootNode.SetAnchorsPreset(Control.LayoutPreset.Center);
            await runner.SimulateFrames(1);

            AssertRuntimeOutputContains("missing.tiff");
        }
        catch (Exception e) {
            Assertions.AssertThat(true)
                .OverrideFailureMessage($"Error: {e.Message}\n{e.StackTrace}")
                .IsFalse();
        }
    }
}
