using GdUnit4.Examples.Basics.Setup.Sources.UI;
using Godot;
using NewGameProject.Tests.XUnit;
using Vector2 = Godot.Vector2;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_12.Screen;

[TestSuite]
public class TestClass : Steps {
    // LoadTestScene forces a deterministic 1000x1000 window.
    const float Vp = 1000f;

    /// The effective (global) ZIndex of a canvas item: the sum of the
    /// relative ZIndex values from the item up to the scene root (Godot's
    /// C# binding has no GetGlobalZIndex).
    static int EffectiveZ(Node node)
    {
        var z = 0;
        for (var cur = node; cur != null; cur = cur.GetParent())
            if (cur is CanvasItem ci)
                z += ci.ZIndex;
        return z;
    }

    
    [TestCategory("Step_12")]
    [TestCase(Timeout = 10_000)]
    [RequireGodotRuntime]
    public async Task Given_set_screen_it_should_fill_the_viewport_below_buttons_and_windows() {
        try {
            AddFileToArchive("module/index.js", "index.js")
                .AddFileToArchive("module/manifest.json", "manifest.json")
                .AddFileToArchive("module/texture.png", "texture.png")
                .EnsureDllAccessible()
                .ProcessArchive();

            var scene = await AttachUiScene();

            var screen = scene.Window("__ui-screen-1");
            AssertPanelThat(screen)
                .IsPositionEqual(0, 0)
                .ViewportIsSize(Vp, Vp)
                .HasBackgroundTexture()
                .HasChildPanelNamed("s1-target");

            // Z order: screen (hosted on uiLayers at z 0) below the app
            // buttons (coreUI z1) below the module windows (moduleUI z2).
            var button = runner.Scene().GetNode<Button>("uiLayers/coreUI/SettingsButton");
            var win = scene.Window("top-win");
            Assertions.AssertThat(EffectiveZ(screen))
                .OverrideFailureMessage("the screen must render at global z 0 (below app buttons)")
                .IsEqual(0);
            Assertions.AssertThat(EffectiveZ(button))
                .OverrideFailureMessage("app buttons must render at global z 1")
                .IsEqual(1);
            Assertions.AssertThat(EffectiveZ(win))
                .OverrideFailureMessage("module windows must render at global z 2 (above the screen)")
                .IsEqual(2);

            ClearSimulatedMouse();
        }
        catch (Exception e) {
            Assertions.AssertThat(true)
                .OverrideFailureMessage($"Error: {e.Message}\n{e.StackTrace}")
                .IsFalse();
        }
    }

    [TestCategory("Step_12")]
    [TestCase(Timeout = 10_000)]
    [RequireGodotRuntime]
    public async Task Given_set_screen_the_viewport_should_match_the_reference() {
        try {
            AddFileToArchive("module/index.js", "index.js")
                .AddFileToArchive("module/manifest.json", "manifest.json")
                .AddFileToArchive("module/texture.png", "texture.png")
                .EnsureDllAccessible()
                .ProcessArchive();

            var scene = await AttachUiScene();

            // The screen fills the viewport, so this captures the whole
            // composition: the screen and its child below, the app buttons
            // and the module window above.
            scene.AssertPanelThat("__ui-screen-1")
                .IsPositionEqual(0, 0)
                .ViewportIsSize(Vp, Vp)
                .ViewportMatches("screen-1.png");

            ClearSimulatedMouse();
        }
        catch (Exception e) {
            Assertions.AssertThat(true)
                .OverrideFailureMessage($"Error: {e.Message}\n{e.StackTrace}")
                .IsFalse();
        }
    }

