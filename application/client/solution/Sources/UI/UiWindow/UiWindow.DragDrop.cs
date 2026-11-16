using Godot;
using NewGameProject.UI;
using System;
using System.Text.Json;
using ContainerInterop = NewGameProject.Runtime.ContainerInterop;
using RuntimeInterop = NewGameProject.Runtime.RuntimeInterop;

namespace GdUnit4.Examples.Basics.Setup.Sources.UI;

/// Drag source + drop target interaction: ghost texture that follows the
/// pointer during a drag, drop-target feedback overlays, and FFI routing of
/// onDragStart / onDrop / onDragEnd back into the sim context.
public partial class UiWindow
{
    // ─── Drag source state ──────────────────────────────────────────────────
    bool _dragSource;
    string[] _dragTypeNames = Array.Empty<string>();
    string _dragPayloadJson = "{}";
    Texture2D? _dragGhostTexture;
    UiPaintSpec _dragGhostPaint = UiPaintSpec.Contain;
    int _dragRequireHoldMs;
    bool _dragStartJs;
    bool _dragEndJs;
    string? _dragCancelAction;

    // ─── Drop target state ──────────────────────────────────────────────────
    bool _dropTarget;
    string[] _dropAccepts = Array.Empty<string>(); // empty = accept any
    bool _dropOnDropJs;
    Texture2D? _dropFeedbackTexture;
    UiPaintSpec _dropFeedbackPaint = UiPaintSpec.Cover;
    UiTextureSurface? _feedbackOverlay;

    // ─── Drag interaction state ─────────────────────────────────────────────
    const float DragStartThreshold = 4f; // px moved before drag begins
    bool _dragPending;
    Vector2 _dragPressPos;
    ulong _dragPressTime; // OS milliseconds at press (Time.GetTicksMsec)
    bool _dragging;
    TextureRect? _ghost;
    // The drop target currently under the pointer (while _dragging).
    UiWindow? _dragHoverTarget;
    // Last global mouse position seen by the drag state machine (either via
    // GuiInput while the pointer is over this window, or via the per-frame
    // _Process fallback when the pointer has left the window's rect).
    Vector2 _dragLastGlobalMouse;
    bool _dragLastMouseValid;

    // ─── Public probes (read-only views over the private drag state) ────────
    /// True while a drag is in progress (after StartDrag, before EndDrag).
    public bool IsDragging => _dragging;
    /// The drop-target window the pointer is currently over (null if none).
    public UiWindow? DragHoverTarget => _dragHoverTarget;
    /// True while the drop-feedback overlay is visible on this window.
    public bool IsDropFeedbackVisible => _feedbackOverlay != null && _feedbackOverlay.Visible;

    // ─── Global drag registry ───────────────────────────────────────────────
    /// All active drop-target windows. Maintained by WireDragDrop (add) and
    /// _ExitTree (remove). The drag source hit-tests against this list each
    /// frame while a drag is active.
    static readonly List<UiWindow> _dropTargetRegistry = new();

    // Tracks which registry entries are stale (window freed) so we can purge
    // lazily without a per-frame scan cost beyond the drag.
    static readonly HashSet<UiWindow> _registryPurged = new();

    // ─── Wiring ─────────────────────────────────────────────────────────────

