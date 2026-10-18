using Godot;
using NewGameProject.Tests.XUnit;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_2.Progress.MinRange;

[TestSuite]
public class TestClass : Steps {
    [TestCategory("Step_2")]
    [TestCase]
    [RequireGodotRuntime]
    public async Task Given_progress_bound_within_min_max_band_the_viewport_should_match_the_reference() {
        try {
            AddFileToArchive("module/index.js", "index.js")
                .AddFileToArchive("module/manifest.json", "manifest.json")
                .AddFileToArchive("module/fill.png", "fill.png")
                .AddFileToArchive("module/track.png", "track.png")
                .EnsureDllAccessible()
                .ProcessArchive();

            AssertRuntimeOutputContainsNot("[ModuleEngine] execute error");

            var scene = await AttachUiScene();

            // hero.hp = 6 across the [min:2, max:10] band -> (6 - 2) / (10 - 2) = 0.5.
            // Without min the same raw value would fill 6 / 10 = 0.6; the reference
            // shows the fill stopping at the band midpoint (x = 100 of 200), proving
            // the value is normalized across [min, max] rather than against max alone.
            scene.AssertPanelThat("prog-min-win")
                .IsPositionEqual(100, 100);
            scene.AssertPanelThat("prog-min")
                .ViewportIsSize(200, 50)
                .ViewportMatches("prog-min.png");
        }
        catch (Exception e) {
            Assertions.AssertThat(true)
                .OverrideFailureMessage($"Error: {e.Message}\n{e.StackTrace}")
                .IsFalse();
        }
    }
}