    [TestCategory("Step_12")]
    [TestCase(Timeout = 10_000)]
    [RequireGodotRuntime]
    public async Task Given_screen_child_click_it_should_replace_the_current_screen() {
        try {
            AddFileToArchive("module/index.js", "index.js")
                .AddFileToArchive("module/manifest.json", "manifest.json")
                .AddFileToArchive("module/texture.png", "texture.png")
                .EnsureDllAccessible()
                .ProcessArchive();

            var scene = await AttachUiScene();

            var target = scene.Window("s1-target");
            Click(target.GlobalPosition + new Vector2(1, 1));
            await runner.SimulateFrames(2);

            // The previous screen (and its subtree) is dropped from the view...
            Assertions.AssertThat(scene.GetWindowOrNull("__ui-screen-1"))
                .OverrideFailureMessage("the old screen must be replaced")
                .IsNull();
            Assertions.AssertThat(scene.GetWindowOrNull("s1-target"))
                .OverrideFailureMessage("the old screen's child must be gone")
                .IsNull();
            // ...and the new screen fills the viewport with its content.
            var next = scene.Window("__ui-screen-2");
            AssertPanelThat(next)
                .IsPositionEqual(0, 0)
                .ViewportIsSize(Vp, Vp)
                .HasChildPanelNamed("s2-label", c => c.HasContentText("screen-b"));
            Assertions.AssertThat(EffectiveZ(next))
                .OverrideFailureMessage("the replacement screen must render at global z 0")
                .IsEqual(0);

            ClearSimulatedMouse();
        }
        catch (Exception e) {
            Assertions.AssertThat(true)
                .OverrideFailureMessage($"Error: {e.Message}\n{e.StackTrace}")
                .IsFalse();
        }
    }

    [TestCategory("Step_12")]
    [TestCase(Timeout = 10_000)]
    [RequireGodotRuntime]
    public async Task Given_button_above_screen_child_click_it_should_not_replace_the_screen() {
        try {
            AddFileToArchive("module/index.js", "index.js")
                .AddFileToArchive("module/manifest.json", "manifest.json")
                .AddFileToArchive("module/texture.png", "texture.png")
                .EnsureDllAccessible()
                .ProcessArchive();

            var scene = await AttachUiScene();

            var target = scene.Window("s1-target");
            // An app button (coreUI, global z 1) drawn over the screen child
            // (global z 0): it must win the click.
            var coreUI = runner.Scene().GetNode<Control>("uiLayers/coreUI");
            var button = new Button { Name = "OverlayButton" };
            button.Position = target.GlobalPosition;
            button.Size = target.Size;
            var wasPressed = false;
            button.Pressed += () => wasPressed = true;
            coreUI.AddChild(button);
            await runner.SimulateFrames(1);

            Click(target.GlobalPosition + new Vector2(1, 1));
            await runner.SimulateFrames(2);

            Assertions.AssertBool(wasPressed)
                .OverrideFailureMessage("the button above the screen must receive the click")
                .IsTrue();
            // The screen child never saw the click: the screen is unchanged.
            AssertPanelThat(scene.Window("__ui-screen-1")).HasChildPanelNamed("s1-target");
            Assertions.AssertThat(scene.GetWindowOrNull("__ui-screen-2"))
                .OverrideFailureMessage("the screen must not swap when a button above it is clicked")
                .IsNull();

            ClearSimulatedMouse();
        }
        catch (Exception e) {
            Assertions.AssertThat(true)
                .OverrideFailureMessage($"Error: {e.Message}\n{e.StackTrace}")
                .IsFalse();
        }
    }

    /// <summary>
    /// Synthetic left-click (press, frame, release) at the given viewport
    /// position. The frame between press and release lets the engine service
    /// the press (onClick handlers run on press) before the release lands.
    /// </summary>
    async Task Click(Vector2 globalPos) {
        var press = new InputEventMouseButton {
            Position = globalPos,
            GlobalPosition = globalPos,
            ButtonIndex = MouseButton.Left,
            Pressed = true,
            ButtonMask = MouseButtonMask.Left
        };
        runner.Scene().GetViewport().PushInput(press);
        await runner.SimulateFrames(1);
        var release = (InputEventMouseButton)press.Duplicate();
        release.Pressed = false;
        release.ButtonMask = 0;
        runner.Scene().GetViewport().PushInput(release);
    }
}
