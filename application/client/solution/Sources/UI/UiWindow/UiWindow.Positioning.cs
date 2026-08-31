using Godot;
using NewGameProject.UI;
using System.Text.Json;

namespace GdUnit4.Examples.Basics.Setup.Sources.UI;

/// Positioning and sizing of a window node: the anchor/align/x/y options,
/// explicit sizes, and content-based sizing.
public partial class UiWindow
{
    /// Sets the positioning mode: `windowMode` true uses the window's
    /// anchor/align/x/y options (PositionWithin against the viewport or the
    /// parent container rect), false falls back to plain escape positioning.
    /// RootNode calls this after re-parenting, when the parent is known.
    public void SetPositioningMode(bool windowMode)
    {
        _isWindow = windowMode;
    }

    /// 9-point anchor/align string to a 0..1 fraction.
    public static (float fx, float fy) AnchorFraction(string? anchor) =>
        (anchor ?? "center").Trim().ToLowerInvariant() switch
        {
            "top-left" => (0f, 0f),
            "top" => (0.5f, 0f),
            "top-right" => (1f, 0f),
            "left" => (0f, 0.5f),
            "center" => (0.5f, 0.5f),
            "right" => (1f, 0.5f),
            "bottom-left" => (0f, 1f),
            "bottom" => (0.5f, 1f),
            "bottom-right" => (1f, 1f),
            _ => (0.5f, 0.5f)
        };

    /// Positions this window within `parentRect`: explicit x/y are the
    /// window's top-left coordinates in parent space; the anchor option
    /// places the window's top-left corner at the parent's anchor point
    /// when no x/y is given (default anchor center).
    public void PositionWithin(Rect2 parentRect, UiNodeData node)
    {
        if (!_isWindow) return;
        var childSize = _windowExplicitSize != Vector2.Zero
            ? _windowExplicitSize
            : SizeToContent();
        if (childSize == Vector2.Zero) childSize = new Vector2(100f, 100f);
        Position = _windowHasXY
            ? new Vector2(
                parentRect.Position.X + _windowOffset.X,
                parentRect.Position.Y + _windowOffset.Y)
            : new Vector2(
                parentRect.Position.X + parentRect.Size.X * _windowAnchorFrac.X,
                parentRect.Position.Y + parentRect.Size.Y * _windowAnchorFrac.Y);
        if (_windowExplicitSize != Vector2.Zero)
            Size = _windowExplicitSize;
    }

    Vector2 SizeToContent()
    {
        var box = GetNodeOrNull<BoxContainer>("box");
        if (box == null) return Vector2.Zero;
        var min = new Vector2();
        for (int i = 0; i < box.GetChildCount(); i++)
        {
            var child = box.GetChild(i) as Control;
            if (child == null) continue;
            min = new Vector2(
                Mathf.Max(min.X, (float)child.GetCombinedMinimumSize().X),
                Mathf.Max(min.Y, (float)child.GetCombinedMinimumSize().Y));
        }
        return min;
    }

    void ApplyWindow(UiNodeData node)
    {
        // The positioning mode (window-style anchor/align/offset vs x/y
        // escape) is settled by RootNode after re-parenting, in
        // SetPositioningMode: a window flow-embedded in a container positions
        // against the container's rect, a top-level window against the
        // viewport rect.
        _isWindow = true;
        var opts = ParseOptions(node);
        var hasX = TryNum(opts, "x", out var x);
        var hasY = TryNum(opts, "y", out var y);
        _windowHasXY = hasX || hasY;
        _windowOffset = new Vector2(hasX ? x : 0f, hasY ? y : 0f);
        // The legacy panel anchor is a 0-1 fraction pair; the new .ui anchor
        // is a 9-point string. Accept both.
        Vector2 anchorFrac = Vector2.Zero;
        if (opts.TryGetProperty("anchor", out var a) && a.ValueKind == JsonValueKind.Object
            && TryNum(a, "x", out var ax) && TryNum(a, "y", out var ay))
        {
            anchorFrac = new Vector2(ax, ay);
        }
        else
        {
            var (fx, fy) = AnchorFraction(TryStr(opts, "anchor"));
            anchorFrac = new Vector2(fx, fy);
        }
        _windowAnchorFrac = anchorFrac;
        TryNum(opts, "width", out var w);
        TryNum(opts, "height", out var h);
        _windowExplicitSize = new Vector2(w, h);

        _layoutSpec = UiGrid.UiGridLayoutSpec.Parse(opts);
        EnsureFlowContainer();
        if (_windowExplicitSize != Vector2.Zero)
        {
            CustomMinimumSize = _windowExplicitSize;
            FixedFlowSize = true;
        }
        else
        {
            FixedFlowSize = false;
            CustomMinimumSize = Vector2.Zero;
        }

        ApplyBackground(opts);
        ApplyBorder(opts);
    }
}
