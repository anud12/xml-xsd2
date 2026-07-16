using Godot;
using NewGameProject.Runtime;

namespace GdUnit4.Examples.Basics.Setup.Sources.UI;

public partial class HoverOutline :Control {

    public MarginContainer MarginContainer;
    public NinePatchRect NinePatchRect;

    public HoverOutline(Hover? hover = null) {
        Name = "HoverOutline";
        MarginContainer = new();
        NinePatchRect = new();
        MarginContainer.AddChild(NinePatchRect);
        AddChild(MarginContainer);
        
        
        
        if (hover.HasValue) {
            var h = hover.Value;
            if (MarginContainer != null) {
                MarginContainer.AddThemeConstantOverride("margin_left", -h.Thickness);
                MarginContainer.AddThemeConstantOverride("margin_right", -h.Thickness);
                MarginContainer.AddThemeConstantOverride("margin_top", -h.Thickness);
                MarginContainer.AddThemeConstantOverride("margin_bottom", -h.Thickness);
            }
            if (NinePatchRect != null && !string.IsNullOrEmpty(h.Texture)) {
                var files = RuntimeInterop.GetFileFromArchive();
                if (files.TryGetValue(h.Texture, out var imageData)) {
                    Image img = new Image();
                    img.LoadPngFromBuffer(imageData);
                    NinePatchRect.Texture = ImageTexture.CreateFromImage(img);
                    NinePatchRect.TextureFilter = TextureFilterEnum.Nearest;
                    NinePatchRect.DrawCenter = false;
                    NinePatchRect.PatchMarginLeft = h.Thickness;
                    NinePatchRect.PatchMarginTop = h.Thickness;
                    NinePatchRect.PatchMarginBottom = h.Thickness;
                    NinePatchRect.PatchMarginRight = h.Thickness;
                }
            }
        }
    }

    public void Resize() {
        this.SetAnchorsPreset(LayoutPreset.FullRect);
        MarginContainer.SetAnchorsPreset(LayoutPreset.FullRect);
        MarginContainer.LayoutMode = 1;
    }
    public override void _Ready() {
        base._Ready();
        
        
    }
}
