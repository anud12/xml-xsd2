using Godot;
using RuntimeInterop = NewGameProject.Runtime.RuntimeInterop;
using NewGameProject.Tests.XUnit;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_20.HoverWindowDataUpdate;

[TestSuite]
public partial class TestClass : Steps
{
    void Toggle()
    {
        var p = new Vector2(305, 105);
        var viewport = runner.Scene().GetViewport();
        viewport.PushInput(new InputEventMouseButton { Position = p, GlobalPosition = p, ButtonIndex = MouseButton.Left, Pressed = true, ButtonMask = MouseButtonMask.Left });
        viewport.PushInput(new InputEventMouseButton { Position = p, GlobalPosition = p, ButtonIndex = MouseButton.Left, Pressed = false });
        RuntimeInterop.RunIteration();
    }

    [TestCase, RequireGodotRuntime]
    public async Task Stationary_hover_rechecks_changed_data()
    {
        try
        {
            AddFileToArchive("module/index.js", "index.js").AddFileToArchive("module/manifest.json", "manifest.json")
                .EnsureDllAccessible().ProcessArchive();
            SimulateMouse(Vector2.Zero);
            var scene = await AttachUiScene();
            await runner.SimulateFrames(5);
            AssertScreenshot("data_before.png");
            HoverAt(new Vector2(110, 110));
            await runner.SimulateFrames(5);
            AssertScreenshot("data_hover.png");
            Assertions.AssertThat(scene.Root.FindWindowById("source:hover-window")).IsNull();
            Toggle();
            await runner.SimulateFrames(15);
            await AssertPanelThat(scene.Root.FindWindowById("source:hover-window")!).AssertRenderMatches("data_visible.png");
            var tooltip = scene.Root.FindWindowById("source:hover-window");
            Assertions.AssertThat(tooltip).IsNotNull();
            if (tooltip != null) Assertions.AssertThat(tooltip.GlobalPosition).IsEqual(new Vector2(122, 122));
            Toggle();
            await runner.SimulateFrames(15);
            AssertScreenshot("data_removed.png");
            Assertions.AssertThat(scene.Root.FindWindowById("source:hover-window")).IsNull();
            Toggle();
            await runner.SimulateFrames(15);
            await AssertPanelThat(scene.Root.FindWindowById("source:hover-window")!).AssertRenderMatches("data_restored.png");
            HoverAt(Vector2.Zero);
            await runner.SimulateFrames(10);
            Assertions.AssertThat(scene.Root.FindWindowById("source:hover-window")).IsNull();
            AssertScreenshot("data_exited.png");
            HoverAt(new Vector2(110, 110));
            await runner.SimulateFrames(10);
            Assertions.AssertThat(scene.Root.FindWindowById("source:hover-window")).IsNotNull();
            await AssertPanelThat(scene.Root.FindWindowById("source:hover-window")!).AssertRenderMatches("data_after.png");
        }
        finally { ClearSimulatedMouse(); }
    }
}
