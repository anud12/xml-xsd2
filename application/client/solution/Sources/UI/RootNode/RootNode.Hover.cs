using Godot;

namespace GdUnit4.Examples.Basics.Setup.Sources.UI;

/// Per-frame hover tracking: polls the mouse position and drives hover
/// enter/exit for every window, because the engine's built-in
/// MouseEntered/MouseExited signals are not delivered reliably for Controls
/// created at runtime in headless test runs.
public partial class RootNode
{
    /// <summary>
    /// Authoritative mouse position for hover tracking when set (test
    /// simulation). The live cursor is only used when this is null, so the
    /// developer's real mouse moving over the runner window cannot cancel a
    /// simulated hover between frames.
    /// </summary>
    public static Vector2? SimulatedMouse;

    void UpdateHoverTracking()
    {
        // The deepest hover-capable window under the mouse owns the hover.
        // Ancestors whose rect contains the mouse do not fire their own
        // hover while a descendant owns it (this is stopPropagation), and a
        // child without its own hover never "covers" its parent, so the
        // parent's hover fires when the child is hovered (bubble-up).
        var sim = SimulatedMouse;
        var mouse = sim.HasValue
            ? sim.Value
            : GetGlobalMousePosition();
        // Snapshot window list: NotifyHoverEnter/Exit may repaint.
        var windows = _windows.Values.ToArray();
        var hoverable = new List<UiWindow>();
        foreach (var win in windows)
        {
            if (win.Visible
                && win.GetGlobalRect().HasPoint(mouse))
                hoverable.Add(win);
        }
        UiWindow owner = null;
        var bestDepth = -1;
        foreach (var win in hoverable)
        {
            if (!win.IsHoverCapable()) continue;
            var d = Depth(win);
            if (d > bestDepth)
            {
                bestDepth = d;
                owner = win;
            }
        }
        foreach (var win in hoverable)
        {
            var hovered = win == owner;
            var was = _hoverStates.TryGetValue(win, out var s) && s;
            if (hovered != was)
            {
                _hoverStates[win] = hovered;
                if (hovered)
                    win.NotifyHoverEnter();
                else
                    win.NotifyHoverExit();
            }
        }
        // Windows no longer under the mouse exit.
        foreach (var win in windows)
        {
            if (hoverable.Contains(win)) continue;
            var was = _hoverStates.TryGetValue(win, out var s) && s;
            if (was)
            {
                _hoverStates[win] = false;
                win.NotifyHoverExit();
            }
        }
    }

    static int Depth(Node n)
    {
        var d = 0;
        var cur = n;
        while (cur != null)
        {
            d++;
            cur = cur.GetParent();
        }
        return d;
    }
}
