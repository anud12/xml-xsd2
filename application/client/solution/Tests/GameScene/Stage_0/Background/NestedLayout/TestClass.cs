using NewGameProject.Tests.XUnit;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_0.Background.NestedLayout;

[TestSuite]
public class TestClass : Steps {
    [TestCategory("Step_0")]
    [TestCase]
    [RequireGodotRuntime]
    public async Task Given_nested_row_column_layout_it_should_position_children_correctly() {
        try {
            AddFileToArchive("module/index.js", "index.js")
                .AddFileToArchive("module/manifest.json", "manifest.json")
                .AddFileToArchive("module/red.png", "red.png")
                .AddFileToArchive("module/green.png", "green.png")
                .AddFileToArchive("module/blue.png", "blue.png")
                .EnsureDllAccessible()
                .ProcessArchive();

            var scene = await AttachUiScene();

            // Outer window is a row.
            scene.AssertPanelThat("nested")
                .IsHorizontal()
                .HasLength(2);

            // Left panel (x): red, at (0,0), size (100,60) — spans full height.
            scene.AssertPanelThat("nested").HasChildPanelNamed("x", p =>
                p.IsPositionEqual(0, 0)
                 .ViewportIsSize(100, 60)
                 .HasBackgroundTexture());

            // Right panel (stats): column, at (100,0), size (100,60).
            scene.AssertPanelThat("nested").HasChildPanelNamed("stats", p =>
                p.IsPositionEqual(100, 0)
                 .ViewportIsSize(100, 60)
                 .IsVertical()
                 .HasLength(2));

            // Top-right (y): green, at (0,0) within stats, size (100,30).
            scene.AssertPanelThat("nested").HasChildPanelNamed("y", p =>
                p.IsPositionEqual(0, 0)
                 .ViewportIsSize(100, 30)
                 .HasBackgroundTexture());

            // Bottom-right (z): blue, at (0,30) within stats, size (100,30).
            scene.AssertPanelThat("nested").HasChildPanelNamed("z", p =>
                p.IsPositionEqual(0, 30)
                 .ViewportIsSize(100, 30)
                 .HasBackgroundTexture());

            // Viewport: the window shows red|green top / blue bottom.
            scene.AssertPanelThat("nested")
                .ViewportMatches("expected-viewport.png");

            DebugSaveScreenshot("result.png");
        }
        catch (Exception e) {
            Assertions.AssertThat(true)
                .OverrideFailureMessage($"Error: {e.Message}\n{e.StackTrace}")
                .IsFalse();
        }
    }
}
