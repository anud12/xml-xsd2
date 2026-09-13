using GdUnit4.Examples.Basics.Setup.Sources.UI;
using Godot;
using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;
using Vector2 = Godot.Vector2;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_9.ContainerViewResizableClick;

[TestSuite]
public class TestClass : Steps {
    [TestCategory("Stage_9")]
    [TestCase(Timeout = 15_000)]
    [RequireGodotRuntime]
    public async Task Given_resized_container_view_it_should_move_node_to_clicked_cell() {
        try {
            CleanupArchive();
            AddFileToArchive("module/index.js", "index.js")
                .AddFileToArchive("module/manifest.json", "manifest.json")
                .EnsureDllAccessible()
                .ProcessArchive();

            var scene = await AttachUiScene();

            var plane = scene.Window("plane");
            Assertions.AssertThat(plane).IsNotNull();
            // Declared 400x200 view of the 10x5 container (cell pitch 40x40).
            Assertions.AssertThat(plane.Size).IsEqual(new Vector2(400, 200));

            // Grow the bottom-right corner by +100 on both axes: the window
            // becomes 500x300 at the same position. The marker geometry stays
            // pinned to the declared 400x200 extent (the runtime position pass
            // uses the frozen viewWidth/viewHeight), so the marker for
            // container cell (6,3) still sits at local (6*40, 3*40) = (240,120).
            Drag(plane, new Vector2(398, 198), new Vector2(100, 100));
            await runner.SimulateFrames(2);
            Assertions.AssertThat(plane.Size).IsEqual(new Vector2(500, 300));

            // Click the (240,120) marker. Resolving against the declared size
            // gives col=(240/400)*10=6, row=(120/200)*5=3 -> cell (6,3).
            // (Resolving against the live 500x300 size would give (4,2).)
            ClickLocal(plane, new Vector2(240, 120));
            await runner.SimulateFrames(2);
            Assertions.AssertThat(RuntimeInterop.IsActorBusy("node-1")).IsTrue();

            // Run the speed-1 move to completion and confirm the destination.
            for (int i = 0; i < 12; i++) {
                RuntimeInterop.RunIteration(1);
                await runner.SimulateFrames(2);
                var c = ContainerInterop.GetContainerById("grid-1");
                if (!RuntimeInterop.IsActorBusy("node-1")) break;
            }
            var done = ContainerInterop.GetContainerById("grid-1");
            Assertions.AssertThat(done.GetXForEntityId["node-1"])
                .OverrideFailureMessage("node should land on the clicked cell's column (6)")
                .IsEqual(6.0);
            Assertions.AssertThat(done.GetYForEntityId["node-1"])
                .OverrideFailureMessage("node should land on the clicked cell's row (3)")
                .IsEqual(3.0);
            ClearSimulatedMouse();
        }
        catch (Exception e) {
            Assertions.AssertThat(true)
                .OverrideFailureMessage($"Error: {e.Message}\n{e.StackTrace}")
                .IsFalse();
        }
    }

    Vector2 _hoverLastGlobal = Vector2.Zero;

    /// Presses the window's local point, drags by the given offset, releases —
    /// pushing synthetic mouse events so the window's resize handler receives
    /// them (viewport treats Position as the viewport-global point).
    void Drag(UiWindow window, Vector2 local, Vector2 offset) {
        var viewport = runner.Scene().GetViewport();
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

    void ClickLocal(UiWindow window, Vector2 local) {
        var globalPos = window.GlobalPosition + local;
        var press = new InputEventMouseButton {
            Position = globalPos,
            GlobalPosition = globalPos,
            ButtonIndex = MouseButton.Left,
            Pressed = true,
            ButtonMask = MouseButtonMask.Left
        };
        runner.Scene().GetViewport().PushInput(press);
        var release = (InputEventMouseButton)press.Duplicate();
        release.Pressed = false;
        release.ButtonMask = 0;
        runner.Scene().GetViewport().PushInput(release);
    }
}
