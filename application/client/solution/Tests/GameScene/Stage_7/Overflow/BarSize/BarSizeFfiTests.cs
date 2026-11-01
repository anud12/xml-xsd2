using GdUnit4;
using Godot;
using NewGameProject.Tests.XUnit;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_7.BarSize;

[TestSuite]
public partial class BarSizeFfiTests : Steps {

    [TestCategory("Stage_7")]
    [TestCase(Timeout = 10_000)]
    [RequireGodotRuntime]
    public async Task Given_scroll_window_with_bar_size_the_horizontal_scrollbar_should_use_that_thickness() {
        try {
            AddFileToArchive("module/index.js", "index.js")
                .AddFileToArchive("module/manifest.json", "manifest.json")
                .AddFileToArchive("..\\OverflowViewport\\item_red.png", "item_red.png")
                .AddFileToArchive("..\\OverflowViewport\\panel_bg.png", "panel_bg.png")
                .AddFileToArchive("..\\OverflowViewport\\scroll_track.png", "scroll_track.png")
                .AddFileToArchive("..\\OverflowViewport\\scroll_grabber.png", "scroll_grabber.png")
                .EnsureDllAccessible()
                .ProcessArchive();

            var scene = await AttachUiScene();
            await runner.SimulateFrames(5);

            var customScroll = scene.Window("bar-custom").GetNodeOrNull<ScrollContainer>("scroll");
            Assertions.AssertThat(customScroll != null)
                .OverrideFailureMessage("bar-custom must have a scroll wrapper")
                .IsTrue();
            var customBar = customScroll!.GetHScrollBar();
            Assertions.AssertThat(customBar != null)
                .OverrideFailureMessage("bar-custom scroll wrapper must expose a horizontal scrollbar")
                .IsTrue();
            var customThickness = (float)customBar!.CustomMinimumSize.Y;
            Assertions.AssertThat(Math.Abs(customThickness - 20f) < 0.5f)
                .OverrideFailureMessage(
                    $"bar-custom horizontal scrollbar must be 20px (barSize 20) but is {customThickness}px")
                .IsTrue();

            // Viewport: the window's full render (content + the 20px bottom bar)
            // must match the reference. barSize 20 leaves a 60px content area,
            // so the bottom 20px is the scrollbar, not content.
            scene.AssertPanelThat("bar-custom")
                .ViewportMatches("expected-bar-custom.png");

            var defaultScroll = scene.Window("bar-default").GetNodeOrNull<ScrollContainer>("scroll");
            Assertions.AssertThat(defaultScroll != null)
                .OverrideFailureMessage("bar-default must have a scroll wrapper")
                .IsTrue();
            var defaultBar = defaultScroll!.GetHScrollBar();
            Assertions.AssertThat(defaultBar != null)
                .OverrideFailureMessage("bar-default scroll wrapper must expose a horizontal scrollbar")
                .IsTrue();
            var defaultThickness = (float)defaultBar!.CustomMinimumSize.Y;
            Assertions.AssertThat(Math.Abs(defaultThickness - 8f) < 0.5f)
                .OverrideFailureMessage(
                    $"bar-default horizontal scrollbar must keep the 8px engine default but is {defaultThickness}px")
                .IsTrue();

            // Viewport: the default 8px bar leaves a 72px content area, so the
            // bottom strip is thinner than bar-custom's — a different reference.
            scene.AssertPanelThat("bar-default")
                .ViewportMatches("expected-bar-default.png");

            ClearSimulatedMouse();
        }
        catch (Exception e) {
            Assertions.AssertThat(true)
                .OverrideFailureMessage($"Error: {e.Message}\n{e.StackTrace}")
                .IsFalse();
        }
    }
}
