using Godot;
using NewGameProject.UI;
using System.Text.Json;

namespace GdUnit4.Examples.Basics.Setup.Sources.UI;

/// A single node of the .ui tree. The core holds the shared per-window state
/// and dispatches node kinds; layout, content, background, positioning, and
/// interactivity live in sibling partial files.
public partial class UiWindow : Control
{
    readonly Dictionary<string, UiWindow> _children = new();
    bool _isText;
    // Cached window layout options; used to reposition on resize.
    Vector2 _windowOffset = Vector2.Zero;
    bool _windowHasXY;
    Vector2 _windowAnchorFrac = new(0.5f, 0.5f);
    Vector2 _windowExplicitSize = Vector2.Zero; // (0,0) = size to content
    bool _isWindow;

    // Parsed layout of this div/window (drives box vs grid).
    UiGrid.UiGridLayoutSpec _layoutSpec = new();
    // Interactivity.
    string? _onClickAction;
    bool _hoverWired;
    string? _hoverEmitAction;
    bool _hoverStopPropagation;
    Texture2D? _hoverTexture;
    Color? _hoverColor;
    Texture2D? _baseBackgroundTexture;
    bool _isHovered;
    ColorRect? _hoverOverlay;
    HoverOutline? _hoverOutline;
    // Background animation (object reference { name, duration, loop }):
    // frames advanced per GetElapsedTimeUnits like the legacy Panel.
    string? _animName;
    int _animDurationTicks = 1;
    bool _animLoop;
    long _animLastElapsed = -1;
    Texture2D? _animTexture;

    /// True when the window declares explicit x/y position options.
    public bool HasWindowXY => _windowHasXY;

    public virtual void Apply(UiNodeData node)
    {
        WireOptions(node);
        if (node.Kind == UiNodeKind.Text || node.Kind == UiNodeKind.Field)
            ApplyText(node);
        else if (node.Kind == UiNodeKind.Image)
            ApplyImage(node);
        else if (node.Kind == UiNodeKind.Canvas)
            ApplyCanvas(node);
        else if (node.Kind == UiNodeKind.Window)
            ApplyWindow(node);
        else
            ApplyLayout(node);
        // Paint re-applies every node each frame and ApplyBackground resets the
        // visual to the base texture; re-assert the hover visual so a background
        // hover-swap survives the re-apply while this window stays hovered.
        if (_isHovered)
            ApplyHoverVisual();
    }

    void WireOptions(UiNodeData node)
    {
        var opts = ParseOptions(node);
        WireInteractivity(node, opts);
    }

    public void SetChildren(List<string> childIds)
    {
        var box = GetNodeOrNull<BoxContainer>("box");
        var grid = GetNodeOrNull<UiGrid>("grid");
        if (box == null && grid == null) return;

        var wanted = new List<string>(childIds);

        foreach (var id in _children.Keys.ToArray())
        {
            if (!wanted.Contains(id))
            {
                RemoveFlowChild(_children[id]);
                _children[id].QueueFree();
                _children.Remove(id);
            }
        }

        for (int i = 0; i < wanted.Count; i++)
        {
            var id = wanted[i];
            if (_children.TryGetValue(id, out var child)
                && box != null
                && child.GetParent() == box
                && child.GetIndex() != i)
            {
                box.MoveChild(child, i);
            }
        }
    }

    void RemoveFlowChild(UiWindow child)
    {
        var box = GetNodeOrNull<BoxContainer>("box");
        if (box != null) box.RemoveChild(child);
        else
        {
            var grid = GetNodeOrNull<UiGrid>("grid");
            if (grid != null) grid.RemoveChild(child);
            else RemoveChild(child);
        }
    }

    static JsonElement ParseOptions(UiNodeData node)
    {
        try
        {
            return JsonDocument.Parse(node.OptionsJson).RootElement;
        }
        catch { return default; }
    }

    static bool TryNum(JsonElement opts, string prop, out float value)
    {
        if (opts.ValueKind != JsonValueKind.Undefined
            && opts.TryGetProperty(prop, out var v)
            && v.ValueKind == JsonValueKind.Number)
        {
            value = (float)v.GetDouble();
            return true;
        }
        value = 0f;
        return false;
    }

    static string? TryStr(JsonElement opts, string prop)
    {
        if (opts.ValueKind == JsonValueKind.Undefined
            || !opts.TryGetProperty(prop, out var v)
            || v.ValueKind != JsonValueKind.String) return null;
        return v.GetString();
    }
}
