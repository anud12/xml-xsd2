using GdUnit4.Examples.Basics.Setup.Sources.UI;
using Godot;
using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;
using Vector2 = Godot.Vector2;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_9.ContainerViewClickMoveTo;

[TestSuite]
public class TestClass : Steps {
    [TestCategory("Stage_9")]
    [TestCase]
    [RequireGodotRuntime]
    public async Task Given_container_view_it_should_move_node_to_clicked_cell() {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .EnsureDllAccessible()
            .ProcessArchive();

        var scene = await AttachUiScene();

        var plane = scene.Window("plane");
        Assertions.AssertThat(plane).IsNotNull();

        // Before any click, node-1 sits at column=2, row=1.
        var before = ContainerInterop.GetContainerById("grid-1");
        Assertions.AssertThat(before.GetXForEntityId["node-1"]).IsEqual(2.0);
        Assertions.AssertThat(before.GetYForEntityId["node-1"]).IsEqual(1.0);
        scene.AssertPanelThat("col").HasContentText("2");
        scene.AssertPanelThat("row").HasContentText("1");

        // The plane is a 700x500 view of the 10x5 container: the cursor cell
        // resolves from the container's sizeX/sizeY by proportion of the click.
        // Local (350, 250) is 50% across the 10-column, 5-row grid -> (5, 2).
        ClickLocal(plane, new Vector2(350, 250));
        // Two frames: the first flushes the click input (move parks) and the
        // second paints, refreshing the readout labels from the entity store.
        await runner.SimulateFrames(2);

        // The click parked a speed-1 move toward (5,2): the actor is now busy.
        // Bresenham: dist = isqrt(3²+1²) = 3, so x advances 1 GTU/tick and y
        // advances 1/3 GTU/tick (its first whole unit on tick 3).
        Assertions.AssertThat(RuntimeInterop.IsActorBusy("node-1")).IsTrue();
        // Tick 1: (3,1).
        RuntimeInterop.RunIteration(1);
        var t1 = ContainerInterop.GetContainerById("grid-1");
        Assertions.AssertThat(t1.GetXForEntityId["node-1"]).IsEqual(3.0);
        Assertions.AssertThat(t1.GetYForEntityId["node-1"]).IsEqual(1.0);
        await runner.SimulateFrames(2);
        scene.AssertPanelThat("col").HasContentText("3");
        scene.AssertPanelThat("row").HasContentText("1");

        // Tick 2: (4,1) — y's fraction still pooling.
        RuntimeInterop.RunIteration(1);
        var t2 = ContainerInterop.GetContainerById("grid-1");
        Assertions.AssertThat(t2.GetXForEntityId["node-1"]).IsEqual(4.0);
        Assertions.AssertThat(t2.GetYForEntityId["node-1"]).IsEqual(1.0);
        await runner.SimulateFrames(2);
        scene.AssertPanelThat("col").HasContentText("4");
        scene.AssertPanelThat("row").HasContentText("1");

        // Tick 3: (5,2) — the destination is reached and the move ends.
        RuntimeInterop.RunIteration(1);
        var done = ContainerInterop.GetContainerById("grid-1");
        Assertions.AssertThat(done.GetXForEntityId["node-1"]).IsEqual(5.0);
        Assertions.AssertThat(done.GetYForEntityId["node-1"]).IsEqual(2.0);
        Assertions.AssertThat(RuntimeInterop.IsActorBusy("node-1")).IsFalse();
        await runner.SimulateFrames(2);
        scene.AssertPanelThat("col").HasContentText("5");
        scene.AssertPanelThat("row").HasContentText("2");

        // A further tick does not resume the finished move.
        RuntimeInterop.RunIteration(1);
        var still = ContainerInterop.GetContainerById("grid-1");
        Assertions.AssertThat(still.GetXForEntityId["node-1"]).IsEqual(5.0);
        Assertions.AssertThat(still.GetYForEntityId["node-1"]).IsEqual(2.0);
        await runner.SimulateFrames(2);
        scene.AssertPanelThat("col").HasContentText("5");
        scene.AssertPanelThat("row").HasContentText("2");
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
        runner.Scene().GetViewport().PushInput(release);
    }
}
