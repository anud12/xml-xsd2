using GdUnit4.Examples.Basics.Setup.Sources.UI;
using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_0.Expression.ListReadOnlyExpression.Count;

[TestSuite]
public partial class TestClass : Steps
{
    [TestCategory("Step_0")]
    [TestCase]
    [RequireGodotRuntime]
    public async Task count_reflects_size_of_entity_list()
    {
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/fonts/Ubuntu-Regular.ttf", "fonts/Ubuntu-Regular.ttf")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .AddFileToArchive("module/texture.png", "texture.png")
            .EnsureDllAccessible()
            .ProcessArchive();

        var scene = await AttachUiScene();

        var assertions = scene.AssertPanelThat("center");

        // Baseline before any effect execution: the initial count is 0.
        assertions.HasContentText("0");

        // First execution counts the two pre-seeded items.
        RuntimeInterop.RunIteration(9);
        await runner.SimulateFrames(1);
        assertions.HasContentText("2");
    }

    [TestCategory("Step_0")]
    [TestCase]
    [RequireGodotRuntime]
    public async Task count_updates_when_the_list_changes()
    {
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/fonts/Ubuntu-Regular.ttf", "fonts/Ubuntu-Regular.ttf")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .AddFileToArchive("module/texture.png", "texture.png")
            .EnsureDllAccessible()
            .ProcessArchive();

        var scene = await AttachUiScene();

        var assertions = scene.AssertPanelThat("center");

        // Baseline before any effect execution.
        assertions.HasContentText("0");

        // First execution: two pre-seeded items.
        RuntimeInterop.RunIteration(9);
        await runner.SimulateFrames(1);
        assertions.HasContentText("2");

        // Second execution: the list grew to three items, so the count updates.
        RuntimeInterop.RunIteration(1);
        await runner.SimulateFrames(1);
        assertions.HasContentText("3");
    }
}