    /// Parses `panel.draggable` and `panel.droppable` from the node options
    /// and wires the corresponding input/registry state. Called from
    /// `WireInteractivity` alongside the onClick/onHover wiring.
    void WireDragDrop(UiNodeData node, JsonElement opts)
    {
        if (opts.ValueKind != JsonValueKind.Object) return;

        // ── Draggable (drag source) ──
        if (opts.TryGetProperty("draggable", out var draggable)
            && draggable.ValueKind == JsonValueKind.Object)
        {
            _dragSource = true;

            // type: string | string[]
            if (draggable.TryGetProperty("type", out var typeEl))
                _dragTypeNames = ParseDragTypeList(typeEl);

            // payload: { entityId?, sourceContainerId?, data? }
            if (draggable.TryGetProperty("payload", out var payload)
                && payload.ValueKind == JsonValueKind.Object)
                _dragPayloadJson = payload.GetRawText();

            // ghost: UiTexture JSON { animation, fill, nineSlice? }
            if (draggable.TryGetProperty("ghost", out var ghost)
                && ghost.ValueKind == JsonValueKind.Object)
            {
                _dragGhostTexture = ResolveUiTexture(ghost);
                _dragGhostPaint = ResolveUiTexturePaint(ghost);
            }

            // requireHold: boolean | number (ms)
            if (draggable.TryGetProperty("requireHold", out var rh))
            {
                if (rh.ValueKind == JsonValueKind.Number)
                    _dragRequireHoldMs = (int)rh.GetDouble();
                else if (rh.ValueKind == JsonValueKind.True)
                    _dragRequireHoldMs = 250; // default hold duration
            }

            // onDragStart / onDragEnd: "__jsHandler" marker
            _dragStartJs = IsJsHandlerMarker(draggable, "onDragStart");
            _dragEndJs = IsJsHandlerMarker(draggable, "onDragEnd");

            // onCancel: a named action (legacy string form)
            if (draggable.TryGetProperty("onCancel", out var oc)
                && oc.ValueKind == JsonValueKind.String
                && oc.GetString() != "__jsHandler")
                _dragCancelAction = oc.GetString();
        }
        else
        {
            _dragSource = false;
            _dragTypeNames = Array.Empty<string>();
            _dragPayloadJson = "{}";
            _dragGhostTexture = null;
            _dragRequireHoldMs = 0;
            _dragStartJs = false;
            _dragEndJs = false;
            _dragCancelAction = null;
        }

        // ── Droppable (drop target) ──
        if (opts.TryGetProperty("droppable", out var droppable)
            && droppable.ValueKind == JsonValueKind.Object)
        {
            _dropTarget = true;

            // accepts: string | string[] (empty/absent = accept any)
            if (droppable.TryGetProperty("accepts", out var accepts))
                _dropAccepts = ParseDragTypeList(accepts);

            // onDrop: "__jsHandler" marker
            _dropOnDropJs = IsJsHandlerMarker(droppable, "onDrop");

            // feedback: UiTexture JSON
            if (droppable.TryGetProperty("feedback", out var fb)
                && fb.ValueKind == JsonValueKind.Object)
            {
                _dropFeedbackTexture = ResolveUiTexture(fb);
                _dropFeedbackPaint = ResolveUiTexturePaint(fb);
            }
            else
            {
                _dropFeedbackTexture = null;
            }

            // Register as a drop target for hit-testing by drag sources.
            if (!_dropTargetRegistry.Contains(this))
                _dropTargetRegistry.Add(this);

            // Wire a GuiInput handler so a release that lands on this target
            // (cross-window drag: the pointer left the drag source) can
            // finalize the drop. The drag source's own GuiInput path handles
            // the release when the pointer is still over the source.
            if (!_dropTargetInputWired)
            {
                _dropTargetInputWired = true;
                GuiInput += OnDropTargetGuiInput;
            }
        }
        else
        {
            _dropTarget = false;
            _dropAccepts = Array.Empty<string>();
            _dropOnDropJs = false;
            _dropFeedbackTexture = null;
            // Deregister if we were previously a target.
            if (_dropTargetRegistry.Remove(this))
                _registryPurged.Add(this);
            if (_feedbackOverlay != null)
            {
                _feedbackOverlay.Visible = false;
                _feedbackOverlay.MouseFilter = MouseFilterEnum.Ignore;
            }
        }

        // ── Wire input for drag source ──
        if (_dragSource && !_guiInputWired)
        {
            // The drag source needs to see GuiInput for press/motion/release.
            // If onClick is also wired, _guiInputWired is already true; if not,
            // wire it here so drag input is received even without onClick.
            _guiInputWired = true;
            GuiInput += OnGuiInput;
            GuiInput += OnDragGuiInput;
            MouseFilter = MouseFilterEnum.Stop;
        }
        else if (_dragSource && _guiInputWired)
        {
            // GuiInput was already wired for onClick; add the drag handler
            // alongside. The existing OnGuiInput handles click; OnDragGuiInput
            // handles the drag state machine.
            if (!_dragInputWired)
            {
                _dragInputWired = true;
                GuiInput += OnDragGuiInput;
            }
        }
    }

