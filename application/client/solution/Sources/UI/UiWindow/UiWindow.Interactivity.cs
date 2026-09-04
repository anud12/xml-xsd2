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
                // Legacy string form: a single named action, no cursor args.
                _onClickAction = onClick.GetString();
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
            && (_onClickAction != null || _onClickStepsJson != null))
        {
            // Each window emits its own action when clicked; the top-most
            // clicked node is the one under the cursor (a child window
            // covering the point consumes the event before the parent).
            if (_onClickAction != null)
                RuntimeInterop.emitAction(_onClickAction);
            else
                ExecuteClickPlan(mb.Position);
            GetViewport().SetInputAsHandled();
        }
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

    /// The local grid cell (col, row) for a click position, resolved against
    /// this window's grid (the "grid" child). A window without a grid (box
    /// layout or leaf) resolves to (0, 0).
    (int Col, int Row) ResolveCursorCell(Godot.Vector2 localPos)
    {
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
