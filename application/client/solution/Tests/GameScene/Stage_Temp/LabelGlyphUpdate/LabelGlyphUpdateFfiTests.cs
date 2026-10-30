using GdUnit4.Examples.Basics.Setup.Sources.UI;
using Godot;
using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_temp.LabelGlyphUpdate;

[TestSuite]
public partial class LabelGlyphUpdateFfiTests : Steps {
    int CountTextLabels(Node node)
    {
        int count = 0;
        if (node is Label lbl && lbl.Name == "text") count++;
        foreach (var child in node.GetChildren()) count += CountTextLabels(child);
        return count;
    }

    [TestCategory("Stage_temp")]
    [TestCase]
    [RequireGodotRuntime]
    public async Task Given_field_showing_10_when_value_changes_to_20_the_label_shows_only_the_new_value() {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/fonts/Ubuntu-Regular.ttf", "fonts/Ubuntu-Regular.ttf")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .ProcessArchive();

        var scene = await AttachUiScene();

        // Let the scene paint a few times at the initial value (10).
        for (int i = 0; i < 5; i++) {
            RuntimeInterop.RunIteration(10);
            await runner.SimulateFrames(1);
        }

        var label = scene.Window("add-btn").GetNodeOrNull<Label>("text");
        var firstText = label?.Text ?? "<no label>";
        var firstLabels = CountTextLabels(scene.Window("add-btn"));
        Console.WriteLine($"DEBUG glyph first='{firstText}' textLabelCount={firstLabels}");
        Assertions.AssertThat(firstText == "10")
            .OverrideFailureMessage($"initial label '{firstText}' != '10'")
            .IsTrue();

        // Change the bound value to another two-glyph number (20).
        RuntimeInterop.SetEntityNumberMapValue("game", "property", 20);
        for (int i = 0; i < 5; i++) {
            RuntimeInterop.RunIteration(10);
            await runner.SimulateFrames(1);
        }

        var secondText = scene.Window("add-btn").GetNodeOrNull<Label>("text")?.Text ?? "<no label>";
        var secondLabels = CountTextLabels(scene.Window("add-btn"));
        Console.WriteLine($"DEBUG glyph second='{secondText}' textLabelCount={secondLabels}");

        // The label must show exactly the new value: two readable glyphs "20",
        // never a concatenation of old + new ("1020") and never a stale "10".
        Assertions.AssertThat(secondText == "20")
            .OverrideFailureMessage($"after changing to 20, label is '{secondText}' (old glyphs not cleared / value not updating cleanly)")
            .IsTrue();

        // And there must be exactly one "text" label under the window: the
        // folded field must not be re-materialized as a duplicate label.
        Assertions.AssertThat(secondLabels == 1)
            .OverrideFailureMessage($"add-btn has {secondLabels} 'text' labels (expected 1; the folded field was re-materialized as a duplicate)")
            .IsTrue();
    }
}
