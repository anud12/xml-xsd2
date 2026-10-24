using Godot;
using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_2.Progress.RecurringDeplete;

[TestSuite]
public class TestClass : Steps
{
    [TestCategory("Step_2")]
    [TestCase]
    [RequireGodotRuntime]
    public async Task Given_progress_bound_to_entity_the_bar_should_deplete_as_a_recurring_effect_decrements_the_value()
    {
        try
        {
            AddFileToArchive("module/index.js", "index.js")
                .AddFileToArchive("module/manifest.json", "manifest.json")
                .AddFileToArchive("module/fill.png", "fill.png")
                .AddFileToArchive("module/track.png", "track.png")
                .EnsureDllAccessible()
                .ProcessArchive();

            AssertRuntimeOutputContainsNot("[ModuleEngine] execute error");

            await AttachUiScene();

            var initial = ReadProgressValue("deplete-bar");
            Assertions.AssertThat(initial).IsEqual(5.0);

            RuntimeInterop.RunIteration(1);
            await runner.SimulateFrames(1);
            var afterOne = ReadProgressValue("deplete-bar");
            Assertions.AssertThat(afterOne < initial).IsTrue();

            RuntimeInterop.RunIteration(3);
            await runner.SimulateFrames(1);
            var afterFour = ReadProgressValue("deplete-bar");
            Assertions.AssertThat(afterFour <= 1.0).IsTrue();
        }
        catch (Exception e)
        {
            Assertions.AssertThat(true)
                .OverrideFailureMessage($"Error: {e.Message}\n{e.StackTrace}")
                .IsFalse();
        }
    }

    static double ReadProgressValue(string nodeId)
    {
        var node = UiStateReader.GetNode(nodeId);
        if (node.ValueKind != System.Text.Json.JsonValueKind.Object)
            throw new Exception($"Node {nodeId} not found in UI state");
        if (!node.TryGetProperty("options", out var opts) || opts.ValueKind != System.Text.Json.JsonValueKind.Object)
            throw new Exception($"Node {nodeId} has no options");
        if (!opts.TryGetProperty("progress", out var p) || p.ValueKind != System.Text.Json.JsonValueKind.Object)
            throw new Exception($"Node {nodeId} has no progress options");
        if (!p.TryGetProperty("value", out var v))
            throw new Exception($"Node {nodeId} progress has no value");
        return v.GetDouble();
    }
}
