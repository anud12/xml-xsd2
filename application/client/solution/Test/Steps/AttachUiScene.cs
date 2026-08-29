using GdUnit4.Examples.Basics.Setup.Sources.UI;
using Godot;

namespace NewGameProject.Tests.XUnit;

public partial class Steps
{
    /// <summary>
    /// Boots the test scene: loads the game scene, attaches a
    /// <see cref="RootNode"/> (which paints the runtime's UI state from the
    /// processed archive) and pumps one frame so window positioning — which
    /// <see cref="RootNode"/> defers to <c>_Ready</c> — has settled. The
    /// returned <see cref="AssertScene"/> is the entry point for all
    /// window assertions.
    /// </summary>
    /// <remarks>
    /// In Debug builds run under a debugger (e.g. "Start" from Visual
    /// Studio), the runner window is maximized and the test run is held
    /// until a key is pressed, so the rendered scene stays visible for
    /// inspection (see <see cref="DebugHoldView"/>). Plain <c>dotnet
    /// test</c> runs (no debugger attached) skip the hold and finish
    /// normally.
    /// </remarks>
    /// <example>
    /// var scene = await AttachUiScene();
    /// scene.AssertPanelThat("center").IsPositionEqual(500, 500);
    /// </example>
    public async Task<AssertScene> AttachUiScene()
    {
        var scene = LoadTestScene();
        var root = new RootNode { Name = "root" };
        scene.AddChild(root);
        // Establish a deterministic initial mouse position so hover tracking
        // starts from a known, off-window state. RootNode.SimulatedMouse is
        // static and can leak between tests when a prior test fails before
        // ClearSimulatedMouse; resetting it here makes each test independent.
        RootNode.SimulatedMouse = new Vector2(0, 0);
        root.Pump();
        await runner.SimulateFrames(1);
        root.Pump();
#if DEBUG
        if (System.Diagnostics.Debugger.IsAttached)
            await DebugHoldView(scene);
#endif
        return new AssertScene(root);
    }

#if DEBUG
    /// <summary>
    /// Shows the runner window maximized for visual inspection during debug
    /// runs. Non-blocking: the test run continues normally.
    /// No-op when no debugger is attached (plain <c>dotnet test</c> runs).
    /// Debug-build only.
    /// </summary>
    public async Task DebugHoldView(Node scene)
    {
        if (!System.Diagnostics.Debugger.IsAttached)
            return;

        runner.MaximizeView();
        GD.Print("[debug] Scene is up");
        Console.WriteLine("[debug] Scene is up");
        await runner.SimulateFrames(1);
    }
#endif
}