    bool _dragInputWired;
    bool _dropTargetInputWired;

    // ─── Drop target release handling (cross-window drags) ──────────────────

    /// Handles a mouse-button release that lands on this drop target while a
    /// cross-window drag is in progress (the pointer left the drag source, so
    /// the drag source's GuiInput does not see the release). Finds the active
    /// drag source whose hover target is this window and calls EndDrag on it.
    void OnDropTargetGuiInput(InputEvent evt)
    {
        if (!_dropTarget) return;
        if (evt is not InputEventMouseButton mb) return;
        if (mb.ButtonIndex != MouseButton.Left || mb.Pressed) return;

        // Find the drag source that is currently dragging and whose hover
        // target is this window.
        foreach (var source in _dropTargetRegistry)
        {
            if (source == null || !IsInstanceValid(source)) continue;
            if (source == this) continue; // not a drag source
            if (!source._dragSource) continue;
            if (!source._dragging) continue;
            if (source._dragHoverTarget == this)
            {
                source.EndDrag(mb.Position, mb.GlobalPosition);
                GetViewport().SetInputAsHandled();
                return;
            }
        }
    }

    // ─── Drag source input handling ─────────────────────────────────────────

    /// Drag state machine driven by GuiInput on a drag-source window.
    /// Mirrors the title-bar drag pattern (press → motion → release) but with
    /// a threshold + optional hold requirement before the drag actually starts.
    void OnDragGuiInput(InputEvent evt)
    {
        if (!_dragSource) return;

        if (evt is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left)
        {
            if (mb.Pressed)
            {
                // Record the press; the drag does NOT start yet.
                _dragPending = true;
                _dragging = false;
                _dragPressPos = mb.Position;
                _dragPressTime = Time.GetTicksMsec();
                // Do NOT call SetInputAsHandled here: if the press resolves to
                // a plain click (no drag), the normal onClick path must fire.
            }
            else
            {
                if (_dragging)
                {
                    // Release while dragging: finalize the drop.
                    EndDrag(mb.Position, mb.GlobalPosition);
                    GetViewport().SetInputAsHandled();
                }
                else if (_dragPending)
                {
                    // Released without exceeding the threshold: this is a click,
                    // not a drag. Let the normal onClick path handle it.
                    _dragPending = false;
                    // Do not consume: let OnGuiInput (click) fire.
                }
            }
            return;
        }

        if (evt is not InputEventMouseMotion mm) return;

        var movedLen = (mm.Position - _dragPressPos).Length();

        // Track the global mouse position for the per-frame fallback update.
        _dragLastGlobalMouse = mm.GlobalPosition;
        _dragLastMouseValid = true;

        if (_dragPending && !_dragging)
        {
            var moved = (mm.Position - _dragPressPos).Length();
            var elapsed = Time.GetTicksMsec() - _dragPressTime;
            var thresholdMet = moved >= DragStartThreshold;
            var holdMet = _dragRequireHoldMs == 0 || elapsed >= (ulong)_dragRequireHoldMs;

            if (thresholdMet && holdMet)
                StartDrag(mm.GlobalPosition);
        }
        else if (_dragging)
        {
            UpdateDragMotion(mm.GlobalPosition);
            GetViewport().SetInputAsHandled();
        }
    }

    /// Begins the drag: fires onDragStart (FFI), creates the ghost, enters the
    /// dragging state.
    void StartDrag(Vector2 globalMouse)
    {
        _dragPending = false;
        _dragging = true;

        // Fire onDragStart if it's a JS handler.
        if (_dragStartJs)
        {
            RuntimeInterop.UiJsDragStart(
                _nodeId,
                FirstDragTypeName(),
                _dragPayloadJson,
                true,
                (int)globalMouse.X, (int)globalMouse.Y);
        }

        // Create the ghost: a top-level TextureRect on the root viewport.
        EnsureGhost();
        if (_ghost != null)
        {
            // Position the ghost from the motion event's global position (the
            // frame-accurate source), not the live OS cursor: synthetic-input
            // test runs push events without moving the real cursor, so
            // GetGlobalMousePosition() would place the ghost off-target. This
            // matches UpdateDragMotion, which also uses the event position.
            _ghost.Position = globalMouse - GhostSize() / 2f;
            _ghost.Visible = true;
        }

        // Seed the frame-update fallback with the current mouse position so the
        // per-frame _Process drag update (DragFrameUpdate) can continue the
        // hit-test even when the pointer leaves this window's rect (GuiInput
        // only fires while the pointer is over the control).
        _dragLastGlobalMouse = globalMouse;
        _dragLastMouseValid = true;

        GetViewport().SetInputAsHandled();
    }

