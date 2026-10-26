using GdUnit4.Examples.Basics.Setup.Sources.UI;
using Godot;
using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_0.Expression.NumberReadOnlyExpression.Random;

[TestSuite]
public partial class TestClass : Steps
{
    [TestCategory("Step_0")]
    [TestCase]
    [RequireGodotRuntime]
    public async Task Random_returns_value_within_range()
    {
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/fonts/Ubuntu-Regular.ttf", "fonts/Ubuntu-Regular.ttf")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .AddFileToArchive("module/texture.png", "texture.png")
            .EnsureDllAccessible()
            .ProcessArchive();

        var scene = await AttachUiScene();

        var assertions = scene.AssertPanelThat("content");

        await runner.SimulateFrames(1);
        RuntimeInterop.RunIteration();
        await runner.SimulateFrames(1);

        assertions.HasContentText("5");
    }

    [TestCategory("Step_0")]
    [TestCase]
    [RequireGodotRuntime]
    public async Task Random_supports_chaining_after_sum()
    {
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/fonts/Ubuntu-Regular.ttf", "fonts/Ubuntu-Regular.ttf")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .AddFileToArchive("module/texture.png", "texture.png")
            .EnsureDllAccessible()
            .ProcessArchive();

        var scene = await AttachUiScene();

        var assertions = scene.AssertPanelThat("chained_content");

        await runner.SimulateFrames(1);
        RuntimeInterop.RunIteration();
        await runner.SimulateFrames(1);

        assertions.HasContentText("15");
    }
}
