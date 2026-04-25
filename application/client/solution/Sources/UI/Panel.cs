using Godot;
using NewGameProject.Runtime;
using Vector2 = Godot.Vector2;

public partial class Panel : Godot.Panel
{
    private NewGameProject.Runtime.Panel panel;

    public Panel(NewGameProject.Runtime.Panel panel)
    {
        this.panel = panel;
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        AnchorTop = panel.Anchor.Y;
        AnchorLeft = panel.Anchor.X;
        SetCustomMinimumSize(new Vector2(panel.Size.Width, panel.Size.Height));
        GrowHorizontal = GrowDirection.Both;
        GrowVertical = GrowDirection.Both;

        // Debug: print incoming native panel values to help diagnose anchor/size mapping issues
        GD.Print($"DEBUG Panel: id={panel.Id} anchor=({panel.Anchor.X},{panel.Anchor.Y}) pivot=({panel.Pivot.X},{panel.Pivot.Y}) offset=({panel.Offset.top},{panel.Offset.bottom},{panel.Offset.left},{panel.Offset.right}) size=({panel.Size.Width},{panel.Size.Height}) background={(panel.Background ?? "null")}");

        // OffsetTop = panel.Offset.top;
        // OffsetBottom = panel.Offset.bottom;
        // OffsetLeft = panel.Offset.left;
        // OffsetRight = panel.Offset.right;
        
        if (panel.Background != null)
        {
            var Files = RuntimeInterop.GetFileFromArchive();
            var imageData = Files[panel.Background];
            Image img = new Image();
            img.LoadExrFromBuffer(imageData);
            TextureFilter = TextureFilterEnum.Nearest;
            AddThemeStyleboxOverride("panel", new StyleBoxTexture
            {
                Texture = ImageTexture.CreateFromImage(img),
                
            });
        }
        
        AddChild(new Label {Text = panel.Id});
    }

    public override void _EnterTree()
    {
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        // Defer positioning until the parent has been sized. Once applied, stop processing.
        // var parent = GetParent() as Control;
        // if (parent == null) { return; }
        // var parentSize = parent.Size;
        // if (parentSize.X > 0 && parentSize.Y > 0)
        // {
        //     // Position the panel at the anchor point within the parent (no pivot offset)
        //     // Position = new Vector2(panel.Anchor.X * parentSize.X, panel.Anchor.Y * parentSize.Y);
        //     SetProcess(false);
        // }
    }
}