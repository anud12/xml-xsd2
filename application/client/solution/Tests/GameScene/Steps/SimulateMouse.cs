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
    /// Resets the simulated mouse to the shared offscreen pin, keeping hover
    /// tracking deterministic instead of falling back to the live cursor.
    /// </summary>
    public Steps ClearSimulatedMouse()
    {
        RootNode.SimulatedMouse = SimulatedMouseOffscreen;
        return this;
    }

    /// <summary>Sets the hover-tracking coordinate without sending GUI input.</summary>
    public Steps HoverAt(Vector2 position) => SimulateMouse(position);

    /// <summary>Sends a mouse event through the supplied viewport.</summary>
    public Steps Click(Viewport viewport, Vector2 position, bool pressed = true)
    {
        viewport.PushInput(new InputEventMouseButton
        {
            Position = position,
            GlobalPosition = position,
            ButtonIndex = MouseButton.Left,
            Pressed = pressed,
        });
        return this;
    }

    /// <summary>Sends mouse motion through the supplied viewport.</summary>
    public Steps Move(Viewport viewport, Vector2 position)
    {
        viewport.PushInput(new InputEventMouseMotion { Position = position, GlobalPosition = position });
        return this;
    }

    /// <summary>Sends press, motion, and release through the supplied viewport.</summary>
    public Steps Drag(Viewport viewport, Vector2 from, Vector2 to)
    {
        Click(viewport, from, true);
        Move(viewport, to);
        Click(viewport, to, false);
        return this;
    }
}
