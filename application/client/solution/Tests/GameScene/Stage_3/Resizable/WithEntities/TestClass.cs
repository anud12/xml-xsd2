using GdUnit4.Examples.Basics.Setup.Sources.UI;
using Godot;
using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;
using Vector2 = Godot.Vector2;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_3.Resizable.WithEntities;

/// A resizable panel that ALSO carries child entities, rendered as child
/// windows through an <c>entityList</c>. The <c>entityList</c> is a division
/// (container view) that must pass mouse input through so the panel's
/// corner-drag resize hit zone stays reachable. Without that pass-through the
/// entityList would swallow the corner press and the panel could never be
/// resized. This test proves the panel resizes (corner drag) even with its
/// child entities in place.
[TestSuite]
public partial class TestClass : Steps {
    [TestCategory("Step_3")]
    [TestCase(Timeout = 10_000)]
    [RequireGodotRuntime]
    public async Task Given_resizable_panel_with_child_entities_it_should_still_grow_when_dragging_the_corner() {
        try {
            AddFileToArchive("module/index.js", "index.js")
                .AddFileToArchive("module/manifest.json", "manifest.json")
                .AddFileToArchive("module/texture.png", "texture.png")
                .AddFileToArchive("module/item.png", "item.png")
                .EnsureDllAccessible()
                .ProcessArchive();

            var scene = await AttachUiScene();
            // The scene runs with RUN_RUNTIME_LOOP=false: advance the runtime once
            // so the entityList expands (materializing the per-entity child
            // windows) and pump a couple of frames so the windows are real Godot
            // nodes we can target with synthetic mouse input.
            RuntimeInterop.RunIteration();
            await runner.SimulateFrames(2);

            var panel = scene.GetWindowOrNull("resizable-entities");
            Assertions.AssertThat(panel != null)
                .OverrideFailureMessage("resizable-entities panel should exist")
                .IsTrue();

            // The entityList rendered its container's entities as child windows
            // (the panel genuinely has child entities).
            var childA = scene.GetWindowOrNull("ent-a");
            var childB = scene.GetWindowOrNull("ent-b");
            Assertions.AssertThat(childA != null)
                .OverrideFailureMessage("ent-a should be rendered as a child window inside the panel")
                .IsTrue();
            Assertions.AssertThat(childB != null)
                .OverrideFailureMessage("ent-b should be rendered as a child window inside the panel")
                .IsTrue();

            // The panel starts at 200x200 at (350,100).
            AssertPanelThat(panel).ViewportIsSize(200, 200);
            AssertPanelThat(panel).IsPositionEqual(350, 100);

            // ── STAGE 1 · INITIAL (no input) ──
            // Viewport (pixel) baseline: the panel shows its two child entities
            // (the entityList has expanded) on the stretched background. These
            // references are regenerated on the first run and reused below to
            // prove the visuals at the resized moment.
            AssertPanelThat(panel).ViewportMatches("initial.png");
            AssertPanelThat(childA).ViewportMatches("item-a.png");
            AssertPanelThat(childB).ViewportMatches("item-b.png");

            // Press the bottom-right corner (local 198,198 — inside the 6px resize
            // hit zone), drag outward by +50 on both axes, release. The corner
            // press must reach the panel even though it carries a pass-through
            // entityList: the panel grows to 250x250, top-left fixed.
            var corner = new Vector2(198, 198);
            Drag(panel, corner, new Vector2(50, 50));
            await runner.SimulateFrames(2);

            AssertPanelThat(panel).ViewportIsSize(250, 250);
            AssertPanelThat(panel).IsPositionEqual(350, 100);

            // ── STAGE 2 · RESIZED (panel grown to 250x250) ──
            // Viewport (pixel): the panel's larger rect is captured — the
            // background stretches to fill the new 250x250 area and the child
            // entities remain laid out in the (now larger) entityList. Proves
            // the resize is visually in effect on a panel carrying entities.
            AssertPanelThat(panel).ViewportMatches("resized.png");

            DebugSaveScreenshot("result.png");
            ClearSimulatedMouse();
        }
        catch (Exception e) {
            Assertions.AssertThat(true)
                .OverrideFailureMessage($"Error: {e.Message}\n{e.StackTrace}")
                .IsFalse();
        }
    }

    /// Presses the window's local point, drags by the given offset, releases —
    /// pushing synthetic mouse events so the window's GuiInput resize handler
    /// (and the pass-through entityList, which ignores the press) receives them.
    void Drag(UiWindow window, Vector2 local, Vector2 offset)
    {
        var viewport = runner.Scene().GetViewport();
        // The viewport treats the event Position as the viewport-global point
        // (GlobalPosition is overwritten on PushInput), so both fields are the
        // window-local point offset by the window's viewport position.
        var gPos = window.GlobalPosition + local;
        var press = new InputEventMouseButton {
            Position = gPos,
            GlobalPosition = gPos,
            ButtonIndex = MouseButton.Left,
            Pressed = true,
            ButtonMask = MouseButtonMask.Left
        };
        viewport.PushInput(press);
        var gMotion = window.GlobalPosition + local + offset;
        var motion = new InputEventMouseMotion {
            Position = gMotion,
            GlobalPosition = gMotion,
            Relative = offset
        };
        viewport.PushInput(motion);
        var gRelease = window.GlobalPosition + local + offset;
        var release = new InputEventMouseButton {
            Position = gRelease,
            GlobalPosition = gRelease,
            ButtonIndex = MouseButton.Left,
            Pressed = false,
            ButtonMask = 0
        };
        viewport.PushInput(release);
    }
}
