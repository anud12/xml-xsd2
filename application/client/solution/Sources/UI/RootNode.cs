using GdUnit4.Examples.Basics.Setup.Sources.UI;
using Godot;
using NewGameProject.Runtime;
using NewGameProject.UI;
using Vector2 = Godot.Vector2;

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

    /// Polls the mouse position each frame and drives hover enter/exit for
    /// every window, because the engine's built-in MouseEntered/MouseExited
    /// signals are not delivered reliably for Controls created at runtime in
    /// headless test runs.
    /// <summary>
    /// Authoritative mouse position for hover tracking when set (test
    /// simulation). The live cursor is only used when this is null, so the
    /// developer's real mouse moving over the runner window cannot cancel a
    /// simulated hover between frames.
    /// </summary>
    public static Vector2? SimulatedMouse;

    void UpdateHoverTracking()
    {
        // The deepest hover-capable window under the mouse owns the hover.
        // Ancestors whose rect contains the mouse do not fire their own
        // hover while a descendant owns it (this is stopPropagation), and a
        // child without its own hover never "covers" its parent, so the
        // parent's hover fires when the child is hovered (bubble-up).
        var sim = SimulatedMouse;
        var mouse = sim.HasValue
            ? sim.Value
            : GetGlobalMousePosition();
        // Snapshot window list: NotifyHoverEnter/Exit may repaint.
        var windows = _windows.Values.ToArray();
        var hoverable = new List<UiWindow>();
        foreach (var win in windows)
        {
            if (win.Visible
                && win.GetGlobalRect().HasPoint(mouse))
                hoverable.Add(win);
        }
        UiWindow owner = null;
        var bestDepth = -1;
        foreach (var win in hoverable)
        {
            if (!win.IsHoverCapable()) continue;
            var d = Depth(win);
            if (d > bestDepth)
            {
                bestDepth = d;
                owner = win;
            }
        }
        foreach (var win in hoverable)
        {
            var hovered = win == owner;
            var was = _hoverStates.TryGetValue(win, out var s) && s;
            if (hovered != was)
            {
                _hoverStates[win] = hovered;
                if (hovered)
                    win.NotifyHoverEnter();
                else
                    win.NotifyHoverExit();
            }
        }
        // Windows no longer under the mouse exit.
        foreach (var win in windows)
        {
            if (hoverable.Contains(win)) continue;
            var was = _hoverStates.TryGetValue(win, out var s) && s;
            if (was)
            {
                _hoverStates[win] = false;
                win.NotifyHoverExit();
            }
        }
    }

    int _dbgOwnerLogged;

    static int Depth(Node n)
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

    /// A window whose only content is a single text/field child renders that
    /// text directly as a "text" Label on the window itself (the child node
    /// is folded away), matching the legacy panel content semantics.
    void FlattenTextChildren(List<UiNodeData> nodes, Dictionary<string, UiNodeData> byId)
    {
        foreach (var node in nodes)
        {
            if (node.Kind != UiNodeKind.Window && node.Kind != UiNodeKind.Division) continue;
            if (node.Children.Count != 1) continue;
            if (!byId.TryGetValue(node.Children[0], out var childNode)) continue;
            if (childNode.Kind != UiNodeKind.Text && childNode.Kind != UiNodeKind.Field) continue;
            if (!_windows.TryGetValue(node.Id, out var win)) continue;
            if (!byId.TryGetValue(childNode.Id, out _)) continue;
            if (win.GetNodeOrNull<Label>("text") != null) continue;

            if (_windows.TryGetValue(childNode.Id, out var childWin))
            {
                childWin.QueueFree();
                _windows.Remove(childNode.Id);
                _flattenedFields.Remove(childNode.Id);
            }
            var label = new Label
            {
                Name = "text",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            label.SetAnchorsPreset(LayoutPreset.FullRect);
            ApplyLabelAlignment(label, ParseOptions(node), ParseOptions(childNode));
            win.AddChild(label);
            label.Text = childNode.Value;
            _flattened.Add(childNode.Id);
            if (childNode.Kind == UiNodeKind.Field)
                _flattenedFields[childNode.Id] = (win, childNode.Id);
        }
    }

    /// Re-resolves the value of every flattened field node from the current
    /// entity store and pushes it into the host window's "text" Label, so
    /// entity value changes surface live (the id-keyed delta alone only
    /// re-applies node state, not the folded-away label).
    void RefreshFlattenedFieldValues(Dictionary<string, UiNodeData> byId)
    {
        foreach (var kv in _flattenedFields)
        {
            var (host, childId) = kv.Value;
            if (!byId.TryGetValue(childId, out var childNode)) continue;
            var label = host.GetNodeOrNull<Label>("text");
            if (label == null) continue;
            if (label.Text != childNode.Value)
                label.Text = childNode.Value;
        }
    }

    static System.Text.Json.JsonElement ParseOptions(UiNodeData node)
    {
        if (!string.IsNullOrEmpty(node.OptionsJson))
            return System.Text.Json.JsonDocument.Parse(node.OptionsJson).RootElement;
        return default;
    }

    /// Applies a legacy 9-point align ("top", "center-left", ...) from the
    /// host or child node options to the flattened content label.
    static void ApplyLabelAlignment(
        Label label, System.Text.Json.JsonElement hostOpts, System.Text.Json.JsonElement childOpts)
    {
        string? align = null;
        foreach (var opts in new[] { hostOpts, childOpts })
        {
            if (opts.ValueKind == System.Text.Json.JsonValueKind.Object
                && opts.TryGetProperty("align", out var a)
                && a.ValueKind == System.Text.Json.JsonValueKind.String)
            {
                align = a.GetString();
                break;
            }
        }
        if (string.IsNullOrEmpty(align)) return;
        label.HorizontalAlignment = align switch
        {
            "top-left" or "center-left" or "bottom-left" => HorizontalAlignment.Left,
            "top-right" or "center-right" or "bottom-right" => HorizontalAlignment.Right,
            _ => HorizontalAlignment.Center
        };
        label.VerticalAlignment = align switch
        {
            "top" or "top-left" or "top-right" => VerticalAlignment.Top,
            "bottom" or "bottom-left" or "bottom-right" => VerticalAlignment.Bottom,
            _ => VerticalAlignment.Center
        };
    }

    /// Moves <paramref name="win"/> under its declared parent: flow children
    /// go into the parent's flow container (box/grid), x/y-positioned child
    /// windows go directly under the parent window. Top-level nodes stay under
    /// this root.
    void Reparent(UiWindow win, UiWindow? parent, UiNodeData node)
    {
        if (parent == null || parent == win)
        {
            if (win.GetParent() != this)
                (win.GetParent() as Node)?.RemoveChild(win);
            if (win.GetParent() != this) AddChild(win);
            return;
        }
        bool hasXY = win.HasWindowXY;
        if (hasXY)
        {
            if (win.GetParent() != parent)
            {
                (win.GetParent() as Node)?.RemoveChild(win);
                parent.AddChild(win);
            }
        }
        else
        {
            var flow = parent.FlowContainer();
            if (flow == null)
            {
                // Parent has no flow container yet (e.g. text node): keep the
                // child directly under the parent window.
                if (win.GetParent() != parent)
                {
                    (win.GetParent() as Node)?.RemoveChild(win);
                    parent.AddChild(win);
                }
            }
            else
            {
                win.ApplyFixedFlowSize(win.FixedFlowSize);
                if (win.GetParent() != flow)
                {
                    (win.GetParent() as Node)?.RemoveChild(win);
                    flow.AddChild(win);
                }
            }
        }
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

    int _dbgProcessLogged;

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
