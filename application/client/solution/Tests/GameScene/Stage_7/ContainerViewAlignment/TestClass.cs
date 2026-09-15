using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;
using Vector2 = Godot.Vector2;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_7.ContainerViewAlignment;

[TestSuite]
public class TestClass : Steps {
    // A 700x500 plane over a 10x5 grid: each container cell is 70 wide and
    // 100 tall. The 2x2 span at top-left cell (2,1) therefore covers local
    // (140,100)-(280,300), with midpoint (210,200) and half-span (70,100).
    static float CellW => 70f;
    static float CellH => 100f;
    static int Span => 2;
    static int Col => 2;
    static int Row => 1;

    // The marker's top-left corner when aligned "top-left".
    static Vector2 TopLeftPos => new(Col * CellW, Row * CellH); // (140,100)
    // The marker's top-left corner when aligned "center": the span midpoint
    // shifted back by half the span, so the marker's center lands on the
    // span midpoint.
    static Vector2 CenterPos => new(
        Col * CellW - Span * CellW / 2f,
        Row * CellH - Span * CellH / 2f); // (70,0)

    [TestCategory("Stage_7")]
    [TestCase]
    [RequireGodotRuntime]
    public async Task Given_container_view_with_center_alignment_it_should_center_marker_on_span_midpoint() {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .EnsureDllAccessible()
            .ProcessArchive();

        var scene = await AttachUiScene();

        // grid-center declares alignment "center"; the runtime must have read
        // the flag off the serialized container row.
        var centered = ContainerInterop.GetContainerById("grid-center");
        Assertions.AssertThat(centered.Alignment).IsEqual(ContainerAlignment.Center);

        // The centered marker is placed at the span midpoint minus half the
        // span, i.e. (x*cellW - span*cellW/2, y*cellH - span*cellH/2).
        scene.AssertPanelThat("node-a")
            .IsPositionEqual(CenterPos.X, CenterPos.Y);
    }

    [TestCategory("Stage_7")]
    [TestCase]
    [RequireGodotRuntime]
    public async Task Given_container_view_without_alignment_it_should_match_top_left() {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .EnsureDllAccessible()
            .ProcessArchive();

        var scene = await AttachUiScene();

        // grid-default omits the alignment flag; the parsed value is null and
        // the marker pins its top-left corner to (x, y).
        var defaults = ContainerInterop.GetContainerById("grid-default");
        Assertions.AssertThat(defaults.Alignment).IsNull();

        // The omitted-flag marker sits exactly at the span's top-left cell —
        // identical to the explicit "top-left" placement.
        scene.AssertPanelThat("node-b")
            .IsPositionEqual(TopLeftPos.X, TopLeftPos.Y);
    }
}
