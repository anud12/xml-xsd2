using GdUnit4.Examples.Basics.Setup.Sources.UI;
using Godot;

namespace NewGameProject.Tests.XUnit;

public partial class Steps
{
    /// <summary>
    /// Makes the simulated mouse position authoritative for hover tracking so
    /// the developer's real cursor (moving over the runner window during a
    /// test run) cannot cancel a simulated hover between frames.
    /// </summary>
    public Steps SimulateMouse(Vector2 globalPosition)
    {
        RootNode.SimulatedMouse = globalPosition;
        return this;
    }

    /// <summary>
    /// Clears the simulated mouse override so hover tracking falls back to
    /// the live cursor again.
    /// </summary>
    public Steps ClearSimulatedMouse()
    {
        RootNode.SimulatedMouse = null;
        return this;
    }
}