    /// Per-frame drag state-machine update, called from the existing _Process
    /// override (UiWindow.Background.cs). While a drag is active and the
    /// pointer is no longer over this window's rect, GuiInput stops firing;
    /// this fallback keeps the ghost and hit-test in sync by polling the
    /// global mouse position each frame. The release is still driven by the
    /// GuiInput path (the release event lands on the window under the pointer
    /// at release time, which for a cross-window drag is the drop target, not
    /// this window — so the release is handled by EndDragViaTarget instead).
    public void DragFrameUpdate()
    {
        if (!_dragging) return;

        // Only update when the pointer is outside this window's rect (when it's
        // inside, the GuiInput path is already driving the state machine and
        // would double-process the same motion).
        var globalMouse = _dragLastMouseValid
            ? _dragLastGlobalMouse
            : GetGlobalMousePosition();
        _dragLastGlobalMouse = globalMouse;
        _dragLastMouseValid = true;

        if (GetGlobalRect().HasPoint(globalMouse)) return; // GuiInput handles it.

        UpdateDragMotion(globalMouse);
    }

    /// Per-motion update while dragging: move the ghost and hit-test drop targets.
    void UpdateDragMotion(Vector2 globalMouse)
    {
        // Move the ghost to follow the pointer.
        if (_ghost != null)
            _ghost.Position = globalMouse - GhostSize() / 2f;

        // Hit-test all registered drop targets to find the compatible one under
        // the pointer. Show/hide feedback overlays accordingly.
        var newTarget = FindDropTargetAt(globalMouse);
        if (newTarget != _dragHoverTarget)
        {
            // Hide the previous target's feedback.
            if (_dragHoverTarget != null)
                _dragHoverTarget.HideDropFeedback();
            // Show the new target's feedback.
            if (newTarget != null)
                newTarget.ShowDropFeedback();
            _dragHoverTarget = newTarget;
        }
    }

    /// Ends the drag: resolves the drop (FFI call to the target's onDrop),
    /// fires onDragEnd, cleans up the ghost.
    void EndDrag(Vector2 localPos, Vector2 globalMouse)
    {
        var target = _dragHoverTarget;
        var dropped = target != null;

        // Fire onDrop on the target if it's a JS handler.
        if (dropped && target._dropOnDropJs)
        {
            var (cellX, cellY) = target.ResolveDropCell(globalMouse);
            var (entityId, sourceContainerId, dataJson) = ExtractPayloadFields();
            RuntimeInterop.UiJsDrop(
                target._nodeId,
                FirstDragTypeName(),
                entityId,
                sourceContainerId,
                true,
                (int)globalMouse.X, (int)globalMouse.Y,
                cellX, cellY,
                dataJson);
        }

        // Fire onDragEnd if it's a JS handler.
        if (_dragEndJs)
        {
            RuntimeInterop.UiJsDragEnd(
                _nodeId,
                FirstDragTypeName(),
                dropped,
                dropped ? (target?._nodeId ?? "") : "",
                _dragPayloadJson,
                true,
                (int)globalMouse.X, (int)globalMouse.Y);
        }
        else if (!dropped && _dragCancelAction != null)
        {
            RuntimeInterop.emitAction(_dragCancelAction);
        }

        // Clean up.
        if (target != null)
            target.HideDropFeedback();
        _dragHoverTarget = null;
        HideGhost();
        _dragging = false;
        _dragLastMouseValid = false;

        RuntimeInterop.DeltaPending = true;
    }

    // ─── Drop target feedback overlay ───────────────────────────────────────

