using GdUnit4.Examples.Basics.Setup.Sources.UI;
using Godot;
using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;
using Vector2 = Godot.Vector2;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_16.DragDropVisual;

/// Visual drag-and-drop test. Unlike the runtime-only `DragDropBuy` test (which
/// calls the FFI drop handler directly), this test drives a LIVE Godot scene and
/// pushes synthetic mouse input (press → motion → motion → release) so the C#
/// drag state machine in `UiWindow.DragDrop.cs` actually runs.
///
/// It asserts at FOUR distinct drag moments — (1) initial, (2) dragging started,
/// (3) over the destination, (4) released — and at each moment checks both the
/// *visual* drag state (via pixel-level <c>ViewportMatches</c> captures of the
/// relevant window, plus the ghost node / drop-feedback overlay / hit-test
/// target probes) and, after release, the *data* drop result (land moved, value
/// deducted, onDrop marker logged). The four reference images live beside this
/// file; the initial/release pair is shared so ASSERT 4 proves the drag cleaned
/// up its visuals.
///
/// Requires the Godot runtime (`GODOT_BIN`); without it this test is skipped.
[TestSuite]
public class DragDropVisualTests : Steps {
    [TestCategory("Stage_16")]
    [TestCase(Timeout = 30_000)]
    [RequireGodotRuntime]
    public async Task it_should_show_ghost_and_drop_feedback_and_complete_the_drop() {
        try {
            AddFileToArchive("module/index.js", "index.js")
                .AddFileToArchive("module/manifest.json", "manifest.json")
                .AddFileToArchive("module/drop_highlight.png", "drop_highlight.png")
                .AddFileToArchive("module/drag_ghost.png", "drag_ghost.png")
                .AddFileToArchive("module/panel_bg.png", "panel_bg.png")
                .AddFileToArchive("module/fonts/Ubuntu-Regular.ttf", "fonts/Ubuntu-Regular.ttf")
                .EnsureDllAccessible()
                .ProcessArchive();

            var scene = await AttachUiScene();
            await runner.SimulateFrames(1);

            var shop = scene.Window("shop");
            var inventory = scene.Window("inventory");
            // Pin the layout (defensive; the module declares these coords). Both
            // windows sit at y=150, below the coreUI button row (y 8-44), so the
            // buttons are never in the captured window frame.
            AssertPanelThat(shop).IsPositionEqual(20, 150);
            AssertPanelThat(inventory).IsPositionEqual(340, 150);

            // Press near the shop's center, release near the inventory's center.
            var pressGlobal = shop.GlobalPosition + shop.Size / 2f;
            var releaseGlobal = inventory.GlobalPosition + inventory.Size / 2f;
            var viewport = runner.Scene().GetViewport();
            var rootViewport = (Engine.GetMainLoop() as SceneTree)!.Root;
            // The ghost is a top-level TextureRect on the root viewport, created
            // on drag start and freed on drag end — re-query it at each step.
            TextureRect? GetGhost() => rootViewport.GetNodeOrNull<TextureRect>("__dragGhost");

            // ── ASSERT 1 · INITIAL: nothing is dragging before any input ──
            Assertions.AssertThat(shop.IsDragging)
                .OverrideFailureMessage("shop should not report IsDragging before any input")
                .IsFalse();
            Assertions.AssertThat(inventory.IsDropFeedbackVisible)
                .OverrideFailureMessage("inventory should show no drop feedback before any input")
                .IsFalse();
            Assertions.AssertThat(shop.DragHoverTarget == null)
                .OverrideFailureMessage("there should be no drag hover target before any input")
                .IsTrue();
            Assertions.AssertThat(GetGhost() == null)
                .OverrideFailureMessage("no drag ghost should exist before any input")
                .IsTrue();
            // Viewport (pixel) verification: the scene renders exactly the two
            // empty panels — no ghost, no feedback overlay. These reference
            // images are also reused at ASSERT 4 to prove the drag fully cleaned
            // up (the hidden ghost/feedback render identically to their absence).
            AssertPanelThat(shop).ViewportMatches("initial-shop.png");
            AssertPanelThat(inventory).ViewportMatches("initial-inventory.png");

            // Press LMB on the shop (the drag source) and move a little while the
            // pointer is still over the shop: far enough to cross the 4-px
            // DragStartThreshold so the drag begins, but not far enough to reach
            // the inventory. The press and this first motion are pushed in the
            // same frame (the proven pattern from Stage_3/Resizable.Drag): pumping
            // a frame between them drops the button-pressed state, so the motion
            // would not be routed to the shop's GuiInput handler.
            viewport.PushInput(new InputEventMouseButton {
                Position = pressGlobal,
                GlobalPosition = pressGlobal,
                ButtonIndex = MouseButton.Left,
                Pressed = true,
                ButtonMask = MouseButtonMask.Left
            });
            var midGlobal = pressGlobal + new Vector2(20, 0); // >4px, still over the shop
            viewport.PushInput(new InputEventMouseMotion {
                Position = midGlobal,
                GlobalPosition = midGlobal,
                Relative = midGlobal - pressGlobal
            });
            await runner.SimulateFrames(2);

            // ── ASSERT 2 · DRAGGING HAS STARTED (pointer still over the source) ──
            Assertions.AssertThat(shop.IsDragging)
                .OverrideFailureMessage("shop should report IsDragging once the drag has started")
                .IsTrue();
            Assertions.AssertThat(GetGhost() is { Visible: true })
                .OverrideFailureMessage("the drag ghost should be visible once the drag has started")
                .IsTrue();
            // New "available zones" affordance: once the drag is in flight, every
            // compatible drop target shows its ambient `animation` indicator even
            // though the pointer is still over the source. So the inventory is in
            // the Available feedback state (not hovered, not hidden).
            Assertions.AssertThat(inventory.FeedbackState)
                .OverrideFailureMessage("the inventory should be in the Available feedback state while still over the source")
                .IsEqual(DropFeedbackState.Available);
            Assertions.AssertThat(inventory.IsDropFeedbackVisible)
                .OverrideFailureMessage("the inventory's ambient 'available' indicator should be visible while still over the source")
                .IsTrue();
            Assertions.AssertThat(shop.DragHoverTarget != inventory)
                .OverrideFailureMessage("the hover target should not be the inventory while still over the source")
                .IsTrue();
            // Viewport (pixel) verification: the 24x24 translucent drag ghost now
            // renders over the shop, centered on the pointer. The inventory now
            // shows its ambient 'available' indicator (not the initial empty panel).
            AssertPanelThat(shop).ViewportMatches("dragging-shop.png");
            AssertPanelThat(inventory).ViewportMatches("available-inventory.png");

            // Move the pointer across to the inventory's center: it is now over
            // the drop target. The drag is already active (StartDrag ran on the
            // first motion), so the button press is captured and this later-frame
            // motion still routes to the shop's drag handler, which updates the
            // hover target to the inventory.
            viewport.PushInput(new InputEventMouseMotion {
                Position = releaseGlobal,
                GlobalPosition = releaseGlobal,
                Relative = releaseGlobal - midGlobal
            });
            await runner.SimulateFrames(2);

            // ── ASSERT 3 · OVER THE DESTINATION ──
            Assertions.AssertThat(shop.IsDragging)
                .OverrideFailureMessage("shop should still report IsDragging while over the destination")
                .IsTrue();
            Assertions.AssertThat(shop.DragHoverTarget == inventory)
                .OverrideFailureMessage("the hover target should be the inventory while the pointer is over it")
                .IsTrue();
            Assertions.AssertThat(inventory.IsDropFeedbackVisible)
                .OverrideFailureMessage("the inventory's drop-feedback overlay should be visible while the pointer is over it")
                .IsTrue();
            // The specific STATE of the hover phase: the pointer is over the
            // destination, so the ambient `onDrag` (Available) indicator is
            // promoted to the `onHover` (Hovered) "release to drop" highlight.
            // IsDropFeedbackVisible is true for BOTH Available and Hovered, so
            // only this pins the phase to "hovered" (symmetric with ASSERT 2's
            // explicit `Available` assertion).
            Assertions.AssertThat(inventory.FeedbackState)
                .OverrideFailureMessage("the inventory should be in the Hovered feedback state while the pointer is over it")
                .IsEqual(DropFeedbackState.Hovered);
            Assertions.AssertThat(GetGhost() is { Visible: true })
                .OverrideFailureMessage("the drag ghost should still be visible while over the destination")
                .IsTrue();
            // Viewport (pixel) verification: the drop-feedback overlay
            // (drop_highlight, stretched to fill the window) now renders over the
            // inventory, with the ghost centered on the pointer on top.
            AssertPanelThat(inventory).ViewportMatches("over-inventory.png");
            // The drag SOURCE, meanwhile, has reverted to its idle look: the
            // 24x24 ghost is the only drag visual and it now tracks the pointer
            // over the destination (off the source), and the source panel is
            // otherwise untouched. Reusing the ASSERT 1 baseline proves the
            // source is not left in a stuck "dragging" visual state.
            AssertPanelThat(shop).ViewportMatches("initial-shop.png");

            // Release LMB over the inventory → resolves the drop.
            viewport.PushInput(new InputEventMouseButton {
                Position = releaseGlobal,
                GlobalPosition = releaseGlobal,
                ButtonIndex = MouseButton.Left,
                Pressed = false,
                ButtonMask = 0
            });
            await runner.SimulateFrames(2);

            // The scene runs with RUN_RUNTIME_LOOP=false, so SimulateFrames pumps
            // only Godot (input/render) frames and never advances the runtime.
            // The release resolved the drop (EndDrag -> UiJsDrop dispatched the
            // action as a queued plan); advance the runtime once so that plan —
            // moving the land and deducting the value — is applied. This mirrors
            // the runtime-only DragDropBuy test, which calls RunIteration() after
            // the FFI drop.
            RuntimeInterop.RunIteration();

            // ── ASSERT 4 · DRAG RELEASED (drop resolved) ──
            Assertions.AssertThat(shop.IsDragging)
                .OverrideFailureMessage("shop should no longer report IsDragging after the drop")
                .IsFalse();
            Assertions.AssertThat(shop.DragHoverTarget == null)
                .OverrideFailureMessage("the drag source's hover target should be cleared after the drop")
                .IsTrue();
            Assertions.AssertThat(inventory.IsDropFeedbackVisible)
                .OverrideFailureMessage("the inventory's drop-feedback overlay should be hidden after the drop")
                .IsFalse();
            Assertions.AssertThat(GetGhost() is not { Visible: true })
                .OverrideFailureMessage("the drag ghost should be hidden (or freed) after the drop")
                .IsTrue();
            // Viewport (pixel) verification: the render is back to the initial
            // baseline — the feedback overlay and the ghost are gone. Reusing the
            // ASSERT 1 references proves the drag cleaned up its visuals.
            AssertPanelThat(shop).ViewportMatches("initial-shop.png");
            AssertPanelThat(inventory).ViewportMatches("initial-inventory.png");

            // END-TO-END drop result (data path) — proves the pushed input
            // actually reached EndDrag → UiJsDrop.
            var lands = EntityInterop.GetEntityById("lands")!;
            var inv = EntityInterop.GetEntityById("inventory")!;
            Assertions.AssertThat(lands.Members("members").Contains("1")).IsFalse();
            Assertions.AssertThat(inv.Members("members").Length).IsEqual(1);
            Assertions.AssertThat(inv.Members("members").Contains("1")).IsTrue();
            Assertions.AssertThat(double.Parse(
                RuntimeInterop.ReadEntityNumberValue("game", "value"))).IsEqual(90.0);
            AssertRuntimeOutputContains("___From module dragdrop visual onDrop fired line___");

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
