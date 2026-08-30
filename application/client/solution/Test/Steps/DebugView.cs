using GdUnit4;
using Godot;

namespace NewGameProject.Tests.XUnit;

public partial class Steps
{
    public async Task DebugView()
    {
        this.runner.MaximizeView();
        await this.runner.SimulateFrames(Int32.MaxValue);
    }

#if DEBUG
    /// <summary>
    /// After each test case, keeps the runner window open (maximized) so
    /// the final rendered state can be inspected; pressing any key releases
    /// the hold and the run continues. No-op when no debugger is attached
    /// (plain <c>dotnet test</c> runs finish normally). Debug-build only.
    /// </summary>
    [AfterTest]
    public async Task HoldViewAfterTest()
    {
        if (!System.Diagnostics.Debugger.IsAttached)
            return;

        runner.MaximizeView();
        GD.Print("[debug] Test finished — window held open; press any key to release");
        Console.WriteLine("[debug] Test finished - window held open; press any key to release");

        var released = new TaskCompletionSource<bool>();
        var capture = new Control { Name = "debug_key_capture" };
        capture.MouseFilter = Control.MouseFilterEnum.Ignore;
        capture.GuiInput += @event =>
        {
            if (@event is InputEventKey { Pressed: true, Echo: false })
                released.TrySetResult(true);
        };
        var rootWindow = (Engine.GetMainLoop() as SceneTree)?.Root;
        if (rootWindow is null)
            return;
        rootWindow.AddChild(capture);

        try
        {
            await runner.SimulateFrames(1);
            capture.GrabFocus();
            while (!released.Task.IsCompleted)
                await runner.SimulateFrames(1);
        }
        finally
        {
            capture.QueueFree();
        }
        GD.Print("[debug] Hold released");
        Console.WriteLine("[debug] Hold released");
    }
#endif
}
