using GdUnit4;
using Godot;
using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;
using Vector2 = Godot.Vector2;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_7.ContainerListView;

[TestSuite]
public partial class TestClass : Steps {
    [TestCategory("Step_7")]
    [TestCase]
    [RequireGodotRuntime]
    public async Task Given_container_list_view_it_renders_three_entity_panels() {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .AddFileToArchive("module/texture.png", "texture.png")
            .AddFileToArchive("module/texture2.png", "texture2.png")
            .EnsureDllAccessible()
            .ProcessArchive();

        var scene = await AttachUiScene();

        scene.AssertPanelThat("list-panel")
            .HasChildDivNamed("items", c =>
                c.IsVertical()
                    .HasLength(3)
                    .HasTemplates(
                        p => p.HasContentText("1").BackgroundMatches("expected-item-background.png"),
                        p => p.HasContentText("2").BackgroundMatches("expected-item-background.png"),
                        p => p.HasContentText("3").BackgroundMatches("expected-item-background.png")
                    ));
        scene.AssertPanelThat("list-panel").ViewportMatches("expected.png");
    }
}
