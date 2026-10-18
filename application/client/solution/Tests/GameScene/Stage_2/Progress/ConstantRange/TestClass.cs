using Godot;
using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_2.Progress.ConstantRange;

[TestSuite]
public class TestClass : Steps {
    [TestCategory("Step_2")]
    [TestCase]
    [RequireGodotRuntime]
    public async Task Given_progress_bound_to_a_constant_range_the_bar_should_ignore_entity_data() {
        try {
            AddFileToArchive("module/index.js", "index.js")
                .AddFileToArchive("module/manifest.json", "manifest.json")
                .AddFileToArchive("module/fill.png", "fill.png")
                .AddFileToArchive("module/track.png", "track.png")
                .EnsureDllAccessible()
                .ProcessArchive();

            AssertRuntimeOutputContainsNot("[ModuleEngine] execute error");

            var scene = await AttachUiScene();

            // Constant 3 over the constant [0, 10] range -> 0.3, while the
            // entity's own hp = 9 must not leak into the bar; the reference
            // shows the fill stopping at 30% (x = 60 of 200).
            scene.AssertPanelThat("prog-const-win")
                .IsPositionEqual(100, 100);
            var assertions = scene.AssertPanelThat("prog-const")
                .ViewportIsSize(200, 50)
                .ViewportMatches("prog-const.png");

            // Mutating the bound entity's number data must not move the bar:
            // constant lambdas carry no per-tick field keys.
            RuntimeInterop.SetEntityNumberMapValue("hero", "hp", 100);
            RuntimeInterop.RunIteration();
            await runner.SimulateFrames(1);

            assertions.ViewportMatches("prog-const.png");
        }
        catch (Exception e) {
            Assertions.AssertThat(true)
                .OverrideFailureMessage($"Error: {e.Message}\n{e.StackTrace}")
                .IsFalse();
        }
    }
}
