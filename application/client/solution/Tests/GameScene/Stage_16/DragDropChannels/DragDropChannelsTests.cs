using GdUnit4.Examples.Basics.Setup.Sources.UI;
using Godot;
using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;
using Vector2 = Godot.Vector2;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_16.DragDropChannels;

/// Verifies that INDEPENDENT drag channels are isolated from one another.
///
/// The module (Stage_16/DragDropChannels) declares two drag channels —
/// "channel-a" and "channel-b" — each with a draggable source panel and a
/// droppable target panel that accepts ONLY that channel. This test drives a
/// LIVE Godot scene and pushes synthetic mouse input (press → motion → motion
/// → release) so the C# drag state machine in `UiWindow.DragDrop.cs` runs.
///
/// For EACH channel it asserts the "available zones" feedback: while that
/// channel's drag is in flight, the matching target shows the ambient
/// {@link DropFeedbackState.Available} indicator (then
/// {@link DropFeedbackState.Hovered} when the pointer is over it), while the
/// OTHER channel's target stays {@link DropFeedbackState.None} — proving the
/// two channels do not leak into each other. After each release it checks the
/// data path (the entity moved into that channel's bin) and that both targets
/// cleaned up back to None.
///
/// Channel isolation is a *state* property (which targets light up), so it is
/// asserted directly via the {@link UiWindow.FeedbackState} probe. The two drop
/// feedback states use DIFFERENT colors (a cool `onDrag` "available" indicator
/// vs a warm `onHover` "release to drop" highlight) and each also renders a
/// centered "DROP" text label (the feedback `text` + a <c>ui.setFont</c> font).
/// They are captured with pixel-level <c>ViewportMatches</c> references so both
/// the color distinction and the text label are evidenced.
///
/// Requires the Godot runtime (`GODOT_BIN`); without it this test is skipped.
[TestSuite]
public class DragDropChannelsTests : Steps {
    [TestCategory("Stage_16")]
    [TestCase(Timeout = 30_000)]
    [RequireGodotRuntime]
    public async Task it_should_isolate_independent_drag_channels() {
        try {
            AddFileToArchive("module/index.js", "index.js")
                .AddFileToArchive("module/manifest.json", "manifest.json")
                .AddFileToArchive("module/drag_zone.png", "drag_zone.png")
                .AddFileToArchive("module/hover_zone.png", "hover_zone.png")
                .AddFileToArchive("module/panel_bg.png", "panel_bg.png")
                .AddFileToArchive("module/fonts/Ubuntu-Regular.ttf", "fonts/Ubuntu-Regular.ttf")
                .EnsureDllAccessible()
                .ProcessArchive();

            var scene = await AttachUiScene();
            await runner.SimulateFrames(1);

            var sourceA = scene.Window("source-a");
            var sourceB = scene.Window("source-b");
            var targetA = scene.Window("target-a");
            var targetB = scene.Window("target-b");
            var viewport = runner.Scene().GetViewport();

            // ── INITIAL: nothing is dragging, no target shows feedback ──
            Assertions.AssertThat(sourceA.IsDragging).IsFalse();
            Assertions.AssertThat(targetA.FeedbackState)
                .OverrideFailureMessage("target-a should be None before any input").IsEqual(DropFeedbackState.None);
            Assertions.AssertThat(targetB.FeedbackState)
                .OverrideFailureMessage("target-b should be None before any input").IsEqual(DropFeedbackState.None);

            // ════════════════ CHANNEL A (source-a → target-a → bin-a) ════════════════
            // Press on source-a + first motion (cross the 4-px threshold, still
            // over source-a). Press + first motion are pushed in the same frame
            // (the proven Stage_3/Resizable.Drag pattern) so the button-pressed
            // state is preserved across the threshold-crossing motion.
            var pressA = sourceA.GlobalPosition + sourceA.Size / 2f;
            viewport.PushInput(MousePress(pressA, true));
            viewport.PushInput(MouseMotion(pressA, pressA + new Vector2(20, 0)));
            await runner.SimulateFrames(2);

            Assertions.AssertThat(sourceA.IsDragging)
                .OverrideFailureMessage("source-a should report IsDragging once the channel-a drag has started")
                .IsTrue();
            // The matching target lights up (Available); the other channel's
            // target must stay dark — channel isolation.
            Assertions.AssertThat(targetA.FeedbackState)
                .OverrideFailureMessage("target-a should be Available while a channel-a drag is in flight")
                .IsEqual(DropFeedbackState.Available);
            Assertions.AssertThat(targetB.FeedbackState)
                .OverrideFailureMessage("target-b must stay None (channel isolation) while a channel-a drag is in flight")
                .IsEqual(DropFeedbackState.None);
            // Pixel evidence of the "available" (onDrag) state: target-a renders
            // its COOL onDrag indicator (distinct from the onHover color asserted
            // later) plus the centered "DROP" feedback text label. Captured as a
            // reference image beside this file.
            AssertPanelThat(targetA).ViewportMatches("available-target-a.png");

            // Move onto target-a → it becomes Hovered; target-b still None.
            var overA = targetA.GlobalPosition + targetA.Size / 2f;
            viewport.PushInput(MouseMotion(pressA + new Vector2(20, 0), overA));
            await runner.SimulateFrames(2);

            Assertions.AssertThat(sourceA.DragHoverTarget == targetA)
                .OverrideFailureMessage("the hover target should be target-a while the pointer is over it")
                .IsTrue();
            Assertions.AssertThat(targetA.FeedbackState)
                .OverrideFailureMessage("target-a should be Hovered while the pointer is over it")
                .IsEqual(DropFeedbackState.Hovered);
            Assertions.AssertThat(targetB.FeedbackState)
                .OverrideFailureMessage("target-b must stay None (channel isolation) while hovering target-a")
                .IsEqual(DropFeedbackState.None);
            // Pixel evidence of the "hovered" (onHover) state: target-a now
            // renders its WARM onHover indicator — a different color from the
            // cool onDrag "available" image captured above (both still carry the
            // "DROP" label), so the two drop states are visibly distinguishable.
            AssertPanelThat(targetA).ViewportMatches("hover-target-a.png");

            // Release over target-a → the channel-a onDrop fires (item-a → bin-a).
            viewport.PushInput(MousePress(overA, false));
            await runner.SimulateFrames(2);
            RuntimeInterop.RunIteration();

            // Both targets clean up back to None; item-a moved tray-a → bin-a.
            Assertions.AssertThat(sourceA.IsDragging)
                .OverrideFailureMessage("source-a should no longer report IsDragging after the channel-a drop")
                .IsFalse();
            Assertions.AssertThat(targetA.FeedbackState)
                .OverrideFailureMessage("target-a should be None after the channel-a drop")
                .IsEqual(DropFeedbackState.None);
            Assertions.AssertThat(targetB.FeedbackState)
                .OverrideFailureMessage("target-b should be None after the channel-a drop")
                .IsEqual(DropFeedbackState.None);
            Assertions.AssertThat(EntityInterop.GetEntityById("tray-a")!.Members("members").Contains("item-a"))
                .OverrideFailureMessage("item-a should no longer be in tray-a after the channel-a drop")
                .IsFalse();
            Assertions.AssertThat(EntityInterop.GetEntityById("bin-a")!.Members("members").Contains("item-a"))
                .OverrideFailureMessage("item-a should be in bin-a after the channel-a drop")
                .IsTrue();
            AssertRuntimeOutputContains("___channel-a onDrop item-a___");

            // Let the scene settle before starting the second channel's drag.
            await runner.SimulateFrames(1);

            // ════════════════ CHANNEL B (source-b → target-b → bin-b) ════════════════
            var pressB = sourceB.GlobalPosition + sourceB.Size / 2f;
            viewport.PushInput(MousePress(pressB, true));
            viewport.PushInput(MouseMotion(pressB, pressB + new Vector2(20, 0)));
            await runner.SimulateFrames(2);

            Assertions.AssertThat(sourceB.IsDragging)
                .OverrideFailureMessage("source-b should report IsDragging once the channel-b drag has started")
                .IsTrue();
            // Symmetric isolation: target-b lights up, target-a stays dark.
            Assertions.AssertThat(targetB.FeedbackState)
                .OverrideFailureMessage("target-b should be Available while a channel-b drag is in flight")
                .IsEqual(DropFeedbackState.Available);
            Assertions.AssertThat(targetA.FeedbackState)
                .OverrideFailureMessage("target-a must stay None (channel isolation) while a channel-b drag is in flight")
                .IsEqual(DropFeedbackState.None);

            var overB = targetB.GlobalPosition + targetB.Size / 2f;
            viewport.PushInput(MouseMotion(pressB + new Vector2(20, 0), overB));
            await runner.SimulateFrames(2);

            Assertions.AssertThat(sourceB.DragHoverTarget == targetB)
                .OverrideFailureMessage("the hover target should be target-b while the pointer is over it")
                .IsTrue();
            Assertions.AssertThat(targetB.FeedbackState)
                .OverrideFailureMessage("target-b should be Hovered while the pointer is over it")
                .IsEqual(DropFeedbackState.Hovered);
            Assertions.AssertThat(targetA.FeedbackState)
                .OverrideFailureMessage("target-a must stay None (channel isolation) while hovering target-b")
                .IsEqual(DropFeedbackState.None);

            // Release over target-b → the channel-b onDrop fires (item-b → bin-b).
            viewport.PushInput(MousePress(overB, false));
            await runner.SimulateFrames(2);
            RuntimeInterop.RunIteration();

            Assertions.AssertThat(sourceB.IsDragging)
                .OverrideFailureMessage("source-b should no longer report IsDragging after the channel-b drop")
                .IsFalse();
            Assertions.AssertThat(targetB.FeedbackState)
                .OverrideFailureMessage("target-b should be None after the channel-b drop")
                .IsEqual(DropFeedbackState.None);
            Assertions.AssertThat(targetA.FeedbackState)
                .OverrideFailureMessage("target-a should be None after the channel-b drop")
                .IsEqual(DropFeedbackState.None);
            Assertions.AssertThat(EntityInterop.GetEntityById("tray-b")!.Members("members").Contains("item-b"))
                .OverrideFailureMessage("item-b should no longer be in tray-b after the channel-b drop")
                .IsFalse();
            Assertions.AssertThat(EntityInterop.GetEntityById("bin-b")!.Members("members").Contains("item-b"))
                .OverrideFailureMessage("item-b should be in bin-b after the channel-b drop")
                .IsTrue();
            AssertRuntimeOutputContains("___channel-b onDrop item-b___");

            DebugSaveScreenshot("result.png");
            ClearSimulatedMouse();
        }
        catch (Exception e) {
            Assertions.AssertThat(true)
                .OverrideFailureMessage($"Error: {e.Message}\n{e.StackTrace}")
                .IsFalse();
        }
    }

    // Small synthetic-input helpers. Press + first motion are pushed back-to-back
    // (no frame between them) so the button-pressed state survives the
    // threshold-crossing motion — otherwise the motion would not route to the
    // drag source's GuiInput handler.
    static InputEventMouseButton MousePress(Vector2 global, bool pressed) => new() {
        Position = global,
        GlobalPosition = global,
        ButtonIndex = MouseButton.Left,
        Pressed = pressed,
        ButtonMask = pressed ? MouseButtonMask.Left : 0
    };
    static InputEventMouseMotion MouseMotion(Vector2 from, Vector2 to) => new() {
        Position = to,
        GlobalPosition = to,
        Relative = to - from
    };
}
