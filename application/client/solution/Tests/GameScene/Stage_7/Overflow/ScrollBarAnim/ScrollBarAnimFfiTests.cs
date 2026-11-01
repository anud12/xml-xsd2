using GdUnit4;
using Godot;
using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_7.ScrollBarAnim;

[TestSuite]
public partial class ScrollBarAnimFfiTests : Steps {

    [TestCategory("Stage_7")]
    [TestCase(Timeout = 10_000)]
    [RequireGodotRuntime]
    public async Task Given_scrollbar_animation_when_time_advances_the_scrollbar_texture_should_change_frame() {
        try {
            AddFileToArchive("module/index.js", "index.js")
                .AddFileToArchive("module/manifest.json", "manifest.json")
                .AddFileToArchive("module/item_red.png", "item_red.png")
                .AddFileToArchive("module/panel_bg.png", "panel_bg.png")
                .AddFileToArchive("module/scroll_track.png", "scroll_track.png")
                .AddFileToArchive("module/scroll_grabber.png", "scroll_grabber.png")
                .EnsureDllAccessible()
                .ProcessArchive();

            var scene = await AttachUiScene();

            // Frame 0: the scrollbar track and grabber show the first frame (cyan).
            scene.AssertPanelThat("sb-anim-host").ViewportMatches("expected-frame0.png");

            // Advance the runtime clock by 2 ticks. With duration 2 and 2 frames
            // (1 tick/frame), localElapsed reaches 2 and the animation advances
            // to frame 1 (magenta).
            RuntimeInterop.RunIteration(2);
            await runner.SimulateFrames(1);

            // Frame 1: the scrollbar track and grabber now show the second frame (magenta).
            scene.AssertPanelThat("sb-anim-host").ViewportMatches("expected-frame1.png");

            ClearSimulatedMouse();
        }
        catch (Exception e) {
            Assertions.AssertThat(true)
                .OverrideFailureMessage($"Error: {e.Message}\n{e.StackTrace}")
                .IsFalse();
        }
    }
}
