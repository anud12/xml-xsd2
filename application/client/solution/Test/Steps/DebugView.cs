using GdUnit4;

namespace NewGameProject.Tests.XUnit;

public partial class Steps
{
    public async Task DebugView(ISceneRunner runner)
    {
        runner.MaximizeView();
        await runner.SimulateFrames(Int32.MaxValue);
    }
}