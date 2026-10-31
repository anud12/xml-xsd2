using GdUnit4;
using Godot;
using NewGameProject.Tests.XUnit;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_7.OverflowViewport;

[TestSuite]
public partial class PanelOverflowViewportFfiTests : Steps {

    [TestCategory("Stage_7")]
    [TestCase(Timeout = 10_000)]
    [RequireGodotRuntime]
    public async Task Given_overflow_list_when_painted_it_should_clip_the_content_to_the_panel() {
        try {
            AddFileToArchive("module/index.js", "index.js")
                .AddFileToArchive("module/manifest.json", "manifest.json")
                .AddFileToArchive("item_red.png", "item_red.png")
                .AddFileToArchive("item_green.png", "item_green.png")
                .AddFileToArchive("item_blue.png", "item_blue.png")
                .AddFileToArchive("panel_bg.png", "panel_bg.png")
                .EnsureDllAccessible()
                .ProcessArchive();

            var scene = await AttachUiScene();

            // Both lists are 5 x 40px of content in a smaller panel, so the
            // viewport shows only the first items and clips the rest.
            scene.AssertPanelThat("hscroll-host").ViewportMatches("expected-initial-h.png");
            scene.AssertPanelThat("vscroll-host").ViewportMatches("expected-initial-v.png");

            ClearSimulatedMouse();
        }
        catch (Exception e) {
            Assertions.AssertThat(true)
                .OverrideFailureMessage($"Error: {e.Message}\n{e.StackTrace}")
                .IsFalse();
        }
    }

    [TestCategory("Stage_7")]
    [TestCase(Timeout = 10_000)]
    [RequireGodotRuntime]
    public async Task Given_overflow_list_when_dragging_the_scrollbar_it_should_scroll_the_content() {
        try {
            AddFileToArchive("module/index.js", "index.js")
                .AddFileToArchive("module/manifest.json", "manifest.json")
                .AddFileToArchive("item_red.png", "item_red.png")
                .AddFileToArchive("item_green.png", "item_green.png")
                .AddFileToArchive("item_blue.png", "item_blue.png")
                .AddFileToArchive("panel_bg.png", "panel_bg.png")
                .EnsureDllAccessible()
                .ProcessArchive();

            var scene = await AttachUiScene();

            var hScroll = scene.Window("hscroll-list").GetNodeOrNull<ScrollContainer>("scroll");
            Assertions.AssertThat(hScroll != null)
                .OverrideFailureMessage("horizontal overflow list must have a scroll wrapper")
                .IsTrue();
            var vScroll = scene.Window("vscroll-list").GetNodeOrNull<ScrollContainer>("scroll");
            Assertions.AssertThat(vScroll != null)
                .OverrideFailureMessage("vertical overflow list must have a scroll wrapper")
                .IsTrue();

            // Baseline: before any interaction each viewport shows the first
            // items and clips the rest (same references the clip test uses).
            scene.AssertPanelThat("hscroll-host").ViewportMatches("expected-initial-h.png");
            scene.AssertPanelThat("vscroll-host").ViewportMatches("expected-initial-v.png");

            // Drag the horizontal scrollbar (the ScrollContainer's bottom
            // edge) from the left of its track toward the right: the content
            // must scroll right, revealing the clipped items.
            var hBefore = hScroll.ScrollHorizontal;
            DragHorizontalScrollbar(hScroll);
            await runner.SimulateFrames(3);
            var hAfter = hScroll.ScrollHorizontal;
            Assertions.AssertThat(hAfter > hBefore)
                .OverrideFailureMessage(
                    $"horizontal list did not scroll after dragging the scrollbar: before={hBefore} after={hAfter}")
                .IsTrue();
            scene.AssertPanelThat("hscroll-host").ViewportMatches("expected-after-h-scroll.png");

            // Drag the vertical scrollbar (the ScrollContainer's right edge)
            // from the top of its track toward the bottom: the content must
            // scroll down, revealing the clipped items.
            var vBefore = vScroll.ScrollVertical;
            DragVerticalScrollbar(vScroll);
            await runner.SimulateFrames(3);
            var vAfter = vScroll.ScrollVertical;
            Assertions.AssertThat(vAfter > vBefore)
                .OverrideFailureMessage(
                    $"vertical list did not scroll after dragging the scrollbar: before={vBefore} after={vAfter}")
                .IsTrue();
            scene.AssertPanelThat("vscroll-host").ViewportMatches("expected-after-scroll.png");
            ClearSimulatedMouse();
        }
        catch (Exception e) {
            Assertions.AssertThat(true)
                .OverrideFailureMessage($"Error: {e.Message}\n{e.StackTrace}")
                .IsFalse();
        }
    }

