using GdUnit4;
using NewGameProject.Tests.XUnit;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_7.Overflow;

[TestSuite]
public partial class PanelOverflowFfiTests : Steps {

    [TestCategory("Stage_7")]
    [TestCase]
    [RequireGodotRuntime]
    public async Task Given_panel_overflow_options_when_painted_the_scroll_wrappers_match_the_declared_axes() {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .AddFileToArchive("..\\OverflowViewport\\panel_bg.png", "panel_bg.png")
            .AddFileToArchive("..\\OverflowViewport\\item_red.png", "item_red.png")
            .AddFileToArchive("..\\OverflowViewport\\scroll_track.png", "scroll_track.png")
            .AddFileToArchive("..\\OverflowViewport\\scroll_grabber.png", "scroll_grabber.png")
            .ProcessArchive();

        var scene = await AttachUiScene();

        // Default (no overflow): content shows, no scroll wrapper on either axis.
        scene.AssertPanelThat("show-list")
            .HasOverflow(width: "show", height: "show")
            .HasLength(5);

        // Horizontal scroll only: the host window declares the scroll region;
        // the entityList is a pass-through (no scroll of its own).
        scene.AssertPanelThat("hscroll-host")
            .HasOverflow(width: "scroll", height: "show")
            .HasLength(1);
        scene.AssertPanelThat("hscroll-list")
            .HasOverflow(width: "show", height: "show")
            .HasLength(5);

        // Vertical scroll only.
        scene.AssertPanelThat("vscroll-host")
            .HasOverflow(width: "show", height: "scroll")
            .HasLength(1);
        scene.AssertPanelThat("vscroll-list")
            .HasOverflow(width: "show", height: "show")
            .HasLength(5);

        // Both axes scroll.
        scene.AssertPanelThat("both-host")
            .HasOverflow(width: "scroll", height: "scroll")
            .HasLength(1);
        scene.AssertPanelThat("both-list")
            .HasOverflow(width: "show", height: "show")
            .HasLength(5);

        // Window-level overflow: the window declares the scroll region, the
        // entityList is a content-sized pass-through (no scroll of its own).
        scene.AssertPanelThat("win-overflow-host")
            .HasOverflow(width: "scroll", height: "show")
            .HasLength(1);
        scene.AssertPanelThat("win-overflow-list")
            .HasOverflow(width: "show", height: "show")
            .HasLength(5);

        // Plain panel (division) with overflow: the scroll behavior is uniform
        // across every flow-hosting component that carries a panel.
        scene.AssertPanelThat("panel-overflow")
            .HasOverflow(width: "scroll", height: "show")
            .HasLength(3);
    }
}