    /// Shows the feedback texture overlay (called by the drag source's hit-test).
    void ShowDropFeedback()
    {
        if (!_dropTarget || _dropFeedbackTexture == null) return;
        if (_feedbackOverlay == null)
        {
            _feedbackOverlay = new UiTextureSurface
            {
                Name = "__dropFeedback",
                MouseFilter = MouseFilterEnum.Ignore,
                ZIndex = 10
            };
            _feedbackOverlay.SetAnchorsPreset(LayoutPreset.FullRect, true);
            AddChild(_feedbackOverlay);
        }
        _feedbackOverlay.Update(_dropFeedbackPaint, _dropFeedbackTexture);
        _feedbackOverlay.Visible = true;
    }

    /// Hides the feedback texture overlay.
    void HideDropFeedback()
    {
        if (_feedbackOverlay != null)
        {
            _feedbackOverlay.Visible = false;
            _feedbackOverlay.MouseFilter = MouseFilterEnum.Ignore;
        }
    }

    /// Resolves the grid cell (cellX, cellY) for a drop position in global
    /// coordinates. Returns (-1, -1) when this window has no grid.
    (int CellX, int CellY) ResolveDropCell(Vector2 globalMouse)
    {
        // Convert global → local for this window.
        var local = GetGlobalTransform().AffineInverse() * globalMouse;

        if (_onClickContainerId != null)
        {
            var container = ContainerInterop.GetContainerById(_onClickContainerId);
            var view = _windowExplicitSize != Vector2.Zero
                ? _windowExplicitSize
                : Size;
            if (container.SizeX is { } sx
                && container.SizeY is { } sy
                && sx.Value > 0 && sy.Value > 0
                && view.X > 0 && view.Y > 0)
            {
                int col = (int)((local.X / view.X) * sx.Value);
                int row = (int)((local.Y / view.Y) * sy.Value);
                col = Math.Clamp(col, 0, (int)sx.Value - 1);
                row = Math.Clamp(row, 0, (int)sy.Value - 1);
                return (col, row);
            }
        }
        var grid = GetNodeOrNull<UiGrid>("grid")
            ?? GetNodeOrNull<ScrollContainer>("scroll")?.GetNodeOrNull<UiGrid>("grid");
        if (grid == null)
            return (-1, -1);
        var (c, r) = grid.CellAt(local);
        return (c, r);
    }

    // ─── Ghost management ───────────────────────────────────────────────────

    /// Ensures the ghost TextureRect exists on the root viewport. Created
    /// lazily on first drag and kept for reuse.
    void EnsureGhost()
    {
        if (_ghost != null && !IsInstanceValid(_ghost)) return;

        var tex = _dragGhostTexture;
        Vector2 size;
        if (tex != null)
        {
            size = tex.GetSize();
        }
        else
        {
            // Default: a 24x24 translucent rect (no texture provided).
            size = new Vector2(24f, 24f);
        }

        // Attach to the root node's parent (the UI layer root) so the ghost
        // renders above all windows.
        var host = GetTree().Root;
        _ghost = new TextureRect
        {
            Name = "__dragGhost",
            MouseFilter = MouseFilterEnum.Ignore,
            ZIndex = 9999,
            Visible = false,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered
        };
        if (tex != null)
            _ghost.Texture = tex;
        else
        {
            // Generate a simple translucent white texture as a placeholder.
            var img = Image.CreateEmpty(24, 24, false, Image.Format.Rgba8);
            img.FillRect(new Rect2I(0, 0, 24, 24), new Color(1f, 1f, 1f, 0.4f));
            _ghost.Texture = ImageTexture.CreateFromImage(img);
        }
        _ghost.SetSize(size);
        host.AddChild(_ghost);
    }

    Vector2 GhostSize()
    {
        if (_ghost == null) return new Vector2(24f, 24f);
        var tex = _ghost.Texture;
        if (tex != null) return tex.GetSize();
        return _ghost.Size;
    }

    void HideGhost()
    {
        if (_ghost != null)
        {
            _ghost.Visible = false;
            _ghost.MouseFilter = MouseFilterEnum.Ignore;
        }
    }

    // ─── Drop target hit-test ───────────────────────────────────────────────

