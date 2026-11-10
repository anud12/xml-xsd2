using Godot;
using NewGameProject.UI;

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
        Pump();
        base._Process(delta);
    }

    /// Drives one frame of painting + hover tracking (used by tests to pump
    /// the node without relying on the engine process callback).
    public void Pump()
    {
        Paint();
        UpdateHoverTracking();
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
        var delta = UiState.FetchDelta();
        if (delta != null && delta.Ops.Count > 0)
            ApplyDelta(delta);
        LayoutPasses();
    }

    /// Force a full rebuild (module hot reload): free every window and repaint
    /// from a fresh full state pull.
    public void ForceFullPaint()
    {
        foreach (var win in _windows.Values)
            win.QueueFree();
        _windows.Clear();
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
    void FullPaint()
    {
        var nodes = UiState.FetchState();
        if (nodes.Count == 0) return;
        _nodes = nodes;
        var byId = nodes.ToDictionary(n => n.Id);
        CreateOrUpdateAll();
        FlattenTextChildren(nodes, byId);
        RefreshFlattenedFieldValues(byId);
        ReparentAll(byId);
        PositionAll(byId);
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
            if (!byId.ContainsKey(id) && !_flattened.Contains(id))
            {
                var removed = _windows[id];
                removed.QueueFree();
                _windows.Remove(id);
                _hoverStates.Remove(removed);
                UiState.RemoveAnimStartTime(id);
            }
        }
    }

    /// Structural passes over the current tree. Runs only when a delta arrived
    /// (a clean frame skips all of it).
    void LayoutPasses()
    {
        var byId = _nodes.ToDictionary(n => n.Id);
        FlattenTextChildren(_nodes, byId);
        RefreshFlattenedFieldValues(byId);
        ReparentAll(byId);
        // Flow content lands in its container during ReparentAll, after the
        // first Apply — so re-assert the content-driven minimum sizes here
        // (cheap) instead of relying on a per-frame re-apply.
        foreach (var win in _windows.Values)
            win.RefreshFlowMinimumSize();
        PositionAll(byId);
    }

    /// Applies a change delta (add/update/remove) to the affected windows and
    /// keeps <see cref="_nodes"/> in sync. Ops are idempotent, so a stale or
    /// redundant op is a harmless no-op.
    void ApplyDelta(UiDelta delta)
    {
        foreach (var op in delta.Ops)
        {
            switch (op.Op)
            {
                case "add":
                case "update":
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
                    if (_windows.TryGetValue(op.Id, out var rem))
                    {
                        if (!IsDead(rem)) rem.QueueFree();
                        _hoverStates.Remove(rem);
                        UiState.RemoveAnimStartTime(op.Id);
                        _windows.Remove(op.Id);
                    }
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
    }

    void RemoveNode(string id)
    {
        var i = _nodes.FindIndex(n => n.Id == id);
        if (i >= 0) _nodes.RemoveAt(i);
    }

    /// Re-parents every window under its declared parent. The child→parent map
    /// is built once (O(n)) rather than scanning all nodes per child.
    void ReparentAll(Dictionary<string, UiNodeData> byId)
    {
        var parentOf = new Dictionary<string, string>();
        foreach (var other in _nodes)
            foreach (var c in other.Children)
                parentOf[c] = other.Id;

        foreach (var node in _nodes)
        {
            // The screen itself stays on the screen host; its children are
            // reparented under it as usual.
            if (node.Kind == UiNodeKind.Screen) continue;
            if (!_windows.TryGetValue(node.Id, out var win)) continue;
            UiWindow? parent = null;
            if (parentOf.TryGetValue(node.Id, out var pid))
                _windows.TryGetValue(pid, out parent);
            Reparent(win, parent, byId[node.Id]);
        }
    }

    /// Positions every window against its parent's rect.
    void PositionAll(Dictionary<string, UiNodeData> byId)
    {
        foreach (var kv in _windows)
        {
            var win = kv.Value;
            if (_flattened.Contains(win.Name)) continue;
            if (!byId.TryGetValue(win.Name, out var node)) continue;
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
            win.PositionWithin(parentRect, node);
        }
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
