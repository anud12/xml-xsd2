using Godot;
using NewGameProject.UI;

namespace GdUnit4.Examples.Basics.Setup.Sources.UI;

/// <summary>
/// The root UI node for the <c>.ui</c> layer. Paints the runtime's UI tree
/// (<see cref="UiState.FetchState"/>) as <see cref="UiWindow"/> nodes: top-level
/// windows are positioned against the viewport rect, flow children are nested
/// in the parent's flow container, and x/y-positioned child windows are nested
/// directly in the parent window and positioned against the parent's rect.
/// Deltas from <see cref="UiState.FetchDelta"/> are applied per frame so live
/// field values and structural changes reconcile without a full repaint.
/// </summary>
public partial class RootNode : Godot.Panel
{
    readonly Dictionary<string, UiWindow> _windows = new();
    readonly HashSet<string> _flattened = new();
    // For each flattened field/text child id, the host window that owns its
    // "text" Label and the child's node id (so the label value can be
    // re-resolved from the entity store each frame).
    readonly Dictionary<string, (UiWindow host, string childId)> _flattenedFields = new();

    public RootNode()
    {
        SetAnchorsPreset(LayoutPreset.FullRect);
        // The root spans the full viewport but must not consume mouse input,
        // otherwise the window nodes it hosts never receive MouseEntered /
        // GuiInput (hover and click).
        MouseFilter = Control.MouseFilterEnum.Ignore;
    }

    readonly Dictionary<UiWindow, bool> _hoverStates = new();

    /// <summary>
    /// Paints the current UI state (idempotent: re-uses existing windows),
    /// re-parents children under their declared parents, and applies any
    /// pending delta.
    /// </summary>

    public void Paint()
    {
        var nodes = UiState.FetchState();
        if (nodes.Count == 0) return;
        var byId = nodes.ToDictionary(n => n.Id);

        // Pass 1: create/apply all nodes (so flow containers exist).
        foreach (var node in nodes)
        {
            if (!_windows.TryGetValue(node.Id, out var win))
            {
                win = CreateWindow(node);
                AddChild(win);
                _windows[node.Id] = win;
            }
            win.Apply(node);
            win.SetChildren(node.Children);
        }

        // Remove windows no longer declared.
        foreach (var id in _windows.Keys.ToArray())
        {
            if (!byId.ContainsKey(id) && !_flattened.Contains(id))
            {
                var removed = _windows[id];
                removed.QueueFree();
                _windows.Remove(id);
                _hoverStates.Remove(removed);
            }
        }

        // A window with exactly one text/field child renders that text
        // directly as a "text" Label (the child window is folded away), so
        // content assertions target the host window.
        FlattenTextChildren(nodes, byId);
        RefreshFlattenedFieldValues(byId);

        var delta = UiState.FetchDelta();
        if (delta != null)
            ApplyDelta(delta);

        // Pass 2: re-parent each node under its declared parent.
        foreach (var node in nodes)
        {
            if (!_windows.TryGetValue(node.Id, out var win)) continue;
            UiWindow? parent = null;
            foreach (var other in nodes)
            {
                if (other.Children.Contains(node.Id))
                {
                    _windows.TryGetValue(other.Id, out parent);
                    break;
                }
            }
            Reparent(win, parent, byId[node.Id]);
        }

        // Pass 3: position each window against its parent's rect.
        foreach (var kv in _windows)
        {
            var win = kv.Value;
            if (_flattened.Contains(win.Name)) continue;
            if (!byId.TryGetValue(win.Name, out var node)) continue;
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
        return new UiWindow { Name = node.Id };
    }

    void ApplyDelta(UiDelta delta)
    {
        foreach (var op in delta.Ops)
        {
            switch (op.Op)
            {
                case "add":
                case "update":
                    if (_windows.TryGetValue(op.Node.Id, out var win))
                    {
                        win.Apply(op.Node);
                        win.SetChildren(op.Node.Children);
                    }
                    break;
                case "remove":
                    if (_windows.TryGetValue(op.Id, out var rem))
                    {
                        rem.QueueFree();
                        _windows.Remove(op.Id);
                    }
                    break;
            }
        }
    }

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
}
