using GdUnit4.Examples.Basics.Setup.Sources.UI;
using Godot;
using NewGameProject.Tests.XUnit;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_15.PinnableWindow;

[TestSuite]
public partial class TestClass : Steps
{
    const string Id = "hover-source:hover-window";

    async Task<AssertScene> Setup(string name)
    {
        AddFileToArchive("module/index.js", "index.js").AddFileToArchive("module/manifest.json", "manifest.json")
            .AddFileToArchive("module/hover.png", "hover.png").EnsureDllAccessible().ProcessArchive();
        SimulateMouse(Vector2.Zero);
        var scene = await AttachUiScene();
        await runner.SimulateFrames(5);
        AssertScreenshot(name + "_before.png");
        HoverAt(scene.Window("hover-source").GlobalPosition + new Vector2(5, 5));
        await runner.SimulateFrames(10);
        return scene;
    }
    static void KeyPress(RootNode root) => root._UnhandledKeyInput(new InputEventKey { Keycode = Key.P, Pressed = true });
    async Task Exit() { HoverAt(Vector2.Zero); await runner.SimulateFrames(10); }
    void ClickPin(UiWindow window)
    {
        var button = window.GetNode<Button>("__pinIndicator");
        var p = button.GlobalPosition + button.Size / 2;
        var viewport = runner.Scene().GetViewport();
        viewport.PushInput(new InputEventMouseButton { Position = p, GlobalPosition = p, ButtonIndex = MouseButton.Left, Pressed = true, ButtonMask = MouseButtonMask.Left });
        viewport.PushInput(new InputEventMouseButton { Position = p, GlobalPosition = p, ButtonIndex = MouseButton.Left, Pressed = false });
    }

