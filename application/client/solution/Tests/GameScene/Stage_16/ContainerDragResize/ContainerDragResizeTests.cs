using GdUnit4.Examples.Basics.Setup.Sources.UI;
using Godot;
using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;
using Vector2 = Godot.Vector2;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_16.ContainerDragResize;

/// A container window that is BOTH resizable and a drag-and-drop target. The
/// test resizes the drop target (`container_2`) with a client-side corner drag
/// and then drives a live mouse drag of `item-1` from `container_1` onto the
/// *resized* `container_2`, proving the drop still resolves to that window's
/// container. It also proves the client-side resize *survives* the repaint
/// that the drop triggers (the window's `_userSize` is honored over the
/// declared size on re-apply), so the window stays its resized size.
///
/// Both container windows are the "resizable + entityList + droppable"
/// combination: each carries an <c>entityList</c> (a division / container view)
/// that renders its container's entities as draggable child windows. The
/// entityList division passes mouse input through (<c>MouseFilter.Pass</c>),
/// so the window's corner-drag resize hit zone is still reachable even with
/// the entityList in place. The drop still resolves to the target because the
/// channel's <c>onDrop</c> finds the destination container BY id
/// (<c>ctx.targetId == window id</c>), independent of the entityList rendering.
/// After the drop, `container_2`'s entityList renders the moved `item-1`.
///
 /// Like the ContainerDrag test, this drives a LIVE Godot scene and pushes
 /// synthetic mouse input. Every stage is highlighted with a pixel-level
 /// <c>ViewportMatches</c> capture of the container windows (initial / resized /
 /// dragging-over-source / over-resized-destination / released-final), in
 /// addition to the numeric resize asserts (<c>ViewportIsSize</c> /
 /// <c>IsPositionEqual</c>), the drag state machine (<c>IsDragging</c>,
 /// <c>DragHoverTarget</c>, <c>IsDropFeedbackVisible</c>, <c>FeedbackState</c>),
 /// and the data path (<c>EntityInterop</c>: the entity moved between
 /// containers). The reference images live beside this file.
