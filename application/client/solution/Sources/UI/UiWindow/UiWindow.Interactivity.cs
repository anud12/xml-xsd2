using Godot;
using NewGameProject.Runtime;
using NewGameProject.UI;
using System.Text.Json;
using RuntimeInterop = NewGameProject.Runtime.RuntimeInterop;

namespace GdUnit4.Examples.Basics.Setup.Sources.UI;

/// Click and hover interactivity: onClick action emission, hover background
/// swap, hover outline, and hover enter/exit actions.
public partial class UiWindow
{
    /// True when the hover was wired with stopPropagation (the window consumes
    /// hover events so ancestors do not fire).
    public bool HoverStopsPropagation => _hoverStopPropagation;

    public string? HoverEmitAction => _hoverEmitAction;

    bool _guiInputWired;
    // Legacy onClick: the handler is a JS function kept by the sim context;
    // the node's options carry the "__jsHandler" marker instead of a plan.
    bool _onClickJs;
    // Resize drag state (resizable windows).
    bool _resizeWired;
    // Active resize edge flags (bitmask) during a drag: 1 = left, 2 = right,
    // 4 = top, 8 = bottom. A corner sets two bits.
    int _resizeEdge;
    Godot.Vector2 _resizeStartSize = Godot.Vector2.Zero;
    Godot.Vector2 _resizeStartPos = Godot.Vector2.Zero;
    Godot.Vector2 _resizeStartMouse = Godot.Vector2.Zero;
    bool _resizing;
    const int EdgeLeft = 1, EdgeRight = 2, EdgeTop = 4, EdgeBottom = 8;
    const float ResizeHitZone = 6f; // px edge/corner hit thickness

