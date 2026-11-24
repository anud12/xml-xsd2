using System.Diagnostics;
using Godot;
using NewGameProject.UI;
using RuntimeInterop = NewGameProject.Runtime.RuntimeInterop;

namespace GdUnit4.Examples.Basics.Setup.Sources.UI;

/// <summary>
/// The root UI node for the <c>.ui</c> layer. Paints the runtime's UI tree as
/// <see cref="UiWindow"/> nodes: top-level windows are positioned against the
/// viewport rect, flow children are nested in the parent's flow container, and
/// x/y-positioned child windows are nested directly in the parent window and
/// positioned against the parent's rect.
/// </summary>
/// <remarks>
/// Repaint is reactive. The Rust runtime already reports changes as an id-keyed
/// delta (an <c>add</c>/<c>update</c>/<c>remove</c> per node whose state —
/// including a re-resolved field/progress value — actually changed this tick).
/// The first frame pulls the full state and builds every window; every later
/// frame consumes only the pending delta, re-applying the affected windows and
/// re-running the structural passes. A clean frame (no node changed) does no
/// repaint work at all.
/// </remarks>
public partial class RootNode : Godot.Panel
{
    readonly Dictionary<string, UiWindow> _windows = new();
    readonly Dictionary<string, TransientWindowLifecycler> _transientWindows = new();
    readonly Dictionary<string, HashSet<string>> _pinnedSubtrees = new();
    const string HoverWindowSuffix = ":hover-window";

    bool IsProtected(string id) => _pinnedSubtrees.Values.Any(ids => ids.Contains(id));

