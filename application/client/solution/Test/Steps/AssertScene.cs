using GdUnit4.Examples.Basics.Setup.Sources.UI;
using Godot;

namespace NewGameProject.Tests.XUnit;

/// <summary>
/// The assertion entry point for a booted test scene. Created by
/// <see cref="Steps.AttachUiScene"/>, which loads the game scene, attaches a
/// <see cref="RootNode"/> (which paints the runtime's UI state) and pumps one
/// frame so window positioning (done in <c>_Ready</c>) has settled.
/// </summary>
/// <example>
/// var scene = await AttachUiScene();
/// scene.AssertPanelThat("center")
///     .IsPositionEqual(500, 500)
///     .HasChildPanelNamed("child", c => c.IsPositionEqual(50, 50));
/// </example>
public class AssertScene {
    readonly RootNode _root;

    internal AssertScene(RootNode root) {
        _root = root;
    }

    /// <summary>
    /// Returns the fluent assertion wrapper for the window with the given id.
    /// The lookup is recursive, so nested windows are found by id alone.
    /// Throws when no such window exists.
    /// </summary>
    /// <param name="id">The declared window id (the .ui node name).</param>
    public Steps.AssertPanel AssertPanelThat(string id) {
        var window = GetWindow(id);
        return new Steps.AssertPanel(window);
    }

    /// <summary>
    /// The raw <see cref="UiWindow"/> node for the given id, for tests that
    /// need the node itself (click coordinates, MouseFilter, ...). Throws
    /// when no such window exists.
    /// </summary>
    public UiWindow Window(string id) {
        return GetWindow(id);
    }

    /// <summary>
    /// The raw <see cref="UiWindow"/> node for the given id, or null when the
    /// window does not exist (for negative-existence assertions).
    /// </summary>
    public UiWindow? GetWindowOrNull(string id) {
        var direct = _root.GetNodeOrNull<UiWindow>(id);
        if (direct != null) return direct;
        return FindChildWindow(_root, id);
    }

    UiWindow GetWindow(string id) {
        var window = GetWindowOrNull(id);
        if (window is null)
            throw new Exception($"No UiWindow named \"{id}\" in the scene");
        return window;
    }

    static UiWindow? FindChildWindow(Node node, string name)
    {
        for (var i = 0; i < node.GetChildCount(); i++)
        {
            var child = node.GetChild(i);
            if (child is UiWindow win)
            {
                if (win.Name == name)
                    return win;
                var nested = FindChildWindow(win, name);
                if (nested != null)
                    return nested;
            }
            else
            {
                var nested = FindChildWindow(child, name);
                if (nested != null)
                    return nested;
            }
        }
        return null;
    }
}