///
/// Requires the Godot runtime (<c>GODOT_BIN</c>); without it this test is skipped.
[TestSuite]
public class ContainerDragResizeTests : Steps {
    [TestCategory("Stage_16")]
    [TestCase(Timeout = 30_000)]
    [RequireGodotRuntime]
    public async Task it_should_still_drop_an_entity_onto_a_resized_container_window() {
        try {
            AddFileToArchive("module/index.js", "index.js")
                .AddFileToArchive("module/manifest.json", "manifest.json")
                .AddFileToArchive("module/drop_highlight.png", "drop_highlight.png")
                .AddFileToArchive("module/panel_bg.png", "panel_bg.png")
                .AddFileToArchive("module/item_bg.png", "item_bg.png")
                .AddFileToArchive("module/fonts/Ubuntu-Regular.ttf", "fonts/Ubuntu-Regular.ttf")
                .EnsureDllAccessible()
                .ProcessArchive();

            // Initial data: item-1 + item-2 start in container_1; container_2 empty.
            var container1 = EntityInterop.GetEntityById("container_1")!;
            var container2 = EntityInterop.GetEntityById("container_2")!;
            Assertions.AssertThat(container1.Members("members").Length).IsEqual(2);
            Assertions.AssertThat(container1.Members("members").Contains("item-1")).IsTrue();
            Assertions.AssertThat(container2.Members("members").Length).IsEqual(0);

            var scene = await AttachUiScene();
            // The scene runs with RUN_RUNTIME_LOOP=false: advance the runtime once
            // so the container entityLists expand (materializing the per-entity
            // child windows) and pump a couple of frames so the windows are real
            // Godot nodes we can target with synthetic mouse input.
            RuntimeInterop.RunIteration();
            await runner.SimulateFrames(2);

            var source = scene.GetWindowOrNull("item-1");
            Assertions.AssertThat(source != null)
                .OverrideFailureMessage("item-1 should be rendered as a child window inside container_1")
                .IsTrue();
            var c1 = scene.GetWindowOrNull("container_1");
            Assertions.AssertThat(c1 != null)
                .OverrideFailureMessage("container_1 window should exist")
                .IsTrue();
            var target = scene.GetWindowOrNull("container_2");
            Assertions.AssertThat(target != null)
                .OverrideFailureMessage("container_2 window should exist")
                .IsTrue();
            var sourceNode = source!;
            var c1Node = c1!;
            var targetNode = target!;

            // ── STAGE 1 · INITIAL (no input) ──
            // Viewport (pixel) baseline: container_1 shows item-1 + item-2; the
            // drop target container_2 is an empty 220x300 panel. These reference
            // images are regenerated on the first run and reused below to prove
            // the visuals at each subsequent moment.
            AssertPanelThat(c1Node).ViewportMatches("initial-container_1.png");
            AssertPanelThat(targetNode).ViewportMatches("initial-container_2.png");

            // ── STEP 1 · RESIZE the drop target (container_2) ──
            // The window starts at 220x300 at (320,100). Press the bottom-right
            // corner (local 218,298 — inside the 6px resize hit zone) and drag
            // outward by +80 on both axes: it grows to 300x380, top-left fixed.
            var originalSize = targetNode.Size;
            Assertions.AssertThat(originalSize.X).IsEqual(220f);
            Assertions.AssertThat(originalSize.Y).IsEqual(300f);

            var viewport = runner.Scene().GetViewport();
            // Press the bottom-right corner (local 218,298 — inside the 6px resize
            // hit zone) and drag outward by +80 on both axes: press + motion +
            // release in a single frame (the proven Stage_3/Resizable.Drag
            // pattern), so the corner press reaches the window's resize handler
            // and the window grows to 300x380 with its top-left fixed.
            Drag(targetNode, new Vector2(218, 298), new Vector2(80, 80));
            await runner.SimulateFrames(2);

            AssertPanelThat(targetNode).ViewportIsSize(300, 380);
            AssertPanelThat(targetNode).IsPositionEqual(320, 100);

            // ── STAGE 2 · RESIZED (container_2 grown to 300x380) ──
            // Viewport (pixel): the window's larger rect is captured — the panel
            // background stretches to fill the new 300x380 area (the client-side
            // resize is visually in effect before any drag).
            AssertPanelThat(targetNode).ViewportMatches("resized-container_2.png");

            // ── STEP 2 · DRAG item-1 onto the RESIZED container_2 ──
            var pressGlobal = sourceNode.GlobalPosition + sourceNode.Size / 2f;
            // The release point is the *resized* window's center — the new,
            // larger rect is the drop hit-area.
            var releaseGlobal = targetNode.GlobalPosition + targetNode.Size / 2f;

            // Press LMB on the item-1 child window and move a little while the
            // pointer is still over it: far enough to cross the 4-px
            // DragStartThreshold so the drag begins, but not far enough to reach
            // container_2. Press + first motion are pushed in the same frame (the
            // proven pattern from Stage_3/Resizable.Drag): pumping a frame between
            // them drops the button-pressed state, so the motion would not route
            // to the window's GuiInput handler.
            viewport.PushInput(new InputEventMouseButton {
                Position = pressGlobal,
                GlobalPosition = pressGlobal,
                ButtonIndex = MouseButton.Left,
                Pressed = true,
                ButtonMask = MouseButtonMask.Left
            });
            var midGlobal = pressGlobal + new Vector2(20, 0);
            viewport.PushInput(new InputEventMouseMotion {
                Position = midGlobal,
                GlobalPosition = midGlobal,
                Relative = midGlobal - pressGlobal
            });
            await runner.SimulateFrames(2);

            // The drag has started (pointer still over the source); the resized
            // container_2 already advertises itself as an available target.
            Assertions.AssertThat(sourceNode.IsDragging)
                .OverrideFailureMessage("item-1 should report IsDragging once the drag has started")
                .IsTrue();
            Assertions.AssertThat(targetNode.FeedbackState)
                .OverrideFailureMessage("the resized container_2 should be Available while still over the source")
                .IsEqual(DropFeedbackState.Available);

            // ── STAGE 3 · DRAGGING (pointer still over the source) ──
            // Viewport (pixel): the translucent drag ghost renders over item-1
            // inside container_1 (which also shows its 'available' indicator),
            // and the resized container_2 shows its 'available' indicator.
            AssertPanelThat(c1Node).ViewportMatches("dragging-container_1.png");
            AssertPanelThat(targetNode).ViewportMatches("available-container_2.png");

            // Move the pointer to the *resized* container_2's center: it is now
            // over the drop target. The drag is already active, so the motion
            // routes to the source's drag handler, which updates the hover target.
            viewport.PushInput(new InputEventMouseMotion {
                Position = releaseGlobal,
                GlobalPosition = releaseGlobal,
                Relative = releaseGlobal - midGlobal
            });
            await runner.SimulateFrames(2);

            // Hovering the resized window resolves to it (its larger rect is the
            // hit-area) and shows the drop-feedback overlay.
            Assertions.AssertThat(sourceNode.IsDragging)
                .OverrideFailureMessage("item-1 should still report IsDragging while over the resized destination")
                .IsTrue();
            Assertions.AssertThat(sourceNode.DragHoverTarget == targetNode)
                .OverrideFailureMessage("the hover target should be the resized container_2 while the pointer is over it")
                .IsTrue();
            Assertions.AssertThat(targetNode.IsDropFeedbackVisible)
                .OverrideFailureMessage("the resized container_2's drop-feedback overlay should be visible")
                .IsTrue();

            // ── STAGE 4 · OVER THE RESIZED DESTINATION ──
            // Viewport (pixel): the drop-feedback overlay (drop_highlight)
            // stretches to fill the RESIZED 300x380 window, with the drag ghost
            // centered on the pointer on top — proving the larger rect is the
            // drop hit-area the feedback is drawn over.
            AssertPanelThat(targetNode).ViewportMatches("over-container_2.png");

            // Release LMB over the resized container_2 → resolves the drop.
            viewport.PushInput(new InputEventMouseButton {
                Position = releaseGlobal,
                GlobalPosition = releaseGlobal,
                ButtonIndex = MouseButton.Left,
                Pressed = false,
                ButtonMask = 0
            });
            await runner.SimulateFrames(2);

            // Advance the runtime once so the drop's move plan (remove item-1 from
            // container_1, re-add it to container_2) is applied and the RootNode
            // repaints from the (now-moved) runtime state.
            RuntimeInterop.RunIteration();
            await runner.SimulateFrames(2);

            // ── STEP 3 · VERIFY: drop resolved AND the resize persisted ──
            Assertions.AssertThat(sourceNode.IsDragging)
                .OverrideFailureMessage("item-1 should no longer report IsDragging after the drop")
                .IsFalse();
            Assertions.AssertThat(sourceNode.DragHoverTarget == null)
                .OverrideFailureMessage("the drag source's hover target should be cleared after the drop")
                .IsTrue();
            Assertions.AssertThat(targetNode.IsDropFeedbackVisible)
                .OverrideFailureMessage("the resized container_2's drop-feedback overlay should be hidden after the drop")
                .IsFalse();

            // The client-side resize survives the repaint the drop triggered:
            // the window's _userSize (300x380) is honored over the declared size.
            AssertPanelThat(targetNode).ViewportIsSize(300, 380);
            AssertPanelThat(targetNode).IsPositionEqual(320, 100);

            // ── STAGE 5 · RELEASED (drop resolved; resize persisted) ──
            // Viewport (pixel): the move is reflected visually — container_1 now
            // shows only item-2 (item-1 moved out) and the resized container_2
            // (still 300x380) now shows item-1. The ghost and feedback overlay
            // are gone, and the resize survived the repaint.
            AssertPanelThat(c1Node).ViewportMatches("result-container_1.png");
            AssertPanelThat(targetNode).ViewportMatches("result-container_2.png");

            // END-TO-END drop result (data path): item-1 moved into the resized
            // container_2; item-2 stayed in container_1.
            container1 = EntityInterop.GetEntityById("container_1")!;
            container2 = EntityInterop.GetEntityById("container_2")!;
            Assertions.AssertThat(container1.Members("members").Contains("item-1"))
                .OverrideFailureMessage("item-1 should no longer be in container_1 after the drop")
                .IsFalse();
            Assertions.AssertThat(container1.Members("members").Contains("item-2"))
                .OverrideFailureMessage("item-2 should still be in container_1 after the drop")
                .IsTrue();
            Assertions.AssertThat(container2.Members("members").Length).IsEqual(1);
            Assertions.AssertThat(container2.Members("members").Contains("item-1"))
                .OverrideFailureMessage("item-1 should be in the resized container_2 after the drop")
                .IsTrue();
            AssertRuntimeOutputContains("___container drag move item-1 -> container_2___");

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
    /// receives them (the same pattern as Stage_3/Resizable.Drag).
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
