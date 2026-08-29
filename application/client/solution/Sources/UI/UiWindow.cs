using Godot;
using NewGameProject.Module;
using NewGameProject.UI;
using System.Text.Json;
using RuntimeInterop = NewGameProject.Runtime.RuntimeInterop;

namespace GdUnit4.Examples.Basics.Setup.Sources.UI;

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
    public bool HoverStopsPropagation => _hoverStopPropagation;
    Texture2D? _hoverTexture;
    Color? _hoverColor;
    Texture2D? _baseBackgroundTexture;
    bool _isHovered;
    ColorRect? _hoverOverlay;
    GdUnit4.Examples.Basics.Setup.Sources.UI.HoverOutline? _hoverOutline;
    // Background animation (object reference { name, duration, loop }):
    // frames advanced per GetElapsedTimeUnits like the legacy Panel.
    string? _animName;
    int _animDurationTicks = 1;
    bool _animLoop;
    long _animLastElapsed = -1;
    Texture2D? _animTexture;

    /// Sets the positioning mode: `windowMode` true uses the window's
    /// anchor/align/x/y options (PositionWithin against the viewport or the
    /// parent container rect), false falls back to plain escape positioning.
    /// RootNode calls this after re-parenting, when the parent is known.
    public void SetPositioningMode(bool windowMode)
    {
        _isWindow = windowMode;
    }

    /// True when the window declares explicit x/y position options.
    public bool HasWindowXY => _windowHasXY;

    /// True when this window is a flow child with an explicit size and should
    /// be sized exactly to it (not stretched to fill the container cross-axis).
    public bool FixedFlowSize;

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

    /// True when this node is a container that places flow children
    /// The flow container (box/grid) holding leaf children, or null when
    /// this node has none yet.
    public Node FlowContainer()
    {
        return (Node)GetNodeOrNull<BoxContainer>("box")
            ?? (Node)GetNodeOrNull<UiGrid>("grid");
    }

    /// True when this node can host flow children.
    public bool IsFlowParent => FlowContainer() != null;

    /// Applies (or clears) the fixed-size sizing mode for a flow child. A
    /// flow child with an explicit width/height must not stretch to fill the
    /// container cross-axis, so its background and content fit the declared
    /// size; a child without an explicit size keeps the default stretch.
    public void ApplyFixedFlowSize(bool apply)
    {
        if (apply)
        {
            // Expand (claim the child's space slot in the container) +
            // ShrinkBegin, but no ExpandFill: the container hands out space
            // yet the child is sized to its minimum (declared width/height),
            // so it does not stretch across the cross-axis.
            SizeFlagsHorizontal = SizeFlags.ShrinkBegin | SizeFlags.Expand;
            SizeFlagsVertical = SizeFlags.ShrinkBegin | SizeFlags.Expand;
        }
        else
        {
            // Default Control sizing: grow to fill the container.
            SizeFlagsHorizontal = SizeFlags.ShrinkBegin | SizeFlags.Fill | SizeFlags.Expand;
            SizeFlagsVertical = SizeFlags.ShrinkBegin | SizeFlags.Fill | SizeFlags.Expand;
        }
    }

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

    void ApplyText(UiNodeData node)
    {
        _isText = true;
        if (GetNodeOrNull<Label>("text") == null)
        {
            var label = new Label { Name = "text" };
            label.SetAnchorsPreset(LayoutPreset.FullRect);
            label.HorizontalAlignment = HorizontalAlignment.Center;
            label.VerticalAlignment = VerticalAlignment.Center;
            AddChild(label);
        }
        GetNode<Label>("text").Text = node.Value;
    }

    void ApplyImage(UiNodeData node)
    {
        var tr = GetNodeOrNull<TextureRect>("img");
        if (tr == null)
        {
            tr = new TextureRect { Name = "img" };
            tr.SetAnchorsPreset(LayoutPreset.FullRect);
            tr.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
            tr.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
            tr.MouseFilter = Control.MouseFilterEnum.Ignore;
            AddChild(tr);
        }
        tr.Texture = null;
        if (string.IsNullOrEmpty(node.Src)) return;
        if (RuntimeInterop.GetFileFromArchive().TryGetValue(node.Src, out var data))
        {
            var img = new Image();
            img.LoadPngFromBuffer(data);
            tr.Texture = ImageTexture.CreateFromImage(img);
        }
        else
        {
            RuntimeInterop.Log($"ui: image src not found in archive: {node.Src}");
        }
    }

    /// Canvas nodes host a 2D world: a full-rect SubViewportContainer named
    /// "world" (stretching its transparent SubViewport to the control's rect,
    /// so the div layout sizes/clips it) wrapping a UiWorldCanvas Node2D in
    /// world space. `options.world.room`/`options.camera` set the active room
    /// and zoom.
    void ApplyCanvas(UiNodeData node)
    {
        var opts = ParseOptions(node);
        var container = GetNodeOrNull<SubViewportContainer>("world");
        if (container == null)
        {
            container = new SubViewportContainer { Name = "world" };
            container.SetAnchorsPreset(LayoutPreset.FullRect);
            container.Stretch = true;
            container.AddChild(BuildWorldViewport());
            AddChild(container);

            // Standalone canvases size to their explicit options (or a
            // default rect) since they have no flow content to size from.
            TryNum(opts, "width", out var cw);
            TryNum(opts, "height", out var ch);
            var explicitSize = new Vector2(cw, ch);
            if (explicitSize != Vector2.Zero)
                CustomMinimumSize = explicitSize;
        }

        var canvasNode = container.GetNodeOrNull<UiWorldCanvas>("viewport/canvas");
        if (canvasNode == null) return;
        var room = "world";
        var zoom = 1f;
        if (opts.TryGetProperty("world", out var worldOpts)
            && worldOpts.ValueKind == JsonValueKind.Object
            && worldOpts.TryGetProperty("room", out var wr)
            && wr.ValueKind == JsonValueKind.String)
        {
            room = wr.GetString() ?? "";
        }
        if (opts.TryGetProperty("camera", out var camOpts) && camOpts.ValueKind == JsonValueKind.Object)
        {
            if (camOpts.TryGetProperty("room", out var cr) && cr.ValueKind == JsonValueKind.String)
                room = cr.GetString() ?? room;
            if (TryNum(camOpts, "zoom", out var z) && z > 0f) zoom = z;
        }
        canvasNode.SetRoom(room);
        var camera = canvasNode.GetNodeOrNull<Camera2D>("camera");
        if (camera != null)
            camera.Zoom = new Vector2(zoom, zoom);
    }

    /// The transparent SubViewport hosting the world Node2D; sized by the
    /// full-rect SubViewportContainer (Stretch) that holds it.
    SubViewport BuildWorldViewport()
    {
        var viewport = new SubViewport { Name = "viewport", TransparentBg = true };
        viewport.Size = new Vector2I(400, 300);
        viewport.AddChild(new UiWorldCanvas { Name = "canvas" });
        return viewport;
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
    }

    void ApplyLayout(UiNodeData node)
    {
        var opts = ParseOptions(node);
        _layoutSpec = UiGrid.UiGridLayoutSpec.Parse(opts);
        EnsureFlowContainer();
        ApplyBackground(opts);
    }

    /// Pick and (re)configure the flow container for this node's layout:
    /// "column"/"row" (or absent) use the BoxContainer path, a number N an
    /// N-column uniform grid, an object a full track grid.
    void EnsureFlowContainer()
    {
        bool isRow = _layoutSpec.Mode == UiGrid.UiGridLayoutSpec.ModeBoxRow;
        bool useGrid = _layoutSpec.Mode == UiGrid.UiGridLayoutSpec.ModeGrid;

        if (useGrid)
        {
            var bc = GetNodeOrNull<BoxContainer>("box");
            if (bc != null) bc.QueueFree();
            var grid = GetNodeOrNull<UiGrid>("grid");
            if (grid == null)
            {
                grid = new UiGrid { Name = "grid" };
                grid.SetAnchorsPreset(LayoutPreset.FullRect);
                AddChild(grid);
            }
            grid.Configure(_layoutSpec);
            MoveChildrenToContainer(grid);
            return;
        }

        var g = GetNodeOrNull<UiGrid>("grid");
        if (g != null) g.QueueFree();
        var bc2 = GetNodeOrNull<BoxContainer>("box");
        if (bc2 == null)
        {
            bc2 = new BoxContainer { Name = "box", Vertical = !isRow };
            bc2.SetAnchorsPreset(LayoutPreset.FullRect);
            bc2.AddThemeConstantOverride("separation", 0);
            AddChild(bc2);
        }
        bc2.Vertical = !isRow;
        MoveChildrenToContainer(bc2);
    }

    /// Move existing flow children into the given container.
    void MoveChildrenToContainer(Node container)
    {
        foreach (var kv in _children)
        {
            var child = kv.Value;
            child.ApplyFixedFlowSize(child.FixedFlowSize);
            if (child.GetParent() == container) continue;
            var parent = child.GetParent();
            if (parent == this || (parent is Node p && IsDescendant(p, container)))
                parent.RemoveChild(child);
            container.AddChild(child);
        }
    }

    static bool IsDescendant(Node node, Node candidate)
    {
        var cur = candidate.GetParent();
        while (cur != null)
        {
            if (cur == node) return true;
            cur = cur.GetParent();
        }
        return false;
    }

    /// options.background: a string is a static PNG archive path (rendered as a
    /// full-rect background behind the content), an object with "kind" is a
    /// sprite map (composed from the TIFF mask + layer skins), an object with
    /// "name" is an animation reference whose frames advance per runtime
    /// elapsed time unit.
    void ApplyBackground(JsonElement opts)
    {
        if (opts.ValueKind == JsonValueKind.Undefined
            || !opts.TryGetProperty("background", out var bg))
            return;
        if (bg.ValueKind == JsonValueKind.String)
        {
            var path = bg.GetString();
            if (string.IsNullOrEmpty(path)) return;
            if (RuntimeInterop.GetFileFromArchive().TryGetValue(path, out var data))
            {
                var img = new Image();
                img.LoadPngFromBuffer(data);
                var tex = ImageTexture.CreateFromImage(img);
                _baseBackgroundTexture = tex;
                SetBackgroundTexture(tex);
            }
            else
            {
                RuntimeInterop.Log($"ui: background not found in archive: {path}");
            }
        }
        else if (bg.ValueKind == JsonValueKind.Object)
        {
            if (bg.TryGetProperty("kind", out var kind)
                && kind.GetString() == "spriteMap")
            {
                ApplySpriteMapBackground(bg);
                return;
            }
            if (!bg.TryGetProperty("name", out var n) || n.ValueKind != JsonValueKind.String)
                return;
        var name = n.GetString() ?? "";
        _animName = name;
        _animDurationTicks = 1;
        _animLoop = false;
        if (bg.TryGetProperty("duration", out var d) && d.ValueKind == JsonValueKind.Number)
            _animDurationTicks = Math.Max(1, (int)d.GetDouble());
        if (bg.TryGetProperty("loop", out var l) && l.ValueKind == JsonValueKind.True)
            _animLoop = true;
        _animLastElapsed = -1;
        _currentFramePath = null;
        // Render frame 0 immediately: in test mode the runtime loop is off, so
        // elapsed time never advances and _Process would never advance a frame.
        AdvanceAnimationFrame(true);
        }
    }

    /// A sprite map background: { kind: "spriteMap", map, layers: [{layer,
    /// texture}] } — composed once from the 16-bit TIFF mask + 8-bit PNG skins.
    void ApplySpriteMapBackground(JsonElement bg)
    {
        var mapPath = bg.TryGetProperty("map", out var m) ? m.GetString() ?? "" : "";
        var files = RuntimeInterop.GetFileFromArchive();
        if (!files.TryGetValue(mapPath, out var mapData))
        {
            RuntimeInterop.Log(
                $"Sprite map: missing TIFF file \"{mapPath}\" for panel \"{Name}\".");
            return;
        }
        var skins = new List<Image>();
        if (bg.TryGetProperty("layers", out var layers) && layers.ValueKind == JsonValueKind.Array)
        {
            foreach (var layer in layers.EnumerateArray())
            {
                var skinPath = layer.TryGetProperty("texture", out var t) ? t.GetString() ?? "" : "";
                if (files.TryGetValue(skinPath, out var skinData))
                {
                    var img = new Image();
                    img.LoadPngFromBuffer(skinData);
                    skins.Add(img);
                }
                else
                {
                    RuntimeInterop.Log(
                        $"Sprite map: missing skin file \"{skinPath}\" for panel \"{Name}\".");
                    skins.Add(null!);
                }
            }
        }
        var composed = SpriteMapCpu.ComposeSpriteMap(mapData, skins.ToArray());
        foreach (var s in skins) s?.Dispose();
        if (composed == null)
        {
            RuntimeInterop.Log($"Sprite map: composition failed for \"{mapPath}\".");
            return;
        }
        var tex = ImageTexture.CreateFromImage(composed);
        _baseBackgroundTexture = tex;
        TextureFilter = TextureFilterEnum.Nearest;
        SetBackgroundTexture(tex);
    }

    /// Computes the current frame index from the runtime elapsed time units
    /// (same scheme as the legacy Panel) and renders it when it changed.
    /// `force` renders frame 0 immediately on first apply.
    void AdvanceAnimationFrame(bool force)
    {
        if (_animName == null) return;
        var elapsed = RuntimeInterop.GetElapsedTimeUnits();
        if (!force && elapsed <= _animLastElapsed) return;
        _animLastElapsed = elapsed;

        var def = UiState.GetAnimation(_animName);
        if (!def.HasValue)
        {
            RuntimeInterop.Log($"ui: background animation '{_animName}' not registered");
            return;
        }
        if (!def.Value.TryGetProperty("frames", out var frames) || frames.ValueKind != JsonValueKind.Array)
            return;
        if (def.Value.TryGetProperty("duration", out var dd) && dd.ValueKind == JsonValueKind.Number)
            _animDurationTicks = Math.Max(1, (int)dd.GetDouble());
        if (def.Value.TryGetProperty("loop", out var ll) && ll.ValueKind == JsonValueKind.True)
            _animLoop = true;
        var framePaths = new List<string>();
        foreach (var f in frames.EnumerateArray())
        {
            if (f.TryGetProperty("sprite", out var s) && s.ValueKind == JsonValueKind.String)
                framePaths.Add(s.GetString() ?? "");
        }
        if (framePaths.Count == 0) return;

        // Same frame scheme as the legacy Panel: frame 0 from the first
        // elapsed unit, ticksPerFrame = duration / frames.
        var ticksPerFrame = Math.Max(
            (int)Math.Round(_animDurationTicks / (double)framePaths.Count), 1);
        var rawIndex = (int)((elapsed - 1) / ticksPerFrame);
        if (rawIndex < 0) rawIndex = 0;
        var frameIndex = _animLoop
            ? rawIndex % framePaths.Count
            : Math.Min(rawIndex, framePaths.Count - 1);

        var path = framePaths[frameIndex];
        if (!force && path == _currentFramePath) return;
        _currentFramePath = path;
        var files = RuntimeInterop.GetFileFromArchive();
        if (!files.TryGetValue(path, out var data))
        {
            RuntimeInterop.Log($"ui: animation frame not found in archive: {path}");
            return;
        }
        var img = new Image();
        img.LoadPngFromBuffer(data);
        _animTexture = ImageTexture.CreateFromImage(img);
        SetBackgroundTexture(_animTexture);
    }

    string? _currentFramePath;

    public override void _Process(double delta)
    {
        if (_animName != null)
            AdvanceAnimationFrame(false);
        base._Process(delta);
    }

    /// Renders the background as a full-rect TextureRect behind the content
    /// (UiWindow is a plain Control, so it has no theme background of its own).
    void SetBackgroundTexture(Texture2D texture)
    {
        var tr = GetNodeOrNull<TextureRect>("background");
        if (tr == null)
        {
            tr = new TextureRect { Name = "background" };
            tr.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
            tr.StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered;
            tr.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
            tr.ZIndex = -10;
            tr.MouseFilter = Control.MouseFilterEnum.Ignore;
            AddChild(tr);
            MoveChild(tr, 0);
            tr.SetAnchorsPreset(LayoutPreset.FullRect, true);
        }
        tr.Texture = texture;
    }

    /// onClick: left-click press emits the named action. onHover: while
    /// hovered the node's background is swapped (texture or solid color),
    /// reverting on exit; an optional emitAction fires enter/exit.
    void WireInteractivity(UiNodeData node, JsonElement opts)
    {
        if (opts.ValueKind == JsonValueKind.Undefined) return;

        if (opts.TryGetProperty("onClick", out var onClick)
            && onClick.ValueKind == JsonValueKind.String)
        {
            _onClickAction = onClick.GetString();
            MouseFilter = MouseFilterEnum.Stop;
            if (!_guiInputWired)
            {
                _guiInputWired = true;
                GuiInput += OnGuiInput;
            }
        }

        if (!opts.TryGetProperty("onHover", out var hover)
            || hover.ValueKind != JsonValueKind.Object)
            return;

        if (hover.TryGetProperty("emitAction", out var ea)
            && ea.ValueKind == JsonValueKind.String)
            _hoverEmitAction = ea.GetString();

        _hoverStopPropagation = hover.TryGetProperty("stopPropagation", out var sp)
            && sp.ValueKind == JsonValueKind.True;

        if (hover.TryGetProperty("background", out var hb))
            ResolveHoverBackground(hb);

        // Legacy hover: { texture, thickness } — a nine-patch outline that
        // appears on mouse enter and hides on exit.
        if (hover.TryGetProperty("texture", out var htx)
            && (htx.ValueKind == JsonValueKind.String
                || htx.ValueKind == JsonValueKind.Object))
        {
            var thickness = hover.TryGetProperty("thickness", out var th)
                && th.ValueKind == JsonValueKind.Number
                ? (int)th.GetDouble()
                : 0;
            EnsureHoverOutline(ResolveHoverTexturePath(htx), thickness);
        }

        if (!_hoverWired && (_hoverEmitAction != null || _hoverTexture != null || _hoverColor.HasValue || _hoverOutline != null))
        {
            _hoverWired = true;
            MouseEntered += OnMouseEntered;
            MouseExited += OnMouseExited;
        }
    }

    bool _guiInputWired;

    /// True when a descendant window with its own onClick covers the point —
    /// the click belongs to the top-most such node, not to this one.
    void OnGuiInput(InputEvent evt)
    {
        if (evt is InputEventMouseButton mb
            && mb.Pressed
            && mb.ButtonIndex == MouseButton.Left
            && _onClickAction != null)
        {
            // Each window emits its own action when clicked; the top-most
            // clicked node is the one under the cursor (a child window
            // covering the point consumes the event before the parent).
            RuntimeInterop.emitAction(_onClickAction);
            GetViewport().SetInputAsHandled();
        }
    }

    void ResolveHoverBackground(JsonElement hb)
    {
        if (hb.ValueKind == JsonValueKind.String)
        {
            var path = hb.GetString();
            if (!string.IsNullOrEmpty(path)
                && RuntimeInterop.GetFileFromArchive().TryGetValue(path, out var data))
            {
                var img = new Image();
                img.LoadPngFromBuffer(data);
                _hoverTexture = ImageTexture.CreateFromImage(img);
            }
            return;
        }
        if (hb.ValueKind != JsonValueKind.Object) return;
        if (hb.TryGetProperty("color", out var c) && c.ValueKind == JsonValueKind.String)
        {
            var col = c.GetString();
            if (!string.IsNullOrEmpty(col) && col.StartsWith("#") && col.Length is 4 or 7 or 9)
                _hoverColor = new Color(col);
            return;
        }
        // Animation ref { name, duration, loop } — use the first registered
        // frame if resolvable, else a neutral highlight.
        _hoverColor = new Color(1f, 1f, 1f, 0.25f);
    }

    /// Resolves a hover texture to an archive PNG path: a plain string is a
    /// path, an object is an animation reference (uses the first frame).
    static string? ResolveHoverTexturePath(JsonElement htx)
    {
        if (htx.ValueKind == JsonValueKind.String)
            return htx.GetString();
        if (htx.ValueKind != JsonValueKind.Object) return null;
        if (!htx.TryGetProperty("name", out var n) || n.ValueKind != JsonValueKind.String)
            return null;
        var def = UiState.GetAnimation(n.GetString() ?? "");
        if (def is null) return null;
        if (!def.Value.TryGetProperty("frames", out var frames)
            || frames.ValueKind != JsonValueKind.Array) return null;
        foreach (var f in frames.EnumerateArray())
        {
            if (f.TryGetProperty("sprite", out var s) && s.ValueKind == JsonValueKind.String)
                return s.GetString();
        }
        return null;
    }

    void EnsureHoverOutline(string? texturePath, int thickness)
    {
        if (_hoverOutline != null || string.IsNullOrEmpty(texturePath)) return;
        _hoverOutline = new GdUnit4.Examples.Basics.Setup.Sources.UI.HoverOutline(
            new NewGameProject.Runtime.Hover
            {
                Texture = texturePath,
                Thickness = thickness
            });
        _hoverOutline.Visible = false;
        AddChild(_hoverOutline);
        _hoverOutline.Resize();
        Resized += () => _hoverOutline?.Resize();
    }

    void OnMouseEntered() => NotifyHoverEnter();

    void OnMouseExited() => NotifyHoverExit();

    /// True when this window owns a hover (an emit action, a hover
    /// background, or a hover outline) — used by <see
    /// cref="RootNode.UpdateHoverTracking"/> to decide which window under
    /// the mouse owns the hover.
    public bool IsHoverCapable()
        => _hoverEmitAction != null
            || _hoverTexture != null
            || _hoverColor.HasValue
            || _hoverOutline != null;

    public string? HoverEmitAction => _hoverEmitAction;

    /// Hover-enter transition (idempotent): shows the hover outline/overlay
    /// and emits the enter action. No bubbling — RootNode drives enter/exit
    /// per window; a window is only entered when no descendant with its own
    /// hover covers the mouse (stopPropagation is therefore implicit).
    public void NotifyHoverEnter()
    {
        if (_isHovered) return;
        _isHovered = true;
        if (_hoverOutline != null) _hoverOutline.Visible = true;
        ApplyHoverVisual();
        if (_hoverEmitAction != null)
            RuntimeInterop.emitAction(_hoverEmitAction + ":enter");
    }

    /// Hover-exit transition (idempotent): hides the hover outline/overlay
    /// and emits the exit action.
    public void NotifyHoverExit()
    {
        if (!_isHovered) return;
        _isHovered = false;
        if (_hoverOutline != null) _hoverOutline.Visible = false;
        ApplyHoverVisual();
        if (_hoverEmitAction != null)
            RuntimeInterop.emitAction(_hoverEmitAction + ":exit");
    }

    void ApplyHoverVisual()
    {
        if (_isHovered)
        {
            if (_hoverTexture != null)
                SetBackgroundTexture(_hoverTexture);
            else if (_hoverColor.HasValue)
                SetHoverTint(_hoverColor.Value);
        }
        else
        {
            ClearHoverTint();
            // Revert to the base background if one was applied at Apply time.
            if (_baseBackgroundTexture != null)
            {
                SetBackgroundTexture(_baseBackgroundTexture);
            }
            else if (_animName != null)
            {
                // Animation background: the hover swap replaced the current
                // frame; force a re-render of the frame for the current
                // elapsed time so the base frame is restored.
                _animLastElapsed = -1;
                AdvanceAnimationFrame(true);
            }
        }
    }

    void SetHoverTint(Color c)
    {
        if (_hoverOverlay == null)
        {
            _hoverOverlay = new ColorRect { Name = "hoverTint" };
            _hoverOverlay.SetAnchorsPreset(LayoutPreset.FullRect);
            _hoverOverlay.MouseFilter = Control.MouseFilterEnum.Ignore;
            _hoverOverlay.ZIndex = -5;
            AddChild(_hoverOverlay);
            MoveChild(_hoverOverlay, 1);
        }
        _hoverOverlay.Color = c;
        _hoverOverlay.Visible = true;
    }

    void ClearHoverTint()
    {
        if (_hoverOverlay != null)
            _hoverOverlay.Visible = false;
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
