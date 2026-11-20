using GdUnit4.Examples.Basics.Setup.Sources.UI;
using Godot;
using NewGameProject.Tests.XUnit;
using Vector2 = Godot.Vector2;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_3.Resizable.WithScrollbar;

/// A resizable panel that ALSO has a vertical overflow scroll bar. The scroll
/// bar sits flush on the window's right edge — exactly over the window's
/// edge/corner resize hit zone — and (being a <c>MouseFilter.Stop</c> control)
/// swallows the corner/edge press before the window's own resize path sees it,
/// so the panel could never be resized. This test proves the panel still grows
/// when its corner is dragged even with the scroll bar in the hit zone.
[TestSuite]
public partial class TestClass : Steps {
    [TestCategory("Step_3")]
    [TestCase(Timeout = 10_000)]
    [RequireGodotRuntime]
    public async Task Given_resizable_panel_with_scrollbar_it_should_still_grow_when_dragging_the_corner() {
        try {
            AddFileToArchive("module/index.js", "index.js")
                .AddFileToArchive("module/manifest.json", "manifest.json")
                .AddFileToArchive("module/panel_bg.png", "panel_bg.png")
                .AddFileToArchive("module/item.png", "item.png")
                .AddFileToArchive("module/scroll_track.png", "scroll_track.png")
                .AddFileToArchive("module/scroll_grabber.png", "scroll_grabber.png")
                .EnsureDllAccessible()
                .ProcessArchive();

            var scene = await AttachUiScene();
            await runner.SimulateFrames(5);

            var panel = scene.GetWindowOrNull("resizable-scroll");
            Assertions.AssertThat(panel != null)
                .OverrideFailureMessage("resizable-scroll panel should exist")
                .IsTrue();

            // The overflow scroll wrapper is present (the six 40px children
            // overflow the 200px panel) and exposes a vertical scroll bar.
            var scroll = panel.GetNodeOrNull<ScrollContainer>("scroll");
            Assertions.AssertThat(scroll != null)
                .OverrideFailureMessage("the panel should have a scroll wrapper")
                .IsTrue();
            var vbar = scroll.GetVScrollBar();
            Assertions.AssertThat(vbar != null)
                .OverrideFailureMessage("the panel should show a vertical scroll bar")
                .IsTrue();

            // The panel starts at 200x200 at (100,100).
            AssertPanelThat(panel).ViewportIsSize(200, 200);
            AssertPanelThat(panel).IsPositionEqual(100, 100);

            // ── STAGE 1 · INITIAL (no input) ──
            // Viewport (pixel) baseline: the panel shows its overflowing
            // children clipped to the 200x200 area with the vertical scroll bar
            // on the right edge. Regenerated on the first run, reused below.
            AssertPanelThat(panel).ViewportMatches("initial.png");

            // Press the bottom-right corner (local 198,198 — inside the 6px resize
            // hit zone, which the vertical scroll bar sits flush over). The press
            // lands on the bar's track: it must begin a RESIZE, not a scroll.
            var corner = new Vector2(198, 198);
            var offset = new Vector2(50, 50);
            var startScroll = vbar.Value;
            PushPress(panel, corner);
            await runner.SimulateFrames(2);

            // The corner-resize press must not have paged the content (the bar is
            // being used to resize, not to scroll).
            Assertions.AssertThat(Math.Abs(vbar.Value - startScroll) < 0.5f)
                .OverrideFailureMessage(
                    $"the corner-resize press must not scroll the content (scroll {startScroll} -> {vbar.Value})")
                .IsTrue();

            // Drag outward by +50 on both axes, release. The corner press must
            // reach the panel even though the scroll bar is in the hit zone: the
            // panel grows to 250x250, top-left fixed.
            PushMotionRelease(panel, corner, offset);
            await runner.SimulateFrames(2);

            AssertPanelThat(panel).ViewportIsSize(250, 250);
            AssertPanelThat(panel).IsPositionEqual(100, 100);

            // ── STAGE 2 · RESIZED (panel grown to 250x250) ──
            // Viewport (pixel): the larger rect — the content and the vertical
            // scroll bar stretch to the new 250x250 area, proving the resize
            // happened on a panel whose edge carries a scroll bar.
            AssertPanelThat(panel).ViewportMatches("resized.png");

            DebugSaveScreenshot("result.png");
            ClearSimulatedMouse();
        }
        catch (Exception e) {
            Assertions.AssertThat(true)
                .OverrideFailureMessage($"Error: {e.Message}\n{e.StackTrace}")
                .IsFalse();
        }
    }

    /// Pushes a synthetic left-button press at the window's local point — so the
    /// resize path receives it even when a scroll bar occupies the corner hit
    /// zone.
    void PushPress(UiWindow window, Vector2 local)
    {
        var viewport = runner.Scene().GetViewport();
        var gPos = window.GlobalPosition + local;
        viewport.PushInput(new InputEventMouseButton {
            Position = gPos,
            GlobalPosition = gPos,
            ButtonIndex = MouseButton.Left,
            Pressed = true,
            ButtonMask = MouseButtonMask.Left
        });
    }

    /// Pushes a synthetic mouse motion (from the window's local point, by the
    /// given offset) followed by a left-button release at the destination.
    void PushMotionRelease(UiWindow window, Vector2 local, Vector2 offset)
    {
        var viewport = runner.Scene().GetViewport();
        var gMotion = window.GlobalPosition + local + offset;
        viewport.PushInput(new InputEventMouseMotion {
            Position = gMotion,
            GlobalPosition = gMotion,
            Relative = offset
        });
        var gRelease = window.GlobalPosition + local + offset;
        viewport.PushInput(new InputEventMouseButton {
            Position = gRelease,
            GlobalPosition = gRelease,
            ButtonIndex = MouseButton.Left,
            Pressed = false,
            ButtonMask = 0
        });
    }
}
