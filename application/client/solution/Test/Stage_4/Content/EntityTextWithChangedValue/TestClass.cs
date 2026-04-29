using System.Runtime.ExceptionServices;
using CommandLine;
using GdUnit4.Examples.Basics.Setup.Sources.UI;
using Godot;
using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;
using Vector2 = Godot.Vector2;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_4.Content.EntityTextWithChangedValue;

[TestSuite]
public class TestClass : Steps {
    [TestCategory("Stage_4")]
    [TestCase(Timeout = 1_000)]
    [RequireGodotRuntime]
    [GodotExceptionMonitor]
    public async Task Given_panel_with_entity_text_value_when_update_text_map_it_should_display_new_value() {
        // I create a module from the fourth folder for Stage_4 test
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .AddFileToArchive("module/texture.exr", "texture.exr")
            .EnsureDllAccessible()
            .ProcessArchive();


        var scene = LoadTestScene();
        var rootNode = new Root();
        var idList = RuntimeInterop.GetPanelIds();
        foreach (var id in idList) {
            rootNode.AddChild(new Panel(RuntimeInterop.GetPanelById(id)));
        }

        scene.AddChild(rootNode);
        rootNode.SetSize(new Vector2() {
            X = 1000,
            Y = 1000
        });
        rootNode.SetAnchorsPreset(Control.LayoutPreset.Center);
        
        // Verify initial panel displays the initial value from entity's textMap
        await runner.SimulateFrames(1);

        var panel = rootNode.GetNode<Panel>(idList[0]);
        AssertPanelThat(panel)
            .IsNonNull()
            .HasContentText("initialTextValue");
        

        // Update entity's textMap using the not-implemented SetEntityTextMapValue API
        RuntimeInterop.SetEntityTextMapValue("entity_id", "textKey", "updatedTextValue");
        
        // Simulate iterations for runtime to process the change
        RuntimeInterop.SimulateIterations(1);

        // Verify panel now displays the updated value after textMap change
        AssertPanelThat(panel)
            .HasContentText("updatedTextValue");
        
    }
}
