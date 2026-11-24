using Godot;
using NewGameProject.Tests.XUnit;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_16.WindowPositioning;

[TestSuite]
public partial class TestClass : Steps
{
    async Task<AssertScene> Setup()
    {
        AddFileToArchive("module/index.js", "index.js").AddFileToArchive("module/manifest.json", "manifest.json")
            .EnsureDllAccessible().ProcessArchive();
        SimulateMouse(Vector2.Zero);
        var scene = await AttachUiScene();
        await runner.SimulateFrames(5);
        return scene;
    }

    void Click(Vector2 p)
    {
        var viewport = runner.Scene().GetViewport();
        viewport.PushInput(new InputEventMouseButton { Position = p, GlobalPosition = p, ButtonIndex = MouseButton.Left, Pressed = true, ButtonMask = MouseButtonMask.Left });
        viewport.PushInput(new InputEventMouseButton { Position = p, GlobalPosition = p, ButtonIndex = MouseButton.Left, Pressed = false });
    }

    [TestCase, RequireGodotRuntime]
    public async Task Link_follows_target_movement()
    {
        try {
            var scene = await Setup(); var target = scene.Window("target"); var linked = scene.Window("linked");
            await AssertPanelThat(linked).AssertRenderMatches("move_before.png");
            var before = linked.GlobalPosition;
            Assertions.AssertThat(before == target.GlobalPosition + target.Size + new Vector2(7, 9)).IsTrue();
            Click(scene.Window("move").GlobalPosition + new Vector2(5, 5)); await runner.SimulateFrames(10);
            Assertions.AssertThat(target.GlobalPosition == new Vector2(130, 120)).IsTrue();
            Assertions.AssertThat(linked.GlobalPosition == before + new Vector2(30, 20)).IsTrue();
        } finally { ClearSimulatedMouse(); }
    }

    [TestCase, RequireGodotRuntime]
    public async Task Link_follows_target_resize()
    {
        try {
            var scene = await Setup(); var target = scene.Window("target"); var linked = scene.Window("linked");
            await AssertPanelThat(linked).AssertRenderMatches("resize_before.png");
            Click(scene.Window("resize").GlobalPosition + new Vector2(5, 5)); await runner.SimulateFrames(10);
            Assertions.AssertThat(target.Size == new Vector2(140, 105)).IsTrue();
            Assertions.AssertThat(linked.GlobalPosition == target.GlobalPosition + target.Size + new Vector2(7, 9)).IsTrue();
        } finally { ClearSimulatedMouse(); }
    }

    [TestCase, RequireGodotRuntime]
    public async Task Cursor_follows_simulated_mouse()
    {
        try {
            var scene = await Setup(); var window = scene.Window("cursor");
            await AssertPanelThat(window).AssertRenderMatches("cursor_before.png");
            HoverAt(new Vector2(250, 250)); await runner.SimulateFrames(5);
            await AssertPanelThat(window).AssertRenderMatches("cursor_middle.png");
            Assertions.AssertThat(window.GlobalPosition == new Vector2(260, 260)).IsTrue();
            HoverAt(new Vector2(350, 350)); await runner.SimulateFrames(5);
            Assertions.AssertThat(window.GlobalPosition == new Vector2(360, 360)).IsTrue();
            await AssertPanelThat(window).AssertRenderMatches("cursor_after.png");
        } finally { ClearSimulatedMouse(); }
    }

    [TestCase, RequireGodotRuntime]
    public async Task Drag_overrides_link_across_layout_passes()
    {
        try {
            var scene = await Setup(); var window = scene.Window("linked");
            await AssertPanelThat(window).AssertRenderMatches("drag_before.png"); var before = window.GlobalPosition;
            var p = before + new Vector2(10, 10); var q = p + new Vector2(35, 25); var viewport = runner.Scene().GetViewport();
            viewport.PushInput(new InputEventMouseButton { Position = p, GlobalPosition = p, ButtonIndex = MouseButton.Left, Pressed = true, ButtonMask = MouseButtonMask.Left });
            viewport.PushInput(new InputEventMouseMotion { Position = q, GlobalPosition = q, Relative = q - p });
            viewport.PushInput(new InputEventMouseButton { Position = q, GlobalPosition = q, ButtonIndex = MouseButton.Left, Pressed = false });
            await runner.SimulateFrames(3); var moved = window.GlobalPosition;
            Assertions.AssertThat(moved != before).IsTrue();
            Click(scene.Window("move").GlobalPosition + new Vector2(5, 5)); await runner.SimulateFrames(10);
            Assertions.AssertThat(window.GlobalPosition == moved).IsTrue();
        } finally { ClearSimulatedMouse(); }
    }

    [TestCase, RequireGodotRuntime]
    public async Task Missing_target_falls_back_to_parent_origin()
    {
        try {
            var scene = await Setup(); var parent = scene.Window("parent"); var missing = scene.Window("missing");
            await AssertPanelThat(missing).AssertRenderMatches("missing_before.png");
            Assertions.AssertThat(missing.GlobalPosition == parent.GlobalPosition + new Vector2(12, 14)).IsTrue();
            await runner.SimulateFrames(10);
            Assertions.AssertThat(missing.GlobalPosition == parent.GlobalPosition + new Vector2(12, 14)).IsTrue();
        } finally { ClearSimulatedMouse(); }
    }
}
