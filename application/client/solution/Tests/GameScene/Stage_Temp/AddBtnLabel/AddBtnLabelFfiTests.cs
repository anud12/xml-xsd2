using GdUnit4.Examples.Basics.Setup.Sources.UI;
using Godot;
using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_temp.AddBtnLabel;

[TestSuite]
public partial class AddBtnLabelFfiTests : Steps {
    [TestCategory("Stage_temp")]
    [TestCase]
    [RequireGodotRuntime]
    public async Task Given_add_btn_with_changing_value_when_ticks_run_the_label_should_show_only_the_current_value() {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/fonts/Ubuntu-Regular.ttf", "fonts/Ubuntu-Regular.ttf")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .ProcessArchive();

        var scene = await AttachUiScene();

        // Advance the sim several periods so the field value changes and the
        // label re-renders repeatedly (the "old" text must be cleared each time).
        for (int i = 0; i < 5; i++) {
            RuntimeInterop.RunIteration(10);
            await runner.SimulateFrames(1);
        }

        var entityValue = RuntimeInterop.ReadEntityNumberValue("game", "property");
        var label = scene.Window("add-btn").GetNodeOrNull<Label>("text");
        var labelText = label?.Text ?? "<no label>";
        var childExists = scene.GetWindowOrNull("add-label") != null;
        Console.WriteLine($"DEBUG AddBtnLabel entityValue='{entityValue}' labelText='{labelText}' add-labelWindowExists={childExists}");

        // The label must show exactly the current value, not a concatenation of
        // every value it has shown.
        bool labelMatches = double.TryParse(labelText, out var labelNum)
            && double.TryParse(entityValue, out var entityNum)
            && labelNum == entityNum;
        Assertions.AssertThat(labelMatches)
            .OverrideFailureMessage($"add-btn label text '{labelText}' does not match the current property value '{entityValue}' (text is not being cleared / is accumulating)")
            .IsTrue();
    }
}
