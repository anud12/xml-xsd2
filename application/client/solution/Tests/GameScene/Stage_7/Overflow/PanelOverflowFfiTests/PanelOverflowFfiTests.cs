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
            .ProcessArchive();

        var scene = await AttachUiScene();

        // Default (no overflow): content shows, no scroll wrapper on either axis.
        scene.AssertPanelThat("show-list")
            .HasOverflow(width: "show", height: "show")
            .HasLength(5);

        // Horizontal scroll only.
        scene.AssertPanelThat("hscroll-list")
            .HasOverflow(width: "scroll", height: "show")
            .HasLength(5);

        // Vertical scroll only.
        scene.AssertPanelThat("vscroll-list")
            .HasOverflow(width: "show", height: "scroll")
            .HasLength(5);

        // Bare "scroll": both axes scroll.
        scene.AssertPanelThat("both-list")
            .HasOverflow(width: "scroll", height: "scroll")
            .HasLength(5);
    }
}
