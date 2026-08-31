using Godot;
using NewGameProject.UI;
using System.Text.Json;
using RuntimeInterop = NewGameProject.Runtime.RuntimeInterop;

namespace GdUnit4.Examples.Basics.Setup.Sources.UI;

/// Leaf content rendering: text labels, archive images, and the 2D world
/// canvas (SubViewport + UiWorldCanvas).
public partial class UiWindow
{
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
}