    /// Finds the deepest compatible drop-target window under the given global
    /// mouse position. Returns null when no compatible target is under the
    /// pointer.
    UiWindow FindDropTargetAt(Vector2 globalMouse)
    {
        // Purge stale registry entries (windows that were freed).
        PurgeStaleTargets();

        UiWindow best = null;
        int bestDepth = -1;
        foreach (var target in _dropTargetRegistry)
        {
            if (target == null || !IsInstanceValid(target)) continue;
            if (!target.Visible) continue;

            // Type compatibility check: the target's accepts list must intersect
            // with the drag source's type names (or the target accepts any).
            if (!IsTypeCompatible(target)) continue;

            // Spatial check: the pointer must be within the target's global rect.
            if (!target.GetGlobalRect().HasPoint(globalMouse)) continue;

            var d = NodeDepth(target);
            if (d > bestDepth)
            {
                bestDepth = d;
                best = target;
            }
        }
        return best;
    }

    /// True when the drag type names intersect with the target's accepts list,
    /// or the target accepts any type (empty accepts list).
    bool IsTypeCompatible(UiWindow target)
    {
        if (target._dropAccepts.Length == 0) return true; // accepts any
        foreach (var typeName in _dragTypeNames)
        {
            if (target._dropAccepts.Contains(typeName))
                return true;
        }
        return false;
    }

    static void PurgeStaleTargets()
    {
        for (int i = _dropTargetRegistry.Count - 1; i >= 0; i--)
        {
            var t = _dropTargetRegistry[i];
            if (t == null || !IsInstanceValid(t))
            {
                _dropTargetRegistry.RemoveAt(i);
                _registryPurged.Remove(t);
            }
        }
    }

    static int NodeDepth(Node n)
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

    // ─── Payload field extraction ───────────────────────────────────────────

    /// Extracts entityId, sourceContainerId, and data JSON from the stored
    /// drag payload. Returns empty strings for absent fields and "{}" for
    /// absent data.
    (string EntityId, string SourceContainerId, string DataJson) ExtractPayloadFields()
    {
        string entityId = "";
        string sourceContainerId = "";
        string dataJson = "{}";
        try
        {
            using var doc = JsonDocument.Parse(_dragPayloadJson);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return ("", "", "{}");

            if (root.TryGetProperty("entityId", out var eid))
            {
                entityId = eid.ValueKind == JsonValueKind.String
                    ? eid.GetString() ?? ""
                    : eid.ToString();
            }
            if (root.TryGetProperty("sourceContainerId", out var scid))
            {
                sourceContainerId = scid.GetString() ?? "";
            }
            if (root.TryGetProperty("data", out var data)
                && data.ValueKind == JsonValueKind.Object)
            {
                dataJson = data.GetRawText();
            }
        }
        catch
        {
            // Malformed payload: pass empty/default.
        }
        return (entityId, sourceContainerId, dataJson);
    }

    string FirstDragTypeName()
    {
        return _dragTypeNames.Length > 0 ? _dragTypeNames[0] : "";
    }

    // ─── UiTexture resolution helpers ───────────────────────────────────────

    /// Resolves a UiTexture JSON object `{ animation, fill?, nineSlice? }` into
    /// a concrete Texture2D (the first frame of the animation, loaded from the
    /// archive). Returns null when the texture cannot be resolved.
    /// Reuses the same pattern as `ResolveHoverBackground` and the animation
    /// frame loader in UiWindow.Background.cs.
    static Texture2D? ResolveUiTexture(JsonElement uiTexture)
    {
        if (uiTexture.ValueKind != JsonValueKind.Object) return null;

        // The animation field is a named reference { name } or a string path.
        // After host.js resolution (firstFrameRef), it should be a string path
        // to the first frame's PNG in the archive.
        string path = null;

        if (uiTexture.TryGetProperty("animation", out var anim))
        {
            string refStr = null;
            if (anim.ValueKind == JsonValueKind.String)
                refStr = anim.GetString();
            else if (anim.ValueKind == JsonValueKind.Object
                && anim.TryGetProperty("name", out var nm)
                && nm.ValueKind == JsonValueKind.String)
                refStr = nm.GetString();

            if (refStr != null)
            {
                // The reference is either a named animation (resolve to its first
                // frame's sprite path) or a direct archive path. Animation names
                // are checked first; a miss falls back to the raw path.
                path = FirstAnimationFrame(refStr) ?? refStr;
            }
        }
        // Fall back: the UiTexture itself might be a resolved string path.
        else if (uiTexture.ValueKind == JsonValueKind.String)
        {
            path = uiTexture.GetString();
        }

        if (string.IsNullOrEmpty(path)) return null;

        var files = RuntimeInterop.GetFileFromArchive();
        if (!files.TryGetValue(path, out var data))
        {
            RuntimeInterop.Log($"ui: drag/drop texture not found in archive: {path}");
            return null;
        }
        return CreateTextureTimed(data);
    }

