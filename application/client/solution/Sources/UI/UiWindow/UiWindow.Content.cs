using Godot;
using NewGameProject.UI;
using System.Text.Json;
using RuntimeInterop = NewGameProject.Runtime.RuntimeInterop;

namespace GdUnit4.Examples.Basics.Setup.Sources.UI;

/// Leaf content rendering: text labels and archive images.
public partial class UiWindow
{
    static readonly Dictionary<string, FontFile> _fontCache = new();

    static FontFile LoadFontFile(string path)
    {
        if (_fontCache.TryGetValue(path, out var cached)) return cached;
        if (!RuntimeInterop.GetFileFromArchive().TryGetValue(path, out var data))
        {
            GD.PushWarning($"ui: font file not found in archive: {path}");
            return null!;
        }
        var hash = string.Join("", System.Security.Cryptography.MD5.HashData(data)
            .Take(8).Select(b => b.ToString("x2")));
        var userFonts = ProjectSettings.GlobalizePath("user://fonts");
        System.IO.Directory.CreateDirectory(userFonts);
        var ttfPath = System.IO.Path.Combine(userFonts, "f_" + hash + ".ttf");
        System.IO.File.WriteAllBytes(ttfPath, data);
        var globPath = ProjectSettings.GlobalizePath("user://fonts/f_" + hash + ".ttf");
        var font = ResourceLoader.Load<FontFile>(globPath);
        _fontCache[path] = font;
        return font;
    }

    internal void ApplyFont(Label label, UiFontData font)
    {
        var fontFile = LoadFontFile(font.File);
        if (fontFile != null)
            label.AddThemeFontOverride("font", fontFile);
        label.AddThemeFontSizeOverride("font_size", (int)font.Size);

        byte a = (byte)(font.Color >> 24);
        byte r = (byte)(font.Color >> 16);
        byte g = (byte)(font.Color >> 8);
        byte b = (byte)font.Color;
        label.AddThemeColorOverride("font_color", new Color(r / 255f, g / 255f, b / 255f, a / 255f));
    }

    void ApplyText(UiNodeData node)
    {
        _isText = true;
        if (GetNodeOrNull<Label>("text") == null)
        {
            var lbl = new Label { Name = "text" };
            lbl.SetAnchorsPreset(LayoutPreset.FullRect);
            lbl.HorizontalAlignment = HorizontalAlignment.Center;
            lbl.VerticalAlignment = VerticalAlignment.Center;
            AddChild(lbl);
        }
        var label = GetNode<Label>("text");
        label.Text = node.Value;
        if (node.Font != null)
            ApplyFont(label, node.Font);
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