    /// onClick: left-click press emits the named action. onHover: while
    /// hovered the node's background is swapped (the hover animation's first
    /// frame) and/or a nine-patch outline is shown, reverting on exit; an
    /// optional emitAction fires enter/exit.
    void WireInteractivity(UiNodeData node, JsonElement opts)
    {
        if (opts.ValueKind == JsonValueKind.Undefined) return;

        if (opts.TryGetProperty("onClick", out var onClick))
        {
            MouseFilter = MouseFilterEnum.Stop;
            if (!_guiInputWired)
            {
                _guiInputWired = true;
                GuiInput += OnGuiInput;
            }
            if (onClick.ValueKind == JsonValueKind.String)
            {
                var s = onClick.GetString();
                if (s == "__jsHandler")
                    _onClickJs = true;
                else
                    // Legacy string form: a single named action, no cursor args.
                    _onClickAction = s;
            }
            else if (onClick.ValueKind == JsonValueKind.Object
                && onClick.TryGetProperty("steps", out var st)
                && st.ValueKind == JsonValueKind.Array
                && st.GetArrayLength() > 0)
            {
                _onClickAction = null;
                _onClickStepsJson = onClick.GetRawText();
            }
        }

        // The container this panel represents; the cursor cell is resolved from
        // its sizeX/sizeY at click time (independent of the layout tracks).
        if (opts.TryGetProperty("container", out var ctr)
            && ctr.ValueKind == JsonValueKind.String)
        {
            var cid = ctr.GetString();
            if (!string.IsNullOrEmpty(cid)) _onClickContainerId = cid;
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

        // Hover outline: { texture, thickness } — a nine-patch that appears on
        // mouse enter and hides on exit. The texture arrives as an archive
        // path (the first frame of the hover animation, aliased by the
        // node store).
        if (hover.TryGetProperty("texture", out var htx)
            && htx.ValueKind == JsonValueKind.String)
        {
            var thickness = hover.TryGetProperty("thickness", out var th)
                && th.ValueKind == JsonValueKind.Number
                ? (int)th.GetDouble()
                : 0;
            EnsureHoverOutline(htx.GetString(), thickness);
        }

        if (!_hoverWired
            && (_hoverEmitAction != null
                || _hoverTexture != null
                || _hoverColor.HasValue
                || _hoverOutline != null))
        {
            _hoverWired = true;
            MouseEntered += OnMouseEntered;
            MouseExited += OnMouseExited;
        }
    }

    /// True when a descendant window with its own onClick covers the point —
    /// the click belongs to the top-most such node, not to this one.
    void OnGuiInput(InputEvent evt)
    {
        if (evt is InputEventMouseButton mb
            && mb.Pressed
            && mb.ButtonIndex == MouseButton.Left
            && (_onClickAction != null || _onClickStepsJson != null || _onClickJs))
        {
            // Each window emits its own action when clicked; the top-most
            // clicked node is the one under the cursor (a child window
            // covering the point consumes the event before the parent).
            if (_onClickJs)
            {
                var (col, row) = ResolveCursorCell(mb.Position);
                RuntimeInterop.UiJsClick(_nodeId, col, row);
            }
            else if (_onClickAction != null)
                RuntimeInterop.emitAction(_onClickAction);
            else
                ExecuteClickPlan(mb.Position);
            GetViewport().SetInputAsHandled();
        }
    }

    /// Wires resize drag input for a resizable window. The window consumes
    /// mouse input (MouseFilter.Stop) so edge/corner hit zones receive
    /// GuiInput even when the window also has an onClick.
    public void WireResizeInput()
    {
        if (_resizeWired) return;
        _resizeWired = true;
        MouseFilter = MouseFilterEnum.Stop;
        GuiInput += OnResizeGuiInput;
    }

    /// Resize drag handler: on left-press inside an edge/corner hit zone, begin
    /// resizing; while resizing, follow the mouse to grow/shrink the window on
    /// the active edge(s) (shifting position when the top/left edge moves, so
    /// the opposite edge stays put); on release, persist the new size. All four
    /// edges and corners are active.
    public bool IsResizable => _resizable;
    public bool IsResizing => _resizing;

    void OnResizeGuiInput(InputEvent evt)
    {
        if (!_resizable) return;
        if (evt is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left)
        {
            if (mb.Pressed)
            {
                var edge = ResizeEdgeAt(mb.Position);
                if (edge != 0)
                {
                    _resizing = true;
                    _resizeEdge = edge;
                    _resizeStartSize = Size;
                    _resizeStartPos = Position;
                    _resizeStartMouse = mb.GlobalPosition;
                    ApplyResizeCursor(_resizeEdge);
                    GetViewport().SetInputAsHandled();
                }
            }
            else if (_resizing)
            {
                _resizing = false;
                _resizeEdge = 0;
                // Restore to whatever edge the cursor now sits on (may be 0).
                ApplyResizeCursor(ResizeEdgeAt(mb.Position));
                GetViewport().SetInputAsHandled();
            }
        }
        else if (evt is InputEventMouseMotion mm)
        {
            if (_resizing)
            {
                var delta = mm.GlobalPosition - _resizeStartMouse;
                var newSize = _resizeStartSize;
                var newPos = _resizeStartPos;
                if (_resizableKeepAspect)
                    KeepAspectResize(delta, _resizeEdge, ref newSize, ref newPos);
                else
                {
                    if ((_resizeEdge & EdgeRight) != 0)
                        newSize.X = Math.Max(1f, _resizeStartSize.X + delta.X);
                    if ((_resizeEdge & EdgeLeft) != 0)
                    {
                        var w = _resizeStartSize.X - delta.X;
                        if (w < 1f) w = 1f;
                        newPos.X = _resizeStartPos.X + (_resizeStartSize.X - w);
                        newSize.X = w;
                    }
                    if ((_resizeEdge & EdgeBottom) != 0)
                        newSize.Y = Math.Max(1f, _resizeStartSize.Y + delta.Y);
                    if ((_resizeEdge & EdgeTop) != 0)
                    {
                        var h = _resizeStartSize.Y - delta.Y;
                        if (h < 1f) h = 1f;
                        newPos.Y = _resizeStartPos.Y + (_resizeStartSize.Y - h);
                        newSize.Y = h;
                    }
                }
                Position = newPos;
                Size = newSize;
                _userSize = newSize;
                if (newPos != _resizeStartPos)
                {
                    _hasUserPosition = true;
                    _userPosition = newPos;
                }
                CustomMinimumSize = newSize;
                GetViewport().SetInputAsHandled();
            }
            else
            {
                // Hover feedback: show the resize cursor over an edge/corner
                // hit zone, restore the default arrow elsewhere.
                ApplyResizeCursor(ResizeEdgeAt(mm.Position));
            }
        }
    }

    /// Aspect-ratio-preserving resize: the width:height ratio is locked to the
    /// declared starting size. The dominant axis delta (the one that produces
    /// the larger proportional change) drives the scale, so the opposite edge
    /// lags by a sub-pixel amount that is rounded to zero — the window stays
    /// proportional to within a pixel.
    void KeepAspectResize(
        Godot.Vector2 delta, int edge,
        ref Godot.Vector2 newSize, ref Godot.Vector2 newPos)
    {
        var startAspect = _resizeStartSize.X / Mathf.Max(1f, _resizeStartSize.Y);

        // Candidate scale factors from each active edge. The dominant edge
        // (largest |proportional delta|) wins; the other axis follows.
        float scale = 1f;
        float dominant = 0f;
        if ((edge & EdgeRight) != 0)
        {
            var s = (_resizeStartSize.X + delta.X) / _resizeStartSize.X;
            if (Math.Abs(s - 1f) > Math.Abs(dominant)) { dominant = s - 1f; scale = s; }
        }
        if ((edge & EdgeLeft) != 0)
        {
            var w = _resizeStartSize.X - delta.X;
            var s = w / _resizeStartSize.X;
            if (Math.Abs(s - 1f) > Math.Abs(dominant)) { dominant = s - 1f; scale = s; }
        }
        if ((edge & EdgeBottom) != 0)
        {
            var s = (_resizeStartSize.Y + delta.Y) / _resizeStartSize.Y;
            if (Math.Abs(s - 1f) > Math.Abs(dominant)) { dominant = s - 1f; scale = s; }
        }
        if ((edge & EdgeTop) != 0)
        {
            var h = _resizeStartSize.Y - delta.Y;
            var s = h / _resizeStartSize.Y;
            if (Math.Abs(s - 1f) > Math.Abs(dominant)) { dominant = s - 1f; scale = s; }
        }

        var nw = Math.Max(1f, _resizeStartSize.X * scale);
        var nh = Math.Max(1f, nw / startAspect);
        newSize = new Godot.Vector2(nw, nh);

        // Shift position so the opposite edge/corner stays put.
        if ((edge & EdgeLeft) != 0)
            newPos.X = _resizeStartPos.X + (_resizeStartSize.X - nw);
        if ((edge & EdgeTop) != 0)
            newPos.Y = _resizeStartPos.Y + (_resizeStartSize.Y - nh);
    }

    /// Maps the active edge bitmask to the matching Godot resize cursor
    /// (arrow when 0 = not over a hit zone) and applies it to the viewport.
    Godot.Control.CursorShape CursorShapeForEdge(int edge)
    {
        var left = (edge & EdgeLeft) != 0;
        var right = (edge & EdgeRight) != 0;
        var top = (edge & EdgeTop) != 0;
        var bottom = (edge & EdgeBottom) != 0;
        if (left && right) return Godot.Control.CursorShape.Hsize;
        if (top && bottom) return Godot.Control.CursorShape.Vsize;
        // Diagonals: the resize arrow runs from the top-left to the bottom-
        // right (NW-SE) for the top-left / bottom-right corners, and from the
        // top-right to the bottom-left (NE-SW) for the top-right / bottom-left
        // corners. Godot names these by the first two letters of the diagonal
        // direction.
        if (top && left) return Godot.Control.CursorShape.Fdiagsize;
        if (bottom && right) return Godot.Control.CursorShape.Fdiagsize;
        if (top && right) return Godot.Control.CursorShape.Bdiagsize;
        if (bottom && left) return Godot.Control.CursorShape.Bdiagsize;
        if (left || right) return Godot.Control.CursorShape.Hsize;
        if (top || bottom) return Godot.Control.CursorShape.Vsize;
        return Godot.Control.CursorShape.Arrow;
    }

    void ApplyResizeCursor(int edge)
    {
        var shape = CursorShapeForEdge(edge);
        // The window's own control cursor: Godot applies the hovered control's
        // shape to the display server each frame, so this is authoritative for
        // the area the window covers.
        MouseDefaultCursorShape = shape;
    }

    /// The active resize edge bitmask at a local point: left/right/top/bottom
    /// edges and their corners; 0 when not over any hit zone.
    int ResizeEdgeAt(Godot.Vector2 local)
    {
        var w = Size.X;
        var h = Size.Y;
        int edge = 0;
        if (local.X <= ResizeHitZone) edge |= EdgeLeft;
        if (local.X >= w - ResizeHitZone) edge |= EdgeRight;
        if (local.Y <= ResizeHitZone) edge |= EdgeTop;
        if (local.Y >= h - ResizeHitZone) edge |= EdgeBottom;
        return edge;
    }

    /// Resolves the click plan captured at panel-definition time: each
    /// cursor symbol in a step's args is replaced by the local grid cell
    /// (col, row) of the click; non-grid panels resolve to (0, 0). The
    /// concrete action + args are then emitted on the C# side.
    void ExecuteClickPlan(Godot.Vector2 localPos)
    {
        try
        {
            using var doc = JsonDocument.Parse(_onClickStepsJson);
            var (col, row) = ResolveCursorCell(localPos);
            foreach (var step in doc.RootElement.GetProperty("steps").EnumerateArray())
            {
                var action = step.GetProperty("action").GetString() ?? "";
                string argsJson = "{}";
                if (step.TryGetProperty("args", out var argsEl)
                    && argsEl.ValueKind == JsonValueKind.Object)
                {
                    var sb = new System.Text.StringBuilder("{");
                    bool first = true;
                    foreach (var prop in argsEl.EnumerateObject())
                    {
                        if (!first) sb.Append(',');
                        first = false;
                        sb.Append('"').Append(prop.Name).Append("\":");
                        if (prop.Value.ValueKind == JsonValueKind.Object
                            && prop.Value.TryGetProperty("__cursor", out var cur))
                        {
                            var axis = cur.GetString();
                            sb.Append(axis == "y" ? row : col);
                        }
                        else
                        {
                            sb.Append(prop.Value.GetRawText());
                        }
                    }
                    sb.Append('}');
                    argsJson = sb.ToString();
                }
                RuntimeInterop.emitAction(action, argsJson);
            }
        }
        catch (Exception ex)
        {
            RuntimeInterop.Log($"ui: onClick plan error: {ex.Message}");
        }
    }

    /// The cell (col, row) for a click position. When this panel represents a
    /// container, the cell is resolved from the container's sizeX/sizeY by
    /// proportion of the click within the window (independent of the declared
    /// layout tracks). Otherwise it falls back to the window's grid child; a
    /// window with neither resolves to (0, 0).
    (int Col, int Row) ResolveCursorCell(Godot.Vector2 localPos)
    {
        if (_onClickContainerId != null)
        {
            var container = ContainerInterop.GetContainerById(_onClickContainerId);
            // The view's items are laid out by the runtime's position pass from
            // the view's declared (viewWidth/viewHeight) size — frozen for the
            // module's lifetime and independent of any user resize. Resolve the
            // click against that same declared extent (not the live Size) so a
            // click lands on the cell whose marker sits under the pointer.
            var view = _windowExplicitSize != Godot.Vector2.Zero
                ? _windowExplicitSize
                : Size;
            if (container.SizeX is { } sx
                && container.SizeY is { } sy
                && sx.Value > 0 && sy.Value > 0
                && view.X > 0 && view.Y > 0)
            {
                int col = (int)((localPos.X / view.X) * sx.Value);
                int row = (int)((localPos.Y / view.Y) * sy.Value);
                col = Math.Clamp(col, 0, (int)sx.Value - 1);
                row = Math.Clamp(row, 0, (int)sy.Value - 1);
                return (col, row);
            }
        }
        var grid = GetNodeOrNull<UiGrid>("grid");
        if (grid == null)
            return (0, 0);
        return grid.CellAt(localPos);
    }

    /// The hover background-swap texture: the node store passes the first
    /// frame of the hover animation as an archive path.
    void ResolveHoverBackground(JsonElement hb)
    {
        if (hb.ValueKind != JsonValueKind.String) return;
        var path = hb.GetString();
        if (!string.IsNullOrEmpty(path)
            && RuntimeInterop.GetFileFromArchive().TryGetValue(path, out var data))
        {
            var img = new Image();
            img.LoadPngFromBuffer(data);
            _hoverTexture = ImageTexture.CreateFromImage(img);
        }
    }

    void EnsureHoverOutline(string? texturePath, int thickness)
    {
        if (_hoverOutline != null || string.IsNullOrEmpty(texturePath)) return;
        _hoverOutline = new HoverOutline(
            new Hover
            {
                Texture = texturePath,
                Thickness = thickness
            });
        _hoverOutline.Visible = false;
        AddChild(_hoverOutline);
        _hoverOutline.Resize();
        Resized += () => _hoverOutline?.Resize();
    }

    void OnMouseEntered()
    {
        NotifyHoverEnter();
        // A resizable window restores the default arrow on entry (the cursor
        // switches to the resize shape only once over an edge hit zone).
        if (_resizable) ApplyResizeCursor(0);
    }

    void OnMouseExited()
    {
        NotifyHoverExit();
        // Leaving the window clears any resize cursor we applied so it does
        // not stick when the mouse is now over a non-resizable area.
        if (_resizable && !_resizing) ApplyResizeCursor(0);
    }

    /// True when this window owns a hover (an emit action, a hover
    /// background, or a hover outline) — used by <see
    /// cref="RootNode.UpdateHoverTracking"/> to decide which window under
    /// the mouse owns the hover.
    public bool IsHoverCapable()
        => _hoverEmitAction != null
            || _hoverTexture != null
            || _hoverColor.HasValue
            || _hoverOutline != null;

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
}
