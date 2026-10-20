using Godot;
using NewGameProject.Runtime;

namespace GdUnit4.Examples.Basics.Setup.Sources.UI;

public partial class HoverOutline :Control {

    public MarginContainer MarginContainer;
    public UiTextureSurface Surface;

    public HoverOutline(Hover? hover = null, UiPaintSpec? paint = null) {
        Name = "HoverOutline";
        MarginContainer = new();
        Surface = new UiTextureSurface { MouseFilter = Control.MouseFilterEnum.Ignore };
        Surface.TextureFilter = TextureFilterEnum.Nearest;
        MarginContainer.AddChild(Surface);
        AddChild(MarginContainer);
        

        if (hover.HasValue) {
            var h = hover.Value;
            if (MarginContainer != null) {
                MarginContainer.AddThemeConstantOverride("margin_left", -h.Thickness);
                MarginContainer.AddThemeConstantOverride("margin_right", -h.Thickness);
                MarginContainer.AddThemeConstantOverride("margin_top", -h.Thickness);
                MarginContainer.AddThemeConstantOverride("margin_bottom", -h.Thickness);
            }
            if (!string.IsNullOrEmpty(h.Texture)) {
                var files = RuntimeInterop.GetFileFromArchive();
                if (files.TryGetValue(h.Texture, out var imageData)) {
                    Image img = new Image();
                    img.LoadPngFromBuffer(imageData);
                    // Explicit paint (from onHover.texturePaint) overrides the
                    // default centered nine-slice frame at the outline thickness.
                    var spec = paint ?? UiPaintSpec.FrameCentered(h.Thickness);
                    Surface.Update(spec, ImageTexture.CreateFromImage(img));
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
