using GdUnit4.Examples.Basics.Setup.Sources.UI;
using Godot;
using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;
using Vector2 = Godot.Vector2;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_4.EntityTextValueUpdate;

[TestSuite]
public partial class TestClass : Steps {
    [TestCategory("Step_3")]
    [TestCase]
    [RequireGodotRuntime]
    public async Task Given_panel_it_should_emit_action_on_click_and_trigger_effect() {
        // I create a module from the first folder
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .AddFileToArchive("module/texture.png", "texture.png")
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

        var assertions = AssertPanelThat(rootNode.GetNode<Panel>(idList[0]))
            .HasContentText("textValue");

        await runner.SimulateFrames(1);
        RuntimeInterop.SetEntityTextMapValue("entity_id", "textKey", "newTextValue");
        assertions.HasContentText("textValue");
        RuntimeInterop.RunIteration();
        await runner.SimulateFrames(1);

        assertions.HasContentText("newTextValue");
    }
}