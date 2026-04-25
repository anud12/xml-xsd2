using GdUnit4;

namespace NewGameProject.Tests.XUnit;

public partial class Steps
{
    public async Task DebugView()
    {
        this.runner.MaximizeView();
        await this.runner.SimulateFrames(Int32.MaxValue);
    }
}