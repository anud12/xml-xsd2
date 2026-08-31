using GdUnit4.Examples.Basics.Setup.Sources.UI;
using Godot;
using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;
using Vector2 = Godot.Vector2;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_4.EntityNumberValueUpdate;

[TestSuite]
public partial class TestClass : Steps {
    [TestCategory("Step_4")]
    [TestCase]
    [RequireGodotRuntime]
    public async Task Given_panel_it_should_update_number_value_when_entity_changes() {
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .AddFileToArchive("module/texture.png", "texture.png")
            .EnsureDllAccessible()
            .ProcessArchive();

        var scene = await AttachUiScene();

        var assertions = scene.AssertPanelThat("center")
            .HasContentText("42");

        await runner.SimulateFrames(1);
        RuntimeInterop.SetEntityNumberMapValue("entity_id", "numberKey", 99);
        assertions.HasContentText("42");
        RuntimeInterop.RunIteration();
        await runner.SimulateFrames(1);

        assertions.HasContentText("99");

    }
}
