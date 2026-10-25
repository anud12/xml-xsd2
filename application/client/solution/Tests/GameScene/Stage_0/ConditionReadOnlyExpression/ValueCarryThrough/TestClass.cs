using GdUnit4.Examples.Basics.Setup.Sources.UI;
using Godot;
using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_0.Condition.ValueCarryThrough;

[TestSuite]
public partial class TestClass : Steps
{
    [TestCategory("Step_0")]
    [TestCase]
    [RequireGodotRuntime]
    public async Task ConditionIfTrue_carries_value_and_decrements()
    {
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/fonts/Ubuntu-Regular.ttf", "fonts/Ubuntu-Regular.ttf")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .AddFileToArchive("module/texture.png", "texture.png")
            .EnsureDllAccessible()
            .ProcessArchive();

        var scene = await AttachUiScene();

        var assertions = scene.AssertPanelThat("center")
            .HasContentText("5");

        await runner.SimulateFrames(1);
        RuntimeInterop.RunIteration();
        await runner.SimulateFrames(1);

        assertions.HasContentText("4");
    }

    [TestCategory("Step_0")]
    [TestCase]
    [RequireGodotRuntime]
    public async Task ConditionOr_carries_value_and_keeps_zero()
    {
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/fonts/Ubuntu-Regular.ttf", "fonts/Ubuntu-Regular.ttf")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .AddFileToArchive("module/texture.png", "texture.png")
            .EnsureDllAccessible()
            .ProcessArchive();

        var scene = await AttachUiScene();

        var assertions = scene.AssertPanelThat("center")
            .HasContentText("5");

        await runner.SimulateFrames(1);

        RuntimeInterop.SetEntityNumberMapValue("entity_id", "hp", 0);
        RuntimeInterop.RunIteration();
        await runner.SimulateFrames(1);

        assertions.HasContentText("0");
    }
}