    void RegisterTransient(UiNodeData node)
    {
        if (node.Kind != UiNodeKind.Window || !node.Id.EndsWith(HoverWindowSuffix, StringComparison.Ordinal)
            || _transientWindows.ContainsKey(node.Id)) return;
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(node.OptionsJson);
            var root = doc.RootElement;
            if (root.ValueKind == System.Text.Json.JsonValueKind.Object
                && root.TryGetProperty("pinnable", out var pinnable)
                && pinnable.ValueKind == System.Text.Json.JsonValueKind.Object
                && pinnable.TryGetProperty("key", out var key)
                && key.ValueKind == System.Text.Json.JsonValueKind.String)
            {
                _transientWindows[node.Id] = new TransientWindowLifecycler();
                _pinKeys[node.Id] = key.GetString() ?? "";
                _lastPinWindow = node.Id;
            }
        }
        catch (System.Text.Json.JsonException) { }
    }

    /// Pin without changing the rendered instance, content, or position.
    public void PinWindow(string windowId)
    {
        if (windowId == null || !_transientWindows.TryGetValue(windowId, out var lifecycle)
            || lifecycle.State != TransientWindowLifecycler.Ownership.Transient) return;
        lifecycle.Pin();
        RuntimeInterop.EmitUiHoverPinned(true, windowId[..^HoverWindowSuffix.Length], windowId);
        if (_windows.TryGetValue(windowId, out var pinnedWindow))
        {
            pinnedWindow.SetSubtreeInteractive(true);
            pinnedWindow.SetPinIndicator(_pinKeys[windowId], true);
        }
        var ids = new HashSet<string>();
        var byId = _nodes.ToDictionary(n => n.Id);
        void Visit(string id)
        {
            if (!ids.Add(id) || !byId.TryGetValue(id, out var node)) return;
            foreach (var child in node.Children) Visit(child);
        }
        Visit(windowId);
        _pinnedSubtrees[windowId] = ids;
    }

    /// Unpin; dismiss immediately if the originating hover has ended.
    public void UnpinWindow(string windowId)
    {
        if (windowId == null || !_transientWindows.TryGetValue(windowId, out var lifecycle)
            || lifecycle.State != TransientWindowLifecycler.Ownership.Pinned) return;
        lifecycle.Unpin();
        RuntimeInterop.EmitUiHoverPinned(false, windowId[..^HoverWindowSuffix.Length], windowId);
        if (_windows.TryGetValue(windowId, out var unpinnedWindow))
        {
            unpinnedWindow.SetPinIndicator(_pinKeys[windowId], false);
            unpinnedWindow.SetSubtreeInteractive(false);
        }
        var ids = _pinnedSubtrees[windowId];
        _pinnedSubtrees.Remove(windowId);
        if (lifecycle.State == TransientWindowLifecycler.Ownership.Dismissed)
        {
            foreach (var id in ids)
                if (!IsProtected(id)) RemoveRenderedNode(id);
        }
    }

    public void SourceHoverChanged(string sourceId, bool active)
    {
        if (!_transientWindows.TryGetValue(sourceId + HoverWindowSuffix, out var lifecycle)) return;
        if (active) lifecycle.SourceHoverEnter();
        else lifecycle.SourceHoverExit();
    }

    void RemoveRenderedNode(string id)
    {
        // A pinned window normally keeps its current parent. If that parent is
        // removed, rescue it to the root before Godot frees the parent subtree.
        if (_windows.TryGetValue(id, out var removed))
        {
            foreach (var pinned in _pinnedSubtrees.Keys.ToArray())
                if (_windows.TryGetValue(pinned, out var survivor) && survivor != removed
                    && removed.IsAncestorOf(survivor))
                {
                    var position = survivor.GlobalPosition;
                    survivor.GetParent().RemoveChild(survivor);
                    AddChild(survivor);
                    survivor.GlobalPosition = position;
                }
            if (!IsDead(removed)) removed.QueueFree();
            _hoverStates.Remove(removed);
            _windows.Remove(id);
        }
        _flattened.Remove(id);
        _flattenedFields.Remove(id);
        _flattenedFontSig.Remove(id);
        _transientWindows.Remove(id);
        _pinKeys.Remove(id);
        if (_lastPinWindow == id) _lastPinWindow = null;
        UiState.RemoveAnimStartTime(id);
    }

    /// Look up a rendered node by its runtime id (which may contain NodePath separators).
    public UiWindow? FindWindowById(string id) => _windows.TryGetValue(id, out var window) && !IsDead(window) ? window : null;
    readonly HashSet<string> _flattened = new();
    // For each flattened field/text child id, the host window that owns its
    // "text" Label and the child's node id (so the label value can be
    // re-resolved from the entity store each frame).
    readonly Dictionary<string, (UiWindow host, string childId)> _flattenedFields = new();
    // Last-applied font signature per flattened field, so a font change
    // (size/color/style) arriving via the delta re-applies to the host label
    // only when it actually changed.
    readonly Dictionary<string, string> _flattenedFontSig = new();
    // The current UI tree as fetched from the runtime; the authoritative
    // structure for the reparent/position passes. Kept in sync from the delta
    // so steady-state frames never need a full state pull.
    List<UiNodeData> _nodes = new();
    // True once the first full build has run; later frames are delta-driven.
    bool _painted;

    // Per-phase paint timing (diagnostic): the native delta fetch, the delta
    // apply, and the structural layout passes, plus the real frame time (the
    // Godot per-frame delta), logged as a throttled summary.
    readonly Stopwatch _paintSw = new();
    long _fetchAccumTicks, _fetchMaxTicks;
    long _applyAccumTicks, _applyMaxTicks;
    long _layoutAccumTicks, _layoutMaxTicks;
    double _lastFrameMs, _frameAccumMs, _frameMaxMs;
    int _paintFrames;

    // Per-sub-phase layout timing (diagnostic): the five structural passes,
    // logged on any frame whose total layout time crosses the spike threshold.
    long _lFlattenTicks, _lFlattenMax;
    long _lRefreshTicks, _lRefreshMax;
    long _lReparentTicks, _lReparentMax;
    long _lFlowTicks, _lFlowMax;
    long _lPositionTicks, _lPositionMax;

    // Cached structure maps, rebuilt only when _nodes changes (a delta / full
    // paint) instead of every frame. LayoutPasses runs every frame, so building
    // the O(n) byId and parentOf dictionaries each frame was needless
    // allocation churn (a source of GC hitches in the layout phase).
    Dictionary<string, UiNodeData> _byIdCache = new();
    Dictionary<string, string> _parentOfCache = new();
    bool _structureDirty = true;

    // When set (production app), the paint timing is logged every 120 frames.
    // Off in tests, which read GetPaintTimingMs directly.
    public static bool EnablePaintTimingLog;
    public static Action<string> DiagnosticLogger = s => GD.Print(s);

    // Largest layout spike seen (and its per-sub-pass breakdown), captured for
    // tests/console. Set by LogLayoutSpike on any frame over the threshold.
    public static double LastLayoutSpikeTotal;
    public static string LastLayoutSpikeDetail = "";

    // The UI's own frame counter: incremented once per pumped frame. This is the
    // UI-tracked, deterministic 'when' for interactions — independent of the Rust
    // runtime's elapsed-time-units clock, which ticks at a different rate.
    public static long CurrentFrame;

    public RootNode()
    {
        SetAnchorsPreset(LayoutPreset.FullRect);
        // The root spans the full viewport but must not consume mouse input,
        // otherwise the window nodes it hosts never receive MouseEntered /
        // GuiInput (hover and click).
        MouseFilter = Control.MouseFilterEnum.Ignore;
        UiState.ClearAnimStartTimes();
    }

    readonly Dictionary<UiWindow, bool> _hoverStates = new();

    public override void _Process(double delta)
    {
        _lastFrameMs = delta * 1000.0;
        Pump();
        base._Process(delta);
    }

    /// Drives one frame of painting + hover tracking (used by tests to pump
    /// the node without relying on the engine process callback).
    public void Pump()
    {
        CurrentFrame++;
        Paint();
        UpdateHoverTracking();
        UpdatePinIndicators();
    }

    /// Per-frame paint: a full build on the first frame, then the runtime's
    /// change delta plus the cheap structural passes. The expensive parts — the
    /// full-slab pull (<see cref="UiState.FetchState"/>) and per-node re-apply
    /// (JSON re-parse + archive/texture re-resolution) — run only for nodes the
    /// runtime reports as changed; a clean frame does neither. The structural
    /// passes (reparent/position/flatten) run every frame from the cached
    /// <see cref="_nodes"/> so client-side interactions (window resize, title
    /// drag, scrollbar) that emit no runtime delta stay reconciled.
    public void Paint()
    {
        if (!_painted)
        {
            FullPaint();
            _painted = true;
            return;
        }
        _paintSw.Restart();
        // Fetch only when a tick has produced work: the tick runs at 8 Hz, so on
        // the other ~11 of 12 frames there is no delta and the native fetch call
        // is pure overhead. DeltaPending is latched by RunIteration (the only
        // producer), so this never skips a real delta.
        UiDelta? delta = null;
        if (NewGameProject.Runtime.RuntimeInterop.DeltaPending)
        {
            NewGameProject.Runtime.RuntimeInterop.DeltaPending = false;
            delta = UiState.FetchDelta();
        }
        long fetchTicks = _paintSw.ElapsedTicks;
        _paintSw.Restart();
        if (delta != null && delta.Ops.Count > 0)
        {
            // A fetched, non-empty delta, not the hover event's DeltaPending
            // latch, is evidence that callback inputs may have changed.
            if (_hoverStates.Any(pair => pair.Value && pair.Key.HasHoverWindow()))
                _hoverDataRecheckPending = true;
            ApplyDelta(delta);
        }
        long applyTicks = _paintSw.ElapsedTicks;
        _paintSw.Restart();
        LayoutPasses();
        long layoutTicks = _paintSw.ElapsedTicks;
        AccumulatePaintTiming(fetchTicks, applyTicks, layoutTicks, _lastFrameMs);
    }

    void AccumulatePaintTiming(long fetchTicks, long applyTicks, long layoutTicks, double frameMs)
    {
        _fetchAccumTicks += fetchTicks; _fetchMaxTicks = Math.Max(_fetchMaxTicks, fetchTicks);
        _applyAccumTicks += applyTicks; _applyMaxTicks = Math.Max(_applyMaxTicks, applyTicks);
        _layoutAccumTicks += layoutTicks; _layoutMaxTicks = Math.Max(_layoutMaxTicks, layoutTicks);
        _frameAccumMs += frameMs; _frameMaxMs = Math.Max(_frameMaxMs, frameMs);
        if (++_paintFrames < 120) return;
        if (!EnablePaintTimingLog) return;
        int frames = _paintFrames;
        DiagnosticLogger($"[paint] frame avg={_frameAccumMs / frames:0.00} max={_frameMaxMs:0.00} | " +
                         $"fetch avg={Ms(_fetchAccumTicks) / frames:0.00} max={Ms(_fetchMaxTicks):0.00} | " +
                         $"apply avg={Ms(_applyAccumTicks) / frames:0.00} max={Ms(_applyMaxTicks):0.00} | " +
                         $"layout avg={Ms(_layoutAccumTicks) / frames:0.00} max={Ms(_layoutMaxTicks):0.00} (ms)");
        _fetchAccumTicks = _fetchMaxTicks = 0;
        _applyAccumTicks = _applyMaxTicks = 0;
        _layoutAccumTicks = _layoutMaxTicks = 0;
        _frameAccumMs = 0; _frameMaxMs = 0;
        _paintFrames = 0;
    }

    /// Per-phase paint timing since the last <see cref="ResetPaintTiming"/>
    /// (diagnostic): average and max, in milliseconds.
    public (double fetchAvg, double fetchMax, double applyAvg, double applyMax,
            double layoutAvg, double layoutMax) GetPaintTimingMs()
    {
        int frames = Math.Max(1, _paintFrames);
        return (Ms(_fetchAccumTicks) / frames, Ms(_fetchMaxTicks),
                Ms(_applyAccumTicks) / frames, Ms(_applyMaxTicks),
                Ms(_layoutAccumTicks) / frames, Ms(_layoutMaxTicks));
    }

    public void ResetPaintTiming()
    {
        _fetchAccumTicks = _fetchMaxTicks = 0;
        _applyAccumTicks = _applyMaxTicks = 0;
        _layoutAccumTicks = _layoutMaxTicks = 0;
        _frameAccumMs = 0; _frameMaxMs = 0;
        _paintFrames = 0;
    }

    public int PaintFrames => _paintFrames;

    static double Ms(long ticks) => (double)ticks / Stopwatch.Frequency * 1000.0;

    /// Force a full rebuild (module hot reload): free every window and repaint
    /// from a fresh full state pull.
    public void ForceFullPaint()
    {
        foreach (var win in _windows.Values)
            win.QueueFree();
        _windows.Clear();
        _transientWindows.Clear();
        _pinKeys.Clear();
        _lastPinWindow = null;
        _pinnedSubtrees.Clear();
        _hoverStates.Clear();
        _flattened.Clear();
        _flattenedFields.Clear();
        _flattenedFontSig.Clear();
        _nodes.Clear();
        _painted = false;
        Paint();
    }

    /// Builds the whole tree from one full state pull (first frame / hot
    /// reload). Creates and applies every window, then runs the structural
    /// passes over the full tree.
    // Reconciliation may add controls even under a pinned window: pinned trees
    // retain their normal filters; transient trees are walked after reparenting.
    void SetTransientFilters(bool interactive)
    {
        foreach (var (id, window) in _windows)
            if (id.EndsWith(HoverWindowSuffix, StringComparison.Ordinal) && !IsProtected(id) && !IsDead(window))
                window.SetSubtreeInteractive(interactive);
    }

    void FullPaint()
    {
        var nodes = UiState.FetchState();
        if (nodes.Count == 0) return;
        _nodes = nodes;
        _structureDirty = true;
        EnsureStructureCaches();
        var byId = _byIdCache;
        CreateOrUpdateAll();
        FlattenTextChildren(nodes, byId);
        RefreshFlattenedFieldValues(byId);
        ReparentAll(byId);
        PositionAll(byId);
        SetTransientFilters(false);
        // The full pull ran a tick that queued the just-applied adds as a
        // pending delta; drain it so the next frame starts clean.
        UiState.FetchDelta();
    }

    /// Creates any missing windows and applies every node (idempotent). Used by
    /// the full build; the steady-state delta path only touches changed nodes.
    void CreateOrUpdateAll()
    {
        foreach (var node in _nodes)
        {
            if (IsProtected(node.Id)) continue;
            RegisterTransient(node);
            if (!_windows.TryGetValue(node.Id, out var win))
            {
                // Folded field children render as their host's "text" Label,
                // not as separate windows. Skip re-materializing them: a
                // re-created field window's own "text" label would sit in the
                // tree for the whole frame and show a stale value over the
                // host's live label.
                if (_flattenedFields.ContainsKey(node.Id)) continue;
                win = CreateWindow(node);
                if (node.Kind == UiNodeKind.Screen)
                {
                    // The screen is a global overlay: host it on the UI layer
                    // root so it renders below the app buttons and windows. It
                    // must be the FIRST child there (Godot dispatches GUI input
                    // by reverse child index): screen < coreUI < moduleUI.
                    ScreenHost.AddChild(win);
                    ScreenHost.MoveChild(win, 0);
                }
                else
                    AddChild(win);
                _windows[node.Id] = win;
            }
            win.Apply(node);
            win.SetChildren(node.Children);
        }

        // Remove windows no longer declared.
        var byId = _nodes.ToDictionary(n => n.Id);
        foreach (var id in _windows.Keys.ToArray())
        {
            if (!byId.ContainsKey(id) && !_flattened.Contains(id) && !IsProtected(id))
                RemoveRenderedNode(id);
        }
    }

    /// Structural passes over the current tree. Runs only when a delta arrived
    /// (a clean frame skips all of it).
    void LayoutPasses()
    {
        EnsureStructureCaches();
        var byId = _byIdCache;
        _paintSw.Restart();
        FlattenTextChildren(_nodes, byId);
        long flattenTicks = _paintSw.ElapsedTicks;
        _paintSw.Restart();
        RefreshFlattenedFieldValues(byId);
        long refreshTicks = _paintSw.ElapsedTicks;
        _paintSw.Restart();
        ReparentAll(byId);
        long reparentTicks = _paintSw.ElapsedTicks;
        _paintSw.Restart();
        // Flow content lands in its container during ReparentAll, after the
        // first Apply — so re-assert the content-driven minimum sizes here
        // (cheap) instead of relying on a per-frame re-apply.
        foreach (var win in _windows.Values)
            win.RefreshFlowMinimumSize();
        long flowTicks = _paintSw.ElapsedTicks;
        _paintSw.Restart();
        PositionAll(byId);
        SetTransientFilters(false);
        long positionTicks = _paintSw.ElapsedTicks;
        LogLayoutSpike(flattenTicks, refreshTicks, reparentTicks, flowTicks, positionTicks);
    }

    /// Diagnostic: on any frame whose total layout time crosses the spike
    /// threshold, log the per-sub-pass breakdown so the culprit (flatten,
    /// refresh, reparent, flow-minimum, or position) is visible.
    void LogLayoutSpike(long flatten, long refresh, long reparent, long flow, long position)
    {
        if (!EnablePaintTimingLog) return;
        double totalMs = Ms(flatten + refresh + reparent + flow + position);
        if (totalMs < 10.0) return;
        var detail = $"flatten={Ms(flatten):0.00} refresh={Ms(refresh):0.00} " +
                     $"reparent={Ms(reparent):0.00} flow={Ms(flow):0.00} " +
                     $"position={Ms(position):0.00}";
        if (totalMs > LastLayoutSpikeTotal)
        {
            LastLayoutSpikeTotal = totalMs;
            LastLayoutSpikeDetail = detail;
        }
        DiagnosticLogger($"[layout-spike] total={totalMs:0.00} | {detail} (ms)");
    }

    /// Rebuilds the id→node and child→parent maps from <see cref="_nodes"/>
    /// when they are stale. Called before any pass that reads the maps, so a
    /// clean frame (no node change) reuses the cached maps and skips the O(n)
    /// rebuild. <see cref="_structureDirty"/> is latched by every mutation of
    /// <see cref="_nodes"/> (full paint, upsert, remove).
    void EnsureStructureCaches()
    {
        if (!_structureDirty) return;
        _byIdCache = _nodes.ToDictionary(n => n.Id);
        _parentOfCache.Clear();
        foreach (var other in _nodes)
            foreach (var c in other.Children)
                _parentOfCache[c] = other.Id;
        _structureDirty = false;
    }

    /// Applies a change delta (add/update/remove) to the affected windows and
    /// keeps <see cref="_nodes"/> in sync. Ops are idempotent, so a stale or
    /// redundant op is a harmless no-op.
    void ApplyDelta(UiDelta delta)
    {
        // Restore defaults before Apply mutates controls, then capture the new
        // defaults after the structural passes, including newly added children.
        SetTransientFilters(true);
        foreach (var op in delta.Ops)
        {
            switch (op.Op)
            {
                case "add":
                case "update":
                    if (IsProtected(op.Node.Id))
                    {
                        UpsertNode(op.Node);
                        // Keep live field values current without re-applying window
                        // options or disturbing a pinned subtree's layout and pin.
                        if (op.Node.Kind == UiNodeKind.Field && !_flattenedFields.ContainsKey(op.Node.Id)
                            && _windows.TryGetValue(op.Node.Id, out var fieldWin) && !IsDead(fieldWin))
                            fieldWin.Apply(op.Node);
                        break;
                    }
                    RegisterTransient(op.Node);
                    // Always keep the structure current: a flattened field has
                    // no window, but its host "text" Label is refreshed from
                    // _nodes in LayoutPasses, so the node must be upserted even
                    // when no window exists.
                    UpsertNode(op.Node);
                    if (_flattenedFields.ContainsKey(op.Node.Id)) break;
                    if (!_windows.TryGetValue(op.Node.Id, out var win))
                    {
                        win = CreateWindow(op.Node);
                        if (op.Node.Kind == UiNodeKind.Screen)
                        {
                            ScreenHost.AddChild(win);
                            ScreenHost.MoveChild(win, 0);
                        }
                        else
                            AddChild(win);
                        _windows[op.Node.Id] = win;
                    }
                    if (!IsDead(win))
                    {
                        win.Apply(op.Node);
                        win.SetChildren(op.Node.Children);
                    }
                    break;
                case "remove":
                    if (_transientWindows.TryGetValue(op.Id + HoverWindowSuffix, out var sourceLifecycle))
                        sourceLifecycle.SourceRemoved();
                    if (_transientWindows.TryGetValue(op.Id, out var removedLifecycle))
                        removedLifecycle.RemoveDelta();
                    if (!IsProtected(op.Id)) RemoveRenderedNode(op.Id);
                    RemoveNode(op.Id);
                    break;
            }
        }
    }

    void UpsertNode(UiNodeData node)
    {
        var i = _nodes.FindIndex(n => n.Id == node.Id);
        if (i >= 0) _nodes[i] = node;
        else _nodes.Add(node);
        _structureDirty = true;
    }

    void RemoveNode(string id)
    {
        var i = _nodes.FindIndex(n => n.Id == id);
        if (i >= 0) _nodes.RemoveAt(i);
        _structureDirty = true;
    }

    /// Re-parents every window under its declared parent. The child→parent map
    /// is the cached <see cref="_parentOfCache"/> (rebuilt in
    /// <see cref="LayoutPasses"/> only when the node set changes) rather than
    /// rebuilt every frame.
    void ReparentAll(Dictionary<string, UiNodeData> byId)
    {
        var parentOf = _parentOfCache;

        foreach (var node in _nodes)
        {
            // The screen itself stays on the screen host; its children are
            // reparented under it as usual.
            if (node.Kind == UiNodeKind.Screen || IsProtected(node.Id)) continue;
            if (!_windows.TryGetValue(node.Id, out var win)) continue;
            UiWindow? parent = null;
            if (parentOf.TryGetValue(node.Id, out var pid))
                _windows.TryGetValue(pid, out parent);
            Reparent(win, parent, byId[node.Id], FirstChildAnchor(node, byId) != null);
        }
    }

    // First is the first declared child ID, including non-window children.
    string? FirstChildAnchor(UiNodeData node, Dictionary<string, UiNodeData> byId)
    {
        if (node.Kind != UiNodeKind.Window || !_parentOfCache.TryGetValue(node.Id, out var id)
            || !byId.TryGetValue(id, out var parent)
            || parent.Kind is not (UiNodeKind.Window or UiNodeKind.Screen)
            || parent.Children.Count == 0 || parent.Children[0] != node.Id) return null;
        try
        {
            using var options = System.Text.Json.JsonDocument.Parse(parent.OptionsJson);
            if (options.RootElement.ValueKind == System.Text.Json.JsonValueKind.Object
                && options.RootElement.TryGetProperty("anchorChild", out var value)
                && value.ValueKind == System.Text.Json.JsonValueKind.String)
                return value.GetString();
        }
        catch (System.Text.Json.JsonException) { }
        return null;
    }

    // Report each unresolved edge once until it becomes valid again.
    readonly HashSet<string> _positionWarnings = new();

    /// Positions every window against its parent's rect.
    void PositionAll(Dictionary<string, UiNodeData> byId)
    {
        var mouse = SimulatedMouse ?? GetGlobalMousePosition();
        foreach (var kv in _windows)
        {
            var win = kv.Value;
            // Godot sanitizes node names (e.g. ':' becomes '_'); the dictionary
            // key retains the original runtime ID used by the snapshot.
            if (_flattened.Contains(kv.Key) || IsProtected(kv.Key)) continue;
            if (!byId.TryGetValue(kv.Key, out var node)) continue;
            // The screen always fills the viewport (FullRect); positioning it
            // against the viewport rect would shrink it to content size.
            if (node.Kind == UiNodeKind.Screen) continue;
            // Flow-embedded windows are sized/placed by the flow container.
            if (win.GetParent() is BoxContainer || win.GetParent() is UiGrid)
                continue;
            win.SetPositioningMode(true);
            Rect2 parentRect;
            var parentWin = win.GetParent() as UiWindow;
            if (parentWin == null)
            {
                // Top-level: against the root (viewport) rect.
                parentRect = new Rect2(Vector2.Zero, Size);
            }
            else
            {
                // Direct child: its local origin is the parent's top-left
                // corner, so the parent's rect in the child's local space has
                // origin (0,0). Using a global delta here would not be
                // idempotent (each repaint would drift the child's position).
                parentRect = new Rect2(Vector2.Zero, parentWin.Size);
            }
            win.PositionWithin(parentRect, node, FirstChildAnchor(node, byId));
        }
        // Resolve dependencies after ordinary layout, in stable ID order. A
        // cycle uses the ordinary-layout position for the broken edge.
        var visiting = new HashSet<string>();
        var done = new HashSet<string>();
        var warnings = new HashSet<string>();
        void WarnOnce(string message)
        {
            warnings.Add(message);
            if (!_positionWarnings.Contains(message)) GD.PushWarning(message);
        }
        void Resolve(string id)
        {
            if (done.Contains(id) || IsProtected(id) || !_windows.TryGetValue(id, out var win)) return;
            visiting.Add(id);
            if (win.PositionAtCursor)
            {
                win.PositionRelative(new Rect2(mouse, Vector2.Zero));
            }
            else if (win.PositionTarget is string target)
            {
                if (visiting.Contains(target))
                    WarnOnce($"Window positioning cycle at {id} -> {target}");
                else if (_windows.TryGetValue(target, out var targetWin))
                {
                    Resolve(target);
                    win.PositionRelative(targetWin.GetGlobalRect());
                }
                else
                {
                    WarnOnce($"Window positioning target not found: {target}");
                    var parent = win.GetParent() as Control;
                    win.PositionRelative(new Rect2(parent?.GlobalPosition ?? Vector2.Zero, Vector2.Zero));
                }
            }
            visiting.Remove(id);
            done.Add(id);
        }
        foreach (var id in _windows.Keys.OrderBy(id => id, StringComparer.Ordinal)) Resolve(id);
        _positionWarnings.Clear();
        _positionWarnings.UnionWith(warnings);
    }

    static UiWindow CreateWindow(UiNodeData node)
    {
        var win = new UiWindow { Name = node.Id };
        if (node.Kind == UiNodeKind.Screen)
        {
            // The global full-screen screen: always fills the viewport
            // (FullRect, no positioning pass). It is hosted on the UI layer
            // root at ZIndex 0 — below the app buttons (coreUI, z 1) and the
            // module windows (moduleUI, z 2).
            win.SetAnchorsPreset(LayoutPreset.FullRect, true);
        }
        return win;
    }

    /// The host for the global screen: the UI layer root (this root's
    /// grandparent), a sibling of coreUI (z 1) and moduleUI (z 2), so the
    /// screen (z 0) renders below both. Falls back to this root when the
    /// layer hierarchy is not present.
    Node ScreenHost => GetParent()?.GetParent() ?? this;

    /// True when the window has been queued for deletion (and is therefore
    /// unsafe to touch through the Godot API once the frame frees it).
    static bool IsDead(UiWindow win)
    {
        return win == null || win.IsQueuedForDeletion();
    }
}