    [TestCase, RequireGodotRuntime]
    public async Task Pin_survives_exit()
    {
        try { var scene = await Setup("pin_survives"); await AssertPanelThat(scene.Root.FindWindowById(Id)!).AssertRenderMatches("pin_survives_hover.png"); var window = scene.Root.FindWindowById(Id); Assertions.AssertThat(window).IsNotNull();
            var position = window!.Position; KeyPress(scene.Root); await runner.SimulateFrames(5); await AssertPanelThat(window).AssertRenderMatches("pin_survives_pinned.png"); await Exit(); await AssertPanelThat(window).AssertRenderMatches("pin_survives_after.png");
            Assertions.AssertThat(scene.Root.FindWindowById(Id) == window).IsTrue(); Assertions.AssertThat(window.Position == position).IsTrue(); }
        finally { ClearSimulatedMouse(); }
    }
    [TestCase, RequireGodotRuntime]
    public async Task Unpin_dismisses_after_exit()
    {
        try { var scene = await Setup("unpin_exit"); await AssertPanelThat(scene.Root.FindWindowById(Id)!).AssertRenderMatches("unpin_exit_hover.png"); var window = scene.Root.FindWindowById(Id)!; KeyPress(scene.Root); await runner.SimulateFrames(5); await AssertPanelThat(window).AssertRenderMatches("unpin_exit_pinned.png"); await Exit(); await AssertPanelThat(window).AssertRenderMatches("unpin_exit_exited.png"); KeyPress(scene.Root);
            Assertions.AssertThat(scene.Root.FindWindowById(Id) == window).IsTrue(); ClickPin(window);
            await runner.SimulateFrames(5); AssertScreenshot("unpin_exit_after.png"); Assertions.AssertThat(scene.Root.FindWindowById(Id)).IsNull(); }
        finally { ClearSimulatedMouse(); }
    }
    [TestCase, RequireGodotRuntime]
    public async Task Unpin_while_hovering_remains_transient()
    {
        try { var scene = await Setup("unpin_hover"); await AssertPanelThat(scene.Root.FindWindowById(Id)!).AssertRenderMatches("unpin_hover_hover.png"); var window = scene.Root.FindWindowById(Id)!; KeyPress(scene.Root); await runner.SimulateFrames(5); await AssertPanelThat(window).AssertRenderMatches("unpin_hover_pinned.png"); KeyPress(scene.Root); ClickPin(window); await runner.SimulateFrames(5); await AssertPanelThat(window).AssertRenderMatches("unpin_hover_unpinned.png");
            Assertions.AssertThat(scene.Root.FindWindowById(Id) == window).IsTrue(); await Exit(); AssertScreenshot("unpin_hover_after.png"); Assertions.AssertThat(scene.Root.FindWindowById(Id)).IsNull(); }
        finally { ClearSimulatedMouse(); }
    }
    [TestCase, RequireGodotRuntime]
    public async Task Indicator_tracks_state()
    {
        try { var scene = await Setup("indicator"); await AssertPanelThat(scene.Root.FindWindowById(Id)!).AssertRenderMatches("indicator_hover.png"); var window = scene.Root.FindWindowById(Id)!;
            var button = window.GetNode<Button>("__pinIndicator"); var label = button.GetNode<Label>("key");
            Assertions.AssertThat(label.Text == "P").IsTrue();
            Assertions.AssertThat(button.AnchorRight == 1 && button.OffsetRight < 0).IsTrue();
            Assertions.AssertThat(label.GetThemeColor("font_color") == Colors.White).IsTrue();
            var pos = window.Position; var p = button.GlobalPosition + button.Size / 2;
            var viewport = runner.Scene().GetViewport();
            void Click() {
                viewport.PushInput(new InputEventMouseButton { Position = p, GlobalPosition = p, ButtonIndex = MouseButton.Left, Pressed = true, ButtonMask = MouseButtonMask.Left });
                viewport.PushInput(new InputEventMouseButton { Position = p, GlobalPosition = p, ButtonIndex = MouseButton.Left, Pressed = false });
            }
            Assertions.AssertThat(button.MouseFilter == Control.MouseFilterEnum.Ignore).IsTrue();
            Click(); Assertions.AssertThat(label.GetThemeColor("font_color") == Colors.White).IsTrue();
            Assertions.AssertThat(window.Position == pos).IsTrue();
            KeyPress(scene.Root); await runner.SimulateFrames(5); await AssertPanelThat(window).AssertRenderMatches("indicator_pinned.png"); Assertions.AssertThat(label.GetThemeColor("font_color") == Colors.Gold).IsTrue();
            Assertions.AssertThat(button.MouseFilter == Control.MouseFilterEnum.Stop).IsTrue();
            KeyPress(scene.Root); Assertions.AssertThat(label.GetThemeColor("font_color") == Colors.Gold).IsTrue();
            Click(); await runner.SimulateFrames(5); await AssertPanelThat(window).AssertRenderMatches("indicator_after.png"); Assertions.AssertThat(label.GetThemeColor("font_color") == Colors.White).IsTrue();
            Assertions.AssertThat(button.MouseFilter == Control.MouseFilterEnum.Ignore).IsTrue(); }
        finally { ClearSimulatedMouse(); }
    }
    [TestCase, RequireGodotRuntime]
    public async Task Mouse_filters_follow_pin_state()
    {
        try
        {
            var scene = await Setup("filters"); await AssertPanelThat(scene.Root.FindWindowById(Id)!).AssertRenderMatches("filters_hover.png");
            var window = scene.Root.FindWindowById(Id)!;
            var title = window.GetNode<Control>("__titleBar");
            void AssertIgnored(Node node)
            {
                if (node is Control control)
                    Assertions.AssertThat(control.MouseFilter == Control.MouseFilterEnum.Ignore).IsTrue();
                foreach (var child in node.GetChildren()) AssertIgnored(child);
            }
            AssertIgnored(window);
            KeyPress(scene.Root); await runner.SimulateFrames(5); await AssertPanelThat(window).AssertRenderMatches("filters_pinned.png");
            Assertions.AssertThat(title.MouseFilter == Control.MouseFilterEnum.Stop).IsTrue();
            KeyPress(scene.Root);
            Assertions.AssertThat(title.MouseFilter == Control.MouseFilterEnum.Stop).IsTrue();
            ClickPin(window);
            await runner.SimulateFrames(5); await AssertPanelThat(window).AssertRenderMatches("filters_after.png");
            AssertIgnored(window);
        }
        finally { ClearSimulatedMouse(); }
    }
    [TestCase, RequireGodotRuntime]
    public async Task Drag_position_survives_pin_cycle()
    {
        try { var scene = await Setup("drag_cycle"); await AssertPanelThat(scene.Root.FindWindowById(Id)!).AssertRenderMatches("drag_cycle_hover.png"); var window = scene.Root.FindWindowById(Id)!; KeyPress(scene.Root); await runner.SimulateFrames(5); await AssertPanelThat(window).AssertRenderMatches("drag_cycle_pinned.png");
            var viewport = runner.Scene().GetViewport(); var p = window.GlobalPosition + new Vector2(10, 10); var q = p + new Vector2(30, 20);
            viewport.PushInput(new InputEventMouseButton { Position = p, GlobalPosition = p, ButtonIndex = MouseButton.Left, Pressed = true, ButtonMask = MouseButtonMask.Left });
            viewport.PushInput(new InputEventMouseMotion { Position = q, GlobalPosition = q, Relative = q - p });
            viewport.PushInput(new InputEventMouseButton { Position = q, GlobalPosition = q, ButtonIndex = MouseButton.Left, Pressed = false });
            await runner.SimulateFrames(5); await AssertPanelThat(window).AssertRenderMatches("drag_cycle_moved.png"); var moved = window.Position; Assertions.AssertThat(moved != new Vector2(300, 300)).IsTrue();
            KeyPress(scene.Root); ClickPin(window); await runner.SimulateFrames(5); await AssertPanelThat(window).AssertRenderMatches("drag_cycle_after.png"); Assertions.AssertThat(window.Position == moved).IsTrue(); }
        finally { ClearSimulatedMouse(); }
    }
}
