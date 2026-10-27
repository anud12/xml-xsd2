using GdUnit4.Examples.Basics.Setup.Sources.UI;
using Godot;
using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;
using Vector2 = Godot.Vector2;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_4.EffectInputOutputFlow;

[TestSuite]
public partial class TestClass : Steps {
    [TestCategory("Step_4")]
    [TestCase]
    [RequireGodotRuntime]
    public async Task Given_emit_payload_it_should_flow_prepare_input_to_apply_output() {
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/fonts/Ubuntu-Regular.ttf", "fonts/Ubuntu-Regular.ttf")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .AddFileToArchive("module/texture.png", "texture.png")
            .EnsureDllAccessible()
            .ProcessArchive();

        var scene = await AttachUiScene();

        // Before the effect fires, the entity's `result` is 0.
        var assertions = scene.AssertPanelThat("center")
            .HasContentText("0");

        // Run one iteration: the pending effect fires. prepare receives the
        // emit payload (base=42) as `input`, returns 52; apply writes 52 to the
        // entity's `result` map from prepare's returned `output`.
        RuntimeInterop.RunIteration(1);
        await runner.SimulateFrames(1);

        // 52 proves input flowed into prepare (not 0) AND prepare's return
        // flowed into apply (not 42, the raw input).
        assertions.HasContentText("52");
    }
}
