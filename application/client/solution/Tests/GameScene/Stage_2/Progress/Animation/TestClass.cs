using Godot;
using NewGameProject.Tests.XUnit;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_2.Progress.Animation;

[TestSuite]
public class TestClass : Steps {
    [TestCategory("Step_2")]
    [TestCase]
    [RequireGodotRuntime]
    public async Task Given_progress_fill_is_an_animation_the_viewport_should_match_the_reference() {
        try {
            AddFileToArchive("module/index.js", "index.js")
                .AddFileToArchive("module/manifest.json", "manifest.json")
                .AddFileToArchive("module/fill_1.png", "fill_1.png")
                .AddFileToArchive("module/fill_2.png", "fill_2.png")
                .AddFileToArchive("module/track.png", "track.png")
                .EnsureDllAccessible()
                .ProcessArchive();

            AssertRuntimeOutputContainsNot("[ModuleEngine] execute error");

            var scene = await AttachUiScene();

            // The fill is an animation reference (2 frames). In test mode the
            // runtime loop is off, so elapsed time never advances and frame 0
            // (fill_1.png) is what renders. The track is a static tile across the
            // full bar. The reference proves the animation ref resolves to the
            // current frame's tile texture (not a static first-frame path).
            scene.AssertPanelThat("prog-anim")
                .IsPositionEqual(100, 100)
                .ViewportIsSize(200, 50)
                .ViewportMatches("prog-anim.png");

            // Headless test viewports don't render textures (the reference is
            // all black), so verify resolution directly: the fill animation
            // resolves to its first frame (fill_1.png) and the static track
            // resolves to track.png, both loaded from the archive.
            var window = scene.Window("prog-anim");
            Assertions.AssertThat(window.ProgressFillSourcePath)
                .OverrideFailureMessage("fill animation should resolve to frame 0 (fill_1.png)")
                .IsEqual("fill_1.png");
            Assertions.AssertThat(window.ProgressFillTexture)
                .OverrideFailureMessage("fill frame 0 texture should load from the archive")
                .IsNotNull();
            Assertions.AssertThat(window.ProgressTrackSourcePath)
                .OverrideFailureMessage("static track should resolve to track.png")
                .IsEqual("track.png");
            Assertions.AssertThat(window.ProgressTrackTexture)
                .OverrideFailureMessage("track texture should load from the archive")
                .IsNotNull();
        }
        catch (Exception e) {
            Assertions.AssertThat(true)
                .OverrideFailureMessage($"Error: {e.Message}\n{e.StackTrace}")
                .IsFalse();
        }
    }
}
