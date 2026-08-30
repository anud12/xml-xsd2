using Godot;
using NewGameProject.UI;
using System.Text.Json;
using RuntimeInterop = NewGameProject.Runtime.RuntimeInterop;

namespace GdUnit4.Examples.Basics.Setup.Sources.UI;

/// Flow container management (box/grid) and container-level visuals
/// (background border) for layout nodes.
public partial class UiWindow
{
    /// True when this node can host flow children.
    public bool IsFlowParent => FlowContainer() != null;

    /// The flow container (box/grid) holding leaf children, or null when
    /// this node has none yet.
    public Node FlowContainer()
    {
        return (Node)GetNodeOrNull<BoxContainer>("box")
            ?? (Node)GetNodeOrNull<UiGrid>("grid");
    }

    /// True when this window is a flow child with an explicit size and should
    /// be sized exactly to it (not stretched to fill the container cross-axis).
    public bool FixedFlowSize;

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

    void ApplyLayout(UiNodeData node)
    {
        var opts = ParseOptions(node);
        _layoutSpec = UiGrid.UiGridLayoutSpec.Parse(opts);
        EnsureFlowContainer();
        ApplyBackground(opts);
        ApplyBorder(opts);
    }

    /// options.border: a full-rect NinePatchRect frame around the node.
    /// `width` is the patch margin (border thickness) applied to all four
    /// sides (default 1 px), `texture` an archive PNG path. The center region
    /// is never drawn (DrawCenter = false), so only the texture's frame shows.
    void ApplyBorder(JsonElement opts)
    {
        if (opts.ValueKind == JsonValueKind.Undefined
            || !opts.TryGetProperty("border", out var border)
            || border.ValueKind != JsonValueKind.Object)
            return;
        if (!border.TryGetProperty("texture", out var t)
            || t.ValueKind != JsonValueKind.String
            || string.IsNullOrEmpty(t.GetString()))
            return;
        var path = t.GetString();
        var width = 1;
        if (border.TryGetProperty("width", out var w) && w.ValueKind == JsonValueKind.Number)
            width = Math.Max(1, (int)w.GetDouble());

        if (!RuntimeInterop.GetFileFromArchive().TryGetValue(path, out var data))
        {
            RuntimeInterop.Log($"ui: border texture not found in archive: {path}");
            return;
        }
        var img = new Image();
        img.LoadPngFromBuffer(data);
        var rect = GetNodeOrNull<NinePatchRect>("border");
        if (rect == null)
        {
            rect = new NinePatchRect { Name = "border" };
            rect.MouseFilter = Control.MouseFilterEnum.Ignore;
            AddChild(rect);
            rect.SetAnchorsPreset(LayoutPreset.FullRect, true);
        }
        rect.Texture = ImageTexture.CreateFromImage(img);
        rect.TextureFilter = TextureFilterEnum.Nearest;
        rect.DrawCenter = false;
        rect.PatchMarginLeft = width;
        rect.PatchMarginRight = width;
        rect.PatchMarginTop = width;
        rect.PatchMarginBottom = width;
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
}
