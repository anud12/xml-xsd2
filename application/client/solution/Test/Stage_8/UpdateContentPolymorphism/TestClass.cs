using GdUnit4.Examples.Basics.Setup.Sources.UI;
using Godot;
using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;
using Vector2 = Godot.Vector2;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_8.UpdateContentPolymorphism;

[TestSuite]
public class TestClass : Steps {
    [TestCategory("Step_8")]
    [TestCase]
    [RequireGodotRuntime]
    public async Task Given_panel_it_should_update_content_polymorphically() {
        // Given: a panel with constant text content "initial"
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .EnsureDllAccessible()
            .ProcessArchive();

        var scene = LoadTestScene();
        var rootNode = new RootNode();
        var idList = RuntimeInterop.GetPanelIds();

        scene.AddChild(rootNode);
        rootNode.SetSize(new Vector2() {
            X = 1000,
            Y = 1000
        });
        rootNode.SetAnchorsPreset(Control.LayoutPreset.Center);
        await runner.SimulateFrames(1);

        // When: I assert the initial content
        AssertPanelThat(rootNode.GetNode<Panel>(idList[0]))
            .HasContentText("initial");

        // When: I update the panel with new content of the same type
        var panel = rootNode.GetNode<Panel>(idList[0]);
        panel.Update(new NewGameProject.Runtime.Panel {
            Id = idList[0],
            Size = new NewGameProject.Runtime.Size { Width = 300f, Height = 100f },
            Anchor = new NewGameProject.Runtime.Vector2 { X = 0.5f, Y = 0.5f },
            Content = new NewGameProject.Runtime.ConstantTextContent("updated", "center")
        });

        // Then: the content should be updated via polymorphic UpdateContent
        AssertPanelThat(panel)
            .HasContentText("updated");
    }
}
