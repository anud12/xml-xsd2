using Godot;
using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_2.Progress.ValueChange;

[TestSuite]
public class TestClass : Steps {
    [TestCategory("Step_2")]
    [TestCase]
    [RequireGodotRuntime]
    public async Task Given_progress_x_the_bar_should_update_when_the_bound_value_changes() {
        try {
            AddFileToArchive("module/index.js", "index.js")
                .AddFileToArchive("module/manifest.json", "manifest.json")
                .AddFileToArchive("module/fill.png", "fill.png")
                .AddFileToArchive("module/track.png", "track.png")
                .EnsureDllAccessible()
                .ProcessArchive();

            AssertRuntimeOutputContainsNot("[ModuleEngine] execute error");

            var scene = await AttachUiScene();

            var assertions = scene.AssertPanelThat("prog-x")
                .IsPositionEqual(100, 100)
                .ViewportIsSize(200, 50)
                .ViewportMatches("prog-x.png");

            await runner.SimulateFrames(1);
            RuntimeInterop.SetEntityNumberMapValue("hero", "hp", 3);
            assertions.ViewportMatches("prog-x.png");

            RuntimeInterop.RunIteration();
            await runner.SimulateFrames(1);

            assertions.ViewportMatches("prog-x-updated.png");
        }
        catch (Exception e) {
            Assertions.AssertThat(true)
                .OverrideFailureMessage($"Error: {e.Message}\n{e.StackTrace}")
                .IsFalse();
        }
    }
}
