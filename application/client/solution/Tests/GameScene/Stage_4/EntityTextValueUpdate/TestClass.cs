using GdUnit4.Examples.Basics.Setup.Sources.UI;
using Godot;
using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;
using Vector2 = Godot.Vector2;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_4.EntityTextValueUpdate;

[TestSuite]
public partial class TestClass : Steps {
    [TestCategory("Step_4")]
    [TestCase]
    [RequireGodotRuntime]
    public async Task Given_panel_it_should_update_text_value_when_entity_changes() {
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .AddFileToArchive("module/texture.png", "texture.png")
            .EnsureDllAccessible()
            .ProcessArchive();

        var scene = await AttachUiScene();

        var assertions = scene.AssertPanelThat("center")
            .HasContentText("textValue");

        await runner.SimulateFrames(1);
        RuntimeInterop.SetEntityTextMapValue("entity_id", "textKey", "newTextValue");
        assertions.HasContentText("textValue");
        RuntimeInterop.RunIteration();
        await runner.SimulateFrames(1);

        assertions.HasContentText("newTextValue");
    }
}
