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
    public UiWindow? GetWindowOrNull(string id)
    {
        var direct = _root.GetNodeOrNull<UiWindow>(id);
        if (direct != null) return direct;
        // Global screens are hosted on the UI layer root (the module root's
        // grandparent), outside the module UI root, so the recursive search
        // starts there when the layer hierarchy is present.
        var layer = _root.GetParent()?.GetParent();
        return FindChildWindow(layer ?? _root, id);
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

    /// <summary>
    /// Prints the full UI node tree rooted at the module UI root, with each
    /// node's type, name, and (for Labels) its text. Used to inspect the live
    /// scene graph after a number of ticks.
    /// </summary>
    public void DumpTree()
    {
        DumpNode(_root, 0);
    }

    /// <summary>
    /// Captures the current viewport and saves a cropped PNG for every visible
    /// control in the tree (named after the node), into <paramref name="outputDir"/>.
    /// Used to visually narrow down which node is mis-rendering.
    /// </summary>
    public void DumpNodeScreenshots(string outputDir)
    {
        var image = _root.GetViewport().GetTexture()?.GetImage();
        if (image == null)
        {
            Godot.GD.Print("DumpNodeScreenshots: failed to get viewport image");
            return;
        }
        System.IO.Directory.CreateDirectory(outputDir);
        DumpNodeScreenshotsRecursive(_root, image, outputDir, "");
    }

    void DumpNodeScreenshotsRecursive(Node node, Image fullImage, string outputDir, string pathPrefix)
    {
        var nodeName = node.Name.ToString();
        var path = string.IsNullOrEmpty(pathPrefix) ? nodeName : $"{pathPrefix}_-_{nodeName}";
        if (node is Control control && control.Visible && control.Size != Vector2.Zero)
        {
            var rect = control.GetGlobalRect();
            var x1 = Mathf.Max(rect.Position.X, 0);
            var y1 = Mathf.Max(rect.Position.Y, 0);
            var x2 = Mathf.Min(rect.Position.X + rect.Size.X, fullImage.GetWidth());
            var y2 = Mathf.Min(rect.Position.Y + rect.Size.Y, fullImage.GetHeight());
            var w = x2 - x1;
            var h = y2 - y1;
            if (w > 0 && h > 0)
            {
                var name = SanitizeName(path);
                if (name.Length > 120) name = name.Substring(0, 120);
                var filePath = System.IO.Path.Combine(outputDir, $"{name}.png");
                var cropped = fullImage.GetRegion(new Rect2I((int)x1, (int)y1, (int)w, (int)h));
                cropped.SavePng(filePath);
                Godot.GD.Print($"DumpNodeScreenshots: {name}.png ({(int)w}x{(int)h}) at ({(int)x1},{(int)y1})");
            }
        }
        for (var i = 0; i < node.GetChildCount(); i++)
            DumpNodeScreenshotsRecursive(node.GetChild(i), fullImage, outputDir, path);
    }

    static string SanitizeName(string name)
    {
        if (string.IsNullOrEmpty(name)) return "unnamed";
        var invalid = System.IO.Path.GetInvalidFileNameChars();
        var sb = new System.Text.StringBuilder();
        foreach (var c in name)
            sb.Append(invalid.Contains(c) ? '_' : c);
        return sb.ToString();
    }

    void DumpNode(Node node, int depth)
    {
        var indent = new string(' ', depth * 2);
        var info = node.GetType().Name + " '" + node.Name + "'";
        if (node is Label label)
            info += " text='" + label.Text + "'";
        else if (node is UiWindow win)
            info += " queuedForDeletion=" + win.IsQueuedForDeletion();
        Godot.GD.Print($"{indent}{info}");
        for (var i = 0; i < node.GetChildCount(); i++)
            DumpNode(node.GetChild(i), depth + 1);
    }
}
