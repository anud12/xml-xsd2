using GdUnit4.Examples.Basics.Setup.Sources.UI;
using Godot;
using NewGameProject.Tests.XUnit;
using Vector2 = Godot.Vector2;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_3.Resizable.WithHorizontalScrollbar;

/// Reproduces the user report: a resizable panel whose horizontal entity list is
/// WIDER than the panel (a horizontal overflow scroll bar) could not be resized
/// horizontally by dragging its left/right border.
[TestSuite]
public partial class TestClass : Steps
{
    [TestCategory("Step_3")]
    [TestCase(Timeout = 10_000)]
    [RequireGodotRuntime]
    public async Task Given_horizontal_overflow_panel_it_should_resize_from_the_right_edge()
    {
        var scene = await Setup();
        var panel = scene.GetWindowOrNull("resizable-hscroll");
        Assertions.AssertThat(panel != null)
            .OverrideFailureMessage("resizable-hscroll panel should exist")
            .IsTrue();

        AssertPanelThat(panel).ViewportIsSize(200, 100);
        AssertPanelThat(panel).IsPositionEqual(100, 100);

        // Press the middle of the RIGHT edge (local x=197, inside the 6px resize
        // hit zone) and drag right by +50: width must grow 200 -> 250, x fixed.
        Drag(panel, new Vector2(197, 50), new Vector2(50, 0));
        await runner.SimulateFrames(2);

        AssertPanelThat(panel).ViewportIsSize(250, 100);
        AssertPanelThat(panel).IsPositionEqual(100, 100);
        ClearSimulatedMouse();
    }

    [TestCategory("Step_3")]
    [TestCase(Timeout = 10_000)]
    [RequireGodotRuntime]
    public async Task Given_horizontal_overflow_panel_it_should_resize_from_the_left_edge()
    {
        var scene = await Setup();
        var panel = scene.GetWindowOrNull("resizable-hscroll");
        Assertions.AssertThat(panel != null)
            .OverrideFailureMessage("resizable-hscroll panel should exist")
            .IsTrue();

        AssertPanelThat(panel).ViewportIsSize(200, 100);

        // Press the middle of the LEFT edge (local x=3) and drag left by -50:
        // width must grow 200 -> 250 and x move 100 -> 50.
        Drag(panel, new Vector2(3, 50), new Vector2(-50, 0));
        await runner.SimulateFrames(2);

        AssertPanelThat(panel).ViewportIsSize(250, 100);
        AssertPanelThat(panel).IsPositionEqual(50, 100);
        ClearSimulatedMouse();
    }

    private async Task<dynamic> Setup()
    {
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
        return scene;
    }

    void Drag(UiWindow window, Vector2 local, Vector2 offset)
    {
        var viewport = runner.Scene().GetViewport();
        var gPos = window.GlobalPosition + local;
        viewport.PushInput(new InputEventMouseButton
        {
            Position = gPos, GlobalPosition = gPos, ButtonIndex = MouseButton.Left,
            Pressed = true, ButtonMask = MouseButtonMask.Left,
        });
        var gMotion = window.GlobalPosition + local + offset;
        viewport.PushInput(new InputEventMouseMotion
        {
            Position = gMotion, GlobalPosition = gMotion, Relative = offset,
        });
        viewport.PushInput(new InputEventMouseButton
        {
            Position = gMotion, GlobalPosition = gMotion, ButtonIndex = MouseButton.Left,
            Pressed = false, ButtonMask = 0,
        });
    }
}
