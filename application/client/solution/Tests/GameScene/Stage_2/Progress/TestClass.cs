using Godot;
using NewGameProject.Tests.XUnit;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_2.Progress;

[TestSuite]
public class TestClass : Steps {
    [TestCategory("Step_2")]
    [TestCase]
    [RequireGodotRuntime]
    public async Task Given_progress_x_the_panel_should_have_the_declared_geometry() {
        try {
            AddFileToArchive("module/index.js", "index.js")
                .AddFileToArchive("module/manifest.json", "manifest.json")
                .AddFileToArchive("module/fill.png", "fill.png")
                .AddFileToArchive("module/track.png", "track.png")
                .EnsureDllAccessible()
                .ProcessArchive();

            AssertRuntimeOutputContainsNot("[ModuleEngine] execute error");

            var scene = await AttachUiScene();

            scene.AssertPanelThat("prog-x")
                .IsPositionEqual(100, 100)
                .ViewportIsSize(200, 50);
        }
        catch (Exception e) {
            Assertions.AssertThat(true)
                .OverrideFailureMessage($"Error: {e.Message}\n{e.StackTrace}")
                .IsFalse();
        }
    }

    [TestCategory("Step_2")]
    [TestCase]
    [RequireGodotRuntime]
    public async Task Given_progress_y_the_panel_should_have_the_declared_geometry() {
        try {
            AddFileToArchive("module/index.js", "index.js")
                .AddFileToArchive("module/manifest.json", "manifest.json")
                .AddFileToArchive("module/fill.png", "fill.png")
                .AddFileToArchive("module/track.png", "track.png")
                .EnsureDllAccessible()
                .ProcessArchive();

            AssertRuntimeOutputContainsNot("[ModuleEngine] execute error");

            var scene = await AttachUiScene();

            scene.AssertPanelThat("prog-y")
                .IsPositionEqual(100, 200)
                .ViewportIsSize(50, 200);
        }
        catch (Exception e) {
            Assertions.AssertThat(true)
                .OverrideFailureMessage($"Error: {e.Message}\n{e.StackTrace}")
                .IsFalse();
        }
    }

    [TestCategory("Step_2")]
    [TestCase]
    [RequireGodotRuntime]
    public async Task Given_progress_x_the_viewport_should_match_the_reference() {
        try {
            AddFileToArchive("module/index.js", "index.js")
                .AddFileToArchive("module/manifest.json", "manifest.json")
                .AddFileToArchive("module/fill.png", "fill.png")
                .AddFileToArchive("module/track.png", "track.png")
                .EnsureDllAccessible()
                .ProcessArchive();

            AssertRuntimeOutputContainsNot("[ModuleEngine] execute error");

            var scene = await AttachUiScene();

            scene.AssertPanelThat("prog-x")
                .IsPositionEqual(100, 100)
                .ViewportIsSize(200, 50)
                .ViewportMatches("prog-x.png");
        }
        catch (Exception e) {
            Assertions.AssertThat(true)
                .OverrideFailureMessage($"Error: {e.Message}\n{e.StackTrace}")
                .IsFalse();
        }
    }

    [TestCategory("Step_2")]
    [TestCase]
    [RequireGodotRuntime]
    public async Task Given_progress_y_the_viewport_should_match_the_reference() {
        try {
            AddFileToArchive("module/index.js", "index.js")
                .AddFileToArchive("module/manifest.json", "manifest.json")
                .AddFileToArchive("module/fill.png", "fill.png")
                .AddFileToArchive("module/track.png", "track.png")
                .EnsureDllAccessible()
                .ProcessArchive();

            AssertRuntimeOutputContainsNot("[ModuleEngine] execute error");

            var scene = await AttachUiScene();

            scene.AssertPanelThat("prog-y")
                .IsPositionEqual(100, 200)
                .ViewportIsSize(50, 200)
                .ViewportMatches("prog-y.png");
        }
        catch (Exception e) {
            Assertions.AssertThat(true)
                .OverrideFailureMessage($"Error: {e.Message}\n{e.StackTrace}")
                .IsFalse();
        }
    }
}
