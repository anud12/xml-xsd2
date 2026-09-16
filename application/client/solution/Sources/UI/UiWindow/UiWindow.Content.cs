using Godot;
using NewGameProject.UI;
using System.Text.Json;
using RuntimeInterop = NewGameProject.Runtime.RuntimeInterop;

namespace GdUnit4.Examples.Basics.Setup.Sources.UI;

/// Leaf content rendering: text labels and archive images.
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
}