    /// Resolves the paint spec from a UiTexture JSON's fill/nineSlice fields,
    /// falling back to `UiPaintSpec.Cover` (full-rect, uniform scale, centered).
    static UiPaintSpec ResolveUiTexturePaint(JsonElement uiTexture)
    {
        if (uiTexture.ValueKind != JsonValueKind.Object) return UiPaintSpec.Cover;

        bool tileW = false, tileH = false, keepAspect = false;
        int sliceW = 0, sliceH = 0;

        if (uiTexture.TryGetProperty("fill", out var fill)
            && fill.ValueKind == JsonValueKind.Object)
        {
            if (fill.TryGetProperty("width", out var fw)
                && fw.ValueKind == JsonValueKind.String)
            {
                var ws = fw.GetString();
                tileW = ws == "tile";
                keepAspect = ws == "cover" || ws == "fit";
            }
            if (fill.TryGetProperty("height", out var fh)
                && fh.ValueKind == JsonValueKind.String)
            {
                var hs = fh.GetString();
                tileH = hs == "tile";
            }
        }
        if (uiTexture.TryGetProperty("nineSlice", out var ns)
            && ns.ValueKind == JsonValueKind.Object)
        {
            if (ns.TryGetProperty("width", out var nw)
                && nw.ValueKind == JsonValueKind.Number)
                sliceW = (int)nw.GetDouble();
            if (ns.TryGetProperty("height", out var nh)
                && nh.ValueKind == JsonValueKind.Number)
                sliceH = (int)nh.GetDouble();
            if (sliceH == 0) sliceH = sliceW;
        }

        if (sliceW > 0)
            return new UiPaintSpec(false, false, false, false, false, sliceW, sliceH);

        if (tileW || tileH)
            return new UiPaintSpec(tileW, tileH, false);

        return UiPaintSpec.Cover;
    }

    // ─── JSON parsing helpers ───────────────────────────────────────────────

    /// Parses a drag type reference (string) or array of references into a
    /// string[].
    static string[] ParseDragTypeList(JsonElement el)
    {
        if (el.ValueKind == JsonValueKind.String)
            return new[] { el.GetString() ?? "" };
        if (el.ValueKind == JsonValueKind.Array)
        {
            var list = new List<string>();
            foreach (var item in el.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                    list.Add(item.GetString() ?? "");
            }
            return list.ToArray();
        }
        return Array.Empty<string>();
    }

    /// True when the given property in the JSON object is the `"__jsHandler"`
    /// marker string (indicating a stashed JS function).
    static bool IsJsHandlerMarker(JsonElement obj, string prop)
    {
        return obj.TryGetProperty(prop, out var v)
            && v.ValueKind == JsonValueKind.String
            && v.GetString() == "__jsHandler";
    }

    // ─── Lifecycle ──────────────────────────────────────────────────────────

    public override void _ExitTree()
    {
        base._ExitTree();
        // Deregister from the drop-target registry.
        if (_dropTarget && _dropTargetRegistry.Remove(this))
            _registryPurged.Add(this);
        // Clean up feedback overlay.
        if (_feedbackOverlay != null)
        {
            _feedbackOverlay.QueueFree();
            _feedbackOverlay = null;
        }
        // Clean up ghost if we own it (it's on the root, freed when the tree exits).
        if (_ghost != null && IsInstanceValid(_ghost))
        {
            _ghost.QueueFree();
            _ghost = null;
        }
    }
}
