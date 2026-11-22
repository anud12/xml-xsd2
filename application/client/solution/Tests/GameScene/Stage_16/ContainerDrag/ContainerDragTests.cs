using GdUnit4.Examples.Basics.Setup.Sources.UI;
using Godot;
using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;
using Vector2 = Godot.Vector2;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_16.ContainerDrag;

/// A panel (window) that maps to a container via `entityList`, whose child
/// entities are individually draggable, dragged between two such panels:
/// `item-1` starts in `container_1`; the test drives a live mouse drag from the
/// `item-1` child window to the `container_2` window and proves the entity is
/// moved from `container_1` to `container_2` (the drag type's `onDrop` fires the
/// move action, identifying the destination via `ctx.targetId`).
///
/// Unlike the runtime-only `DragDropBuy` test, this drives a LIVE Godot scene
/// and pushes synthetic mouse input (press → motion → motion → release) so the
/// C# drag state machine in `UiWindow.DragDrop.cs` runs. It asserts at four
/// distinct moments (initial / dragging / over destination / released); at each
/// it checks both the *visual* drag state (via pixel-level
/// <c>ViewportMatches</c> captures of the container windows — the ghost over
/// the source, the drop-feedback overlay over the destination — and, after
/// release, the *data* drop result (the entity moved between containers,
/// reflected in the re-rendered entityLists). The reference images live beside
/// this file.
///
/// Requires the Godot runtime (`GODOT_BIN`); without it this test is skipped.
[TestSuite]
public class ContainerDragTests : Steps {
    [TestCategory("Stage_16")]
    [TestCase(Timeout = 30_000)]
    [RequireGodotRuntime]
    public async Task it_should_move_a_container_entity_by_dragging_its_child_window_to_another_container() {
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
            Assertions.AssertThat(container1.Members("members").Contains("item-2")).IsTrue();
            Assertions.AssertThat(container2.Members("members").Length).IsEqual(0);

            var scene = await AttachUiScene();
            // The scene runs with RUN_RUNTIME_LOOP=false: advance the runtime once
            // so the container entityLists expand (materializing the per-entity
            // child windows) and pump a couple of frames so the child windows are
            // real Godot nodes we can target with synthetic mouse input.
            RuntimeInterop.RunIteration();
            await runner.SimulateFrames(2);

            // Each entity is rendered as its own draggable child window inside the
            // container window's entityList.
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

            // Press on the item-1 child window (the drag source), release on the
            // container_2 window (the drop target).
            var pressGlobal = sourceNode.GlobalPosition + sourceNode.Size / 2f;
            var releaseGlobal = targetNode.GlobalPosition + targetNode.Size / 2f;
            var viewport = runner.Scene().GetViewport();

            // ── ASSERT 1 · INITIAL: nothing is dragging before any input ──
            Assertions.AssertThat(sourceNode.IsDragging)
                .OverrideFailureMessage("item-1 should not report IsDragging before any input")
                .IsFalse();
            Assertions.AssertThat(targetNode.IsDropFeedbackVisible)
                .OverrideFailureMessage("container_2 should show no drop feedback before any input")
                .IsFalse();
            Assertions.AssertThat(sourceNode.DragHoverTarget == null)
                .OverrideFailureMessage("there should be no drag hover target before any input")
                .IsTrue();
            // Viewport (pixel) verification: container_1 shows both item child
            // panels (item-1 on top, item-2 below); container_2 is an empty panel.
            // These are the baseline references reused later to prove the visuals
            // are exactly as expected at each drag moment.
            AssertPanelThat(c1Node).ViewportMatches("initial-container_1.png");
            AssertPanelThat(targetNode).ViewportMatches("initial-container_2.png");

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
            var midGlobal = pressGlobal + new Vector2(20, 0); // >4px, still over item-1
            viewport.PushInput(new InputEventMouseMotion {
                Position = midGlobal,
                GlobalPosition = midGlobal,
                Relative = midGlobal - pressGlobal
            });
            await runner.SimulateFrames(2);

            // ── ASSERT 2 · DRAGGING HAS STARTED (pointer still over the source) ──
            Assertions.AssertThat(sourceNode.IsDragging)
                .OverrideFailureMessage("item-1 should report IsDragging once the drag has started")
                .IsTrue();
            Assertions.AssertThat(sourceNode.DragHoverTarget != targetNode)
                .OverrideFailureMessage("the hover target should not be container_2 while still over the source")
                .IsTrue();
            // New "available zones" affordance: while the "land" drag is in
            // flight, BOTH container_1 and container_2 (both accept "land") show
            // their ambient `animation` indicator, even though the pointer is
            // still over the source. container_2 is therefore Available.
            Assertions.AssertThat(targetNode.FeedbackState)
                .OverrideFailureMessage("container_2 should be in the Available feedback state while still over the source")
                .IsEqual(DropFeedbackState.Available);
            // Viewport (pixel): the translucent drag ghost renders over item-1
            // inside container_1 (which also shows its ambient 'available'
            // indicator), and container_2 now shows its 'available' indicator.
            AssertPanelThat(c1Node).ViewportMatches("dragging-container_1.png");
            AssertPanelThat(targetNode).ViewportMatches("available-container_2.png");

            // Move the pointer across to the container_2 window's center: it is
            // now over the drop target. The drag is already active, so the later
            // motion still routes to the source's drag handler, which updates the
            // hover target to container_2.
            viewport.PushInput(new InputEventMouseMotion {
                Position = releaseGlobal,
                GlobalPosition = releaseGlobal,
                Relative = releaseGlobal - midGlobal
            });
            await runner.SimulateFrames(2);

            // ── ASSERT 3 · OVER THE DESTINATION ──
            Assertions.AssertThat(sourceNode.IsDragging)
                .OverrideFailureMessage("item-1 should still report IsDragging while over the destination")
                .IsTrue();
            Assertions.AssertThat(sourceNode.DragHoverTarget == targetNode)
                .OverrideFailureMessage("the hover target should be container_2 while the pointer is over it")
                .IsTrue();
            Assertions.AssertThat(targetNode.IsDropFeedbackVisible)
                .OverrideFailureMessage("container_2's drop-feedback overlay should be visible while the pointer is over it")
                .IsTrue();
            // Viewport (pixel): the drop-feedback overlay (drop_highlight,
            // stretched to fill the window) now renders over container_2, with
            // the ghost centered on the pointer on top.
            AssertPanelThat(targetNode).ViewportMatches("over-container_2.png");

            // Release LMB over container_2 → resolves the drop (its onDrop fires
            // the move action).
            viewport.PushInput(new InputEventMouseButton {
                Position = releaseGlobal,
                GlobalPosition = releaseGlobal,
                ButtonIndex = MouseButton.Left,
                Pressed = false,
                ButtonMask = 0
            });
            await runner.SimulateFrames(2);

            // The release resolved the drop (EndDrag -> UiJsDrop dispatched the
            // move action as a queued plan). Advance the runtime once so the plan
            // (remove item-1 from container_1, re-add it to container_2) is
            // applied — the same move the runtime-only DragDropBuy test applies.
            RuntimeInterop.RunIteration();
            // Pump frames so the RootNode repaints from the (now-moved) runtime
            // state: container_1's entityList drops item-1 and container_2's
            // entityList gains it. Required before the viewport (pixel) asserts
            // below so they capture the post-move render.
            await runner.SimulateFrames(2);

            // ── ASSERT 4 · DRAG RELEASED (drop resolved) ──
            Assertions.AssertThat(sourceNode.IsDragging)
                .OverrideFailureMessage("item-1 should no longer report IsDragging after the drop")
                .IsFalse();
            Assertions.AssertThat(sourceNode.DragHoverTarget == null)
                .OverrideFailureMessage("the drag source's hover target should be cleared after the drop")
                .IsTrue();
            Assertions.AssertThat(targetNode.IsDropFeedbackVisible)
                .OverrideFailureMessage("container_2's drop-feedback overlay should be hidden after the drop")
                .IsFalse();
            // Viewport (pixel): the move is reflected visually — container_1 now
            // shows only item-2 (item-1 moved out) and container_2 now shows
            // item-1 (the moved entity). The ghost and feedback overlay are gone.
            AssertPanelThat(c1Node).ViewportMatches("result-container_1.png");
            AssertPanelThat(targetNode).ViewportMatches("result-container_2.png");

            // END-TO-END drop result (data path): the entity moved from
            // container_1 to container_2; item-2 stayed in container_1.
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
                .OverrideFailureMessage("item-1 should be in container_2 after the drop")
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
}