    [TestCategory("Stage_7")]
    [TestCase(Timeout = 10_000)]
    [RequireGodotRuntime]
    public async Task Given_window_overflow_when_pass_through_entity_list_overflows_it_should_show_a_horizontal_scrollbar() {
        try {
            AddFileToArchive("module/index.js", "index.js")
                .AddFileToArchive("module/manifest.json", "manifest.json")
                .AddFileToArchive("item_red.png", "item_red.png")
                .AddFileToArchive("item_green.png", "item_green.png")
                .AddFileToArchive("item_blue.png", "item_blue.png")
                .AddFileToArchive("panel_bg.png", "panel_bg.png")
                .EnsureDllAccessible()
                .ProcessArchive();

            var scene = await AttachUiScene();
            await runner.SimulateFrames(5);

            // The window (not the entityList) carries the overflow: a pass-
            // through entityList must still drive the window's scroll, so the
            // window shows a horizontal scrollbar for the overflowing content.
            var host = scene.Window("win-scroll-host");
            var scroll = host.GetNodeOrNull<ScrollContainer>("scroll");
            Assertions.AssertThat(scroll != null)
                .OverrideFailureMessage("window with overflow must have a scroll wrapper")
                .IsTrue();
            Assertions.AssertThat(scroll!.HorizontalScrollMode == ScrollContainer.ScrollMode.Auto)
                .OverrideFailureMessage("window width overflow must enable horizontal auto scroll")
                .IsTrue();

            // The pass-through entityList must propagate its content width so
            // the window's scroll content is wider than the scroll viewport; that
            // overflow is exactly what makes the ScrollContainer show its
            // horizontal scrollbar (a plain Control reports (0,0) and would not).
            var box = scroll.GetNode<BoxContainer>("box");
            var contentWidth = (float)box.GetCombinedMinimumSize().X;
            var scrollWidth = (float)scroll.Size.X;
            Assertions.AssertThat(contentWidth > scrollWidth)
                .OverrideFailureMessage(
                    $"window scroll content must overflow the viewport: content={contentWidth} scroll={scrollWidth}")
                .IsTrue();

            scene.AssertPanelThat("win-scroll-host").ViewportMatches("expected-window-overflow.png");
            ClearSimulatedMouse();
        }
        catch (Exception e) {
            Assertions.AssertThat(true)
                .OverrideFailureMessage($"Error: {e.Message}\n{e.StackTrace}")
                .IsFalse();
        }
    }

    /// Presses the vertical scrollbar of the given ScrollContainer (at the top
    /// of its track, where the grabber sits at scroll offset 0) and drags it
    /// to the bottom, pushing synthetic mouse events the same way the title-
    /// drag test does, so the ScrollContainer's grabber follows the cursor.
    static void DragVerticalScrollbar(ScrollContainer scroll)
    {
        var viewport = scroll.GetViewport();
        var rect = scroll.GetGlobalRect();
        // The vertical scrollbar sits at the right edge; grab near its center.
        var gx = (float)(rect.Position.X + rect.Size.X - 8.0);
        var y0 = (float)(rect.Position.Y + 4.0);
        var y1 = (float)(rect.Position.Y + rect.Size.Y - 4.0);
        var offset = new Vector2(0, y1 - y0);

        viewport.PushInput(new InputEventMouseButton {
            Position = new Vector2(gx, y0),
            GlobalPosition = new Vector2(gx, y0),
            ButtonIndex = MouseButton.Left,
            Pressed = true,
            ButtonMask = MouseButtonMask.Left,
        });
        viewport.PushInput(new InputEventMouseMotion {
            Position = new Vector2(gx, y1),
            GlobalPosition = new Vector2(gx, y1),
            Relative = offset,
        });
        viewport.PushInput(new InputEventMouseButton {
            Position = new Vector2(gx, y1),
            GlobalPosition = new Vector2(gx, y1),
            ButtonIndex = MouseButton.Left,
            Pressed = false,
            ButtonMask = 0,
        });
    }

    /// Presses the horizontal scrollbar of the given ScrollContainer (at the
    /// left of its track, where the grabber sits at scroll offset 0) and drags
    /// it to the right, pushing synthetic mouse events the same way the
    /// vertical helper does, so the ScrollContainer's grabber follows the cursor.
    static void DragHorizontalScrollbar(ScrollContainer scroll)
    {
        var viewport = scroll.GetViewport();
        var rect = scroll.GetGlobalRect();
        // The horizontal scrollbar sits at the bottom edge; grab near its center.
        var gy = (float)(rect.Position.Y + rect.Size.Y - 8.0);
        var x0 = (float)(rect.Position.X + 4.0);
        var x1 = (float)(rect.Position.X + rect.Size.X - 4.0);
        var offset = new Vector2(x1 - x0, 0);

        viewport.PushInput(new InputEventMouseButton {
            Position = new Vector2(x0, gy),
            GlobalPosition = new Vector2(x0, gy),
            ButtonIndex = MouseButton.Left,
            Pressed = true,
            ButtonMask = MouseButtonMask.Left,
        });
        viewport.PushInput(new InputEventMouseMotion {
            Position = new Vector2(x1, gy),
            GlobalPosition = new Vector2(x1, gy),
            Relative = offset,
        });
        viewport.PushInput(new InputEventMouseButton {
            Position = new Vector2(x1, gy),
            GlobalPosition = new Vector2(x1, gy),
            ButtonIndex = MouseButton.Left,
            Pressed = false,
            ButtonMask = 0,
        });
    }
}
