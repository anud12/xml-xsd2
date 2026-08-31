using GdUnit4.Examples.Basics.Setup.Sources.UI;
using Godot;
using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;
using Vector2 = Godot.Vector2;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_2.Hover;

[TestSuite]
public partial class TestClass : Steps {
    [TestCategory("Step_2")]
    [TestCase]
    [RequireGodotRuntime]
    public async Task Given_panel_with_hover_it_should_show_on_mouse_enter() {
        try {
            AddFileToArchive("module/index.js", "index.js")
                .AddFileToArchive("module/manifest.json", "manifest.json")
                .AddFileToArchive("module/texture.png", "texture.png")
                .AddFileToArchive("module/hover.png", "hover.png")
                .EnsureDllAccessible()
                .ProcessArchive();

            var scene = await AttachUiScene();

            // Pin the mouse to a known position clear of every window so the
            // live cursor (used while SimulatedMouse is null) can't affect the
            // initial hover state.
            SimulateMouse(new Vector2(0, 0));
            await runner.SimulateFrames(1);

            var panel = scene.Window("hoverPanel");
            AssertPanelThat(panel).IsNonNull();

            var hoverOutline = panel.GetNode<HoverOutline>("HoverOutline");
            Assertions.AssertThat(hoverOutline).IsNotNull();
            Assertions.AssertThat(hoverOutline.Visible)
                .OverrideFailureMessage("HoverOutline should start invisible")
                .IsFalse();

            AssertScreenshot("initial.png");

            var at = panel.GlobalPosition + new Vector2(1, 1);
            SimulateMouse(at);
            await runner.SimulateMouseMoveAbsolute(at, 0);
            await runner.SimulateFrames(1);

            Assertions.AssertThat(hoverOutline.Visible)
                .OverrideFailureMessage("HoverOutline should be visible after mouse enter")
                .IsTrue();

            AssertScreenshot("mouse_enter.png");

            SimulateMouse(new Vector2(0, 0));
            await runner.SimulateMouseMoveAbsolute(new Vector2(0, 0), 0);
            await runner.SimulateFrames(1);

            Assertions.AssertThat(hoverOutline.Visible)
                .OverrideFailureMessage("HoverOutline should be invisible after mouse exit")
                .IsFalse();

            AssertScreenshot("mouse_leave.png");
            ClearSimulatedMouse();
            
        }
        catch (Exception e) {
            Assertions.AssertThat(true)
                .OverrideFailureMessage($"Error: {e.Message}\n{e.StackTrace}")
                .IsFalse();
        }
    }
}
