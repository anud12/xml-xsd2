using Godot;

namespace GdUnit4.Examples.Basics.Setup.Sources.UI;

public partial class RootNode
{
    readonly Dictionary<string, string> _pinKeys = new();
    string? _lastPinWindow;

    // Godot enum names, case-insensitive (A-Z, F1-F35, Space and other named keys).
    static bool TryPinKey(string text, out Key key) =>
        Enum.TryParse(text, true, out key) && Enum.IsDefined(typeof(Key), key) && !char.IsDigit(text.FirstOrDefault());

    public override void _UnhandledKeyInput(InputEvent evt)
    {
        if (evt is InputEventKey { Pressed: true, Echo: false } input && HandlePinKey(input.Keycode))
            GetViewport().SetInputAsHandled();
    }

    // Pointer takes priority; otherwise the most recently hovered/created window.
    public bool HandlePinKey(Key key)
    {
        var mouse = SimulatedMouse ?? GetGlobalMousePosition();
        var target = _pinKeys.Keys.Where(id => FindWindowById(id) is { } w && w.Visible
                && w.GetGlobalRect().HasPoint(mouse) && TryPinKey(_pinKeys[id], out var k) && k == key)
            .OrderByDescending(id => Depth(_windows[id])).ThenBy(id => id, StringComparer.Ordinal).FirstOrDefault();
        target ??= _lastPinWindow != null && _pinKeys.TryGetValue(_lastPinWindow, out var text)
            && TryPinKey(text, out var lastKey) && lastKey == key && FindWindowById(_lastPinWindow) != null
            ? _lastPinWindow : null;
        if (target == null) return false;
        _lastPinWindow = target;
        if (!_pinnedSubtrees.ContainsKey(target)) PinWindow(target);
        return true;
    }

    // The button only unpins, even if a stale callback fires during a transition.
    void UnpinFromButton(string id)
    {
        if (!_pinnedSubtrees.ContainsKey(id)) return;
        _lastPinWindow = id;
        UnpinWindow(id);
    }

    void UpdatePinIndicators()
    {
        var mouse = SimulatedMouse ?? GetGlobalMousePosition();
        foreach (var id in _pinKeys.Keys.OrderBy(id => id, StringComparer.Ordinal))
            if (FindWindowById(id) is { } window)
            {
                window.SetPinIndicator(_pinKeys[id], _pinnedSubtrees.ContainsKey(id), () => UnpinFromButton(id));
                if (window.Visible && window.GetGlobalRect().HasPoint(mouse)) _lastPinWindow = id;
            }
    }
}
