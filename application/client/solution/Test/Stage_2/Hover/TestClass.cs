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
                .AddFileToArchive("module/texture.exr", "texture.exr")
                .AddFileToArchive("module/hover.exr", "hover.exr")
                .EnsureDllAccessible()
                .ProcessArchive();

            var scene = LoadTestScene();
            var rootNode = new RootNode();

            scene.AddChild(rootNode);
            rootNode.SetSize(new Vector2() {
                X = 1000,
                Y = 1000
            });
            rootNode.SetAnchorsPreset(Control.LayoutPreset.Center);
            await runner.SimulateFrames(1);

            var panel = rootNode.GetNode<Panel>("hoverPanel");
            AssertPanelThat(panel).IsNonNull();

            var hoverOutline = panel.GetNode<HoverOutline>("HoverOutline");
            Assertions.AssertThat(hoverOutline).IsNotNull();
            Assertions.AssertThat(hoverOutline.Visible)
                .OverrideFailureMessage("HoverOutline should start invisible")
                .IsFalse();

            AssertScreenshot("initial.png");

            var mouseEnterEvent = new InputEventMouseMotion() {
                Position = new Vector2(51, 51),
                GlobalPosition = new Vector2(51, 51),
            };
            runner.Scene().GetViewport().PushInput(mouseEnterEvent);
            await runner.SimulateFrames(1);

            Assertions.AssertThat(hoverOutline.Visible)
                .OverrideFailureMessage("HoverOutline should be visible after mouse enter")
                .IsTrue();

            AssertScreenshot("mouse_enter.png");

            var mouseExitEvent = new InputEventMouseMotion() {
                Position = new Vector2(0, 0),
                GlobalPosition = new Vector2(0, 0),
            };
            runner.Scene().GetViewport().PushInput(mouseExitEvent);
            await runner.SimulateFrames(1);

            Assertions.AssertThat(hoverOutline.Visible)
                .OverrideFailureMessage("HoverOutline should be invisible after mouse exit")
                .IsFalse();

            AssertScreenshot("mouse_leave.png");
            
        }
        catch (Exception e) {
            Assertions.AssertThat(true)
                .OverrideFailureMessage($"Error: {e.Message}\n{e.StackTrace}")
                .IsFalse();
        }
    }
}
