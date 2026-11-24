using Godot;
using NewGameProject.UI;
using System.Text.Json;

namespace GdUnit4.Examples.Basics.Setup.Sources.UI;

/// Positioning and sizing of a window node: the anchor/align/x/y options,
/// explicit sizes, and content-based sizing.
public partial class UiWindow
{
    public string? PositionTarget { get; private set; }
    public bool PositionAtCursor { get; private set; }
    public Vector2 RelativeOffset { get; private set; }
    public Vector2 TargetAnchor { get; private set; }
    public Vector2 SelfAnchor { get; private set; }

    public void PositionRelative(Rect2 target)
    {
        if (_hasUserPosition) return;
        GlobalPosition = target.Position + target.Size * TargetAnchor + RelativeOffset - Size * SelfAnchor;
    }

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
            "left" or "center-left" => (0f, 0.5f),
            "center" => (0.5f, 0.5f),
            "right" or "center-right" => (1f, 0.5f),
            "bottom-left" => (0f, 1f),
            "bottom" => (0.5f, 1f),
            "bottom-right" => (1f, 1f),
            _ => (0.5f, 0.5f)
        };

    /// Positions this window within `parentRect`: explicit x/y are the
    /// window's top-left coordinates in parent space; the anchor option
    /// places the window's top-left corner at the parent's anchor point
    /// when no x/y is given (default anchor center).
    public void PositionWithin(Rect2 parentRect, UiNodeData node, string? parentAnchorChild = null)
    {
        if (!_isWindow) return;
        var childSize = _windowExplicitSize != Vector2.Zero
            ? _windowExplicitSize
            : SizeToContent();
        if (childSize == Vector2.Zero) childSize = new Vector2(100f, 100f);
        if (_userSize != Vector2.Zero) childSize = _userSize;
        if (_hasUserPosition)
            Position = _userPosition;
        else if (!_windowHasXY && parentAnchorChild != null)
        {
            var (fx, fy) = AnchorFraction(parentAnchorChild);
            Position = parentRect.Position + (parentRect.Size - childSize) * new Vector2(fx, fy);
        }
        else
            Position = _windowHasXY
                ? new Vector2(
                    parentRect.Position.X + _windowOffset.X,
                    parentRect.Position.Y + _windowOffset.Y)
                : new Vector2(
                    parentRect.Position.X + parentRect.Size.X * _windowAnchorFrac.X,
                    parentRect.Position.Y + parentRect.Size.Y * _windowAnchorFrac.Y);
        if (_userSize != Vector2.Zero)
            Size = _userSize;
        else if (_windowExplicitSize != Vector2.Zero)
            Size = _windowExplicitSize;
    }

    Vector2 SizeToContent()
    {
        var box = FlowContainer();
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
        ParseWindowGeometry(opts);
        // resizable: object { horizontal?, vertical?, keepAspectRatio? } — each
        // axis is opt-in (a window is resizable on an axis only when that axis
        // is set true). Only meaningful for windows (explicitly sized panels).
        if (opts.ValueKind == JsonValueKind.Object
            && opts.TryGetProperty("resizable", out var rz)
            && rz.ValueKind == JsonValueKind.Object)
        {
            _resizableHorizontal = RzTrue(rz, "horizontal");
            _resizableVertical = RzTrue(rz, "vertical");
            _resizableKeepAspect = RzTrue(rz, "keepAspectRatio");
        }
        _resizable = _resizableHorizontal || _resizableVertical;
        if (_resizable)
            WireResizeInput();

        _layoutSpec = UiGrid.UiGridLayoutSpec.Parse(opts);
        EnsureFlowContainer();
        ApplyOverflow(opts, FlowContainer());
        ApplyTitle(opts);

        ApplyBackground(opts);
        ApplyBorder(opts);
        // Last, so the grips sit above the scroll container, overlay bars,
        // background, and any content added earlier.
        if (_resizable)
            EnsureResizeGrips();
    }

    /// Parses the window geometry options (x/y/anchor/width/height) into the
    /// cached positioning state and applies the flow sizing consequence: an
    /// explicit size becomes a fixed minimum (a user resize overrides it),
    /// otherwise the window sizes to its content.
    void ParseWindowGeometry(JsonElement opts)
    {
        PositionTarget = null;
        PositionAtCursor = false;
        RelativeOffset = Vector2.Zero;
        TargetAnchor = Vector2.Zero;
        SelfAnchor = Vector2.Zero;
        if (opts.ValueKind == JsonValueKind.Object && opts.TryGetProperty("position", out var position)
            && position.ValueKind == JsonValueKind.Object)
        {
            JsonElement relative = default;
            if (position.TryGetProperty("link", out var link) && link.ValueKind == JsonValueKind.Object)
            {
                relative = link;
                PositionTarget = TryStr(link, "target");
                var (tx, ty) = AnchorFraction(TryStr(link, "anchor") ?? "top-left");
                var (sx, sy) = AnchorFraction(TryStr(link, "selfAnchor") ?? "top-left");
                TargetAnchor = new Vector2(tx, ty);
                SelfAnchor = new Vector2(sx, sy);
            }
            else if (position.TryGetProperty("cursor", out var cursor) && cursor.ValueKind == JsonValueKind.Object)
            {
                relative = cursor;
                PositionAtCursor = true;
            }
            if (relative.ValueKind == JsonValueKind.Object && relative.TryGetProperty("offset", out var offset)
                && offset.ValueKind == JsonValueKind.Object)
            {
                TryNum(offset, "x", out var ox);
                TryNum(offset, "y", out var oy);
                RelativeOffset = new Vector2(ox, oy);
            }
        }
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
        // A container view (top-level `container` option) stretches to fill
        // its host window like a division; the declared extent is cached only
        // for cursor-cell resolution, never as a fixed flow size, so a user
        // resize of the host carries the view with it.
        bool isContainerView = opts.ValueKind == JsonValueKind.Object
            && opts.TryGetProperty("group", out var ctr)
            && ctr.ValueKind == JsonValueKind.String
            && !string.IsNullOrEmpty(ctr.GetString());
        if (_windowExplicitSize != Vector2.Zero && !isContainerView)
        {
            // A user resize overrides the declared minimum so a shrunken size
            // is not clamped back up to the declared explicit size each frame.
            CustomMinimumSize = _userSize != Vector2.Zero ? _userSize : _windowExplicitSize;
            FixedFlowSize = true;
        }
        else
        {
            FixedFlowSize = false;
            CustomMinimumSize = Vector2.Zero;
        }
    }

    /// True when `obj[prop]` is a truthy flag (JSON true, or a non-zero number).
    static bool RzTrue(JsonElement obj, string prop)
        => obj.TryGetProperty(prop, out var v)
            && (v.ValueKind == JsonValueKind.True
                || (v.ValueKind == JsonValueKind.Number && v.GetDouble() != 0));
}
