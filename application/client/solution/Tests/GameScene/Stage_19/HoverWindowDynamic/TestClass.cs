using Godot;
using NewGameProject.Tests.XUnit;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_19.HoverWindowDynamic;

[TestSuite]
public partial class TestClass : Steps
{
    void Toggle() => ClickAt(new Vector2(305, 105));

    void ClickAt(Vector2 p)
    {
        var viewport = runner.Scene().GetViewport();
        viewport.PushInput(new InputEventMouseButton { Position = p, GlobalPosition = p, ButtonIndex = MouseButton.Left, Pressed = true, ButtonMask = MouseButtonMask.Left });
        viewport.PushInput(new InputEventMouseButton { Position = p, GlobalPosition = p, ButtonIndex = MouseButton.Left, Pressed = false });
    }

    [TestCase, RequireGodotRuntime]
    public async Task Full_paint_after_capability_gain_creates_tooltip_without_motion()
    {
        try
        {
            AddFileToArchive("module/index.js", "index.js").AddFileToArchive("module/manifest.json", "manifest.json")
                .EnsureDllAccessible().ProcessArchive();
            SimulateMouse(Vector2.Zero);
            var scene = await AttachUiScene();
            await runner.SimulateFrames(5);
            AssertScreenshot("full_paint_before.png");
            HoverAt(new Vector2(110, 110));
            await runner.SimulateFrames(5);
            AssertScreenshot("full_paint_hover.png");
            Assertions.AssertThat(scene.Root.FindWindowById("source:hover-window")).IsNull();
            Toggle();
            scene.Root.ForceFullPaint();
            await runner.SimulateFrames(15);
            await AssertPanelThat(scene.Root.FindWindowById("source:hover-window")!).AssertRenderMatches("full_paint_visible.png");
            var tooltip = scene.Root.FindWindowById("source:hover-window");
            Assertions.AssertThat(tooltip).IsNotNull();
            if (tooltip != null) Assertions.AssertThat(tooltip.GlobalPosition == new Vector2(122, 122)).IsTrue();
            await AssertPanelThat(scene.Root.FindWindowById("source:hover-window")!).AssertRenderMatches("full_paint_after.png");
        }
        finally { ClearSimulatedMouse(); }
    }

    [TestCase, RequireGodotRuntime]
    public async Task Added_and_removed_hover_window_updates_without_pointer_motion()
    {
        try
        {
            AddFileToArchive("module/index.js", "index.js").AddFileToArchive("module/manifest.json", "manifest.json")
                .EnsureDllAccessible().ProcessArchive();
            SimulateMouse(Vector2.Zero);
            var scene = await AttachUiScene();
            await runner.SimulateFrames(5);
            AssertScreenshot("dynamic_before.png");
            HoverAt(new Vector2(110, 110));
            await runner.SimulateFrames(5);
            AssertScreenshot("dynamic_hover.png");
            Assertions.AssertThat(scene.Root.FindWindowById("source:hover-window")).IsNull();
            Toggle();
            await runner.SimulateFrames(15);
            await AssertPanelThat(scene.Root.FindWindowById("source:hover-window")!).AssertRenderMatches("dynamic_visible.png");
            var tooltip = scene.Root.FindWindowById("source:hover-window");
            Assertions.AssertThat(tooltip).IsNotNull();
            if (tooltip != null) Assertions.AssertThat(tooltip.GlobalPosition == new Vector2(122, 122)).IsTrue();
            ClickAt(new Vector2(405, 105));
            await runner.SimulateFrames(15);
            await AssertPanelThat(scene.Root.FindWindowById("source:hover-window")!).AssertRenderMatches("dynamic_updated.png");
            Assertions.AssertThat(scene.Root.FindWindowById("source:hover-window") == tooltip).IsTrue();
            Toggle();
            await runner.SimulateFrames(15);
            AssertScreenshot("dynamic_removed.png");
            Assertions.AssertThat(scene.Root.FindWindowById("source:hover-window")).IsNull();
            Toggle();
            await runner.SimulateFrames(15);
            await AssertPanelThat(scene.Root.FindWindowById("source:hover-window")!).AssertRenderMatches("dynamic_restored.png");
            Assertions.AssertThat(scene.Root.FindWindowById("source:hover-window")).IsNotNull();
            HoverAt(Vector2.Zero);
            await runner.SimulateFrames(10);
            Assertions.AssertThat(scene.Root.FindWindowById("source:hover-window")).IsNull();
            AssertScreenshot("dynamic_after.png");
        }
        finally { ClearSimulatedMouse(); }
    }
}
