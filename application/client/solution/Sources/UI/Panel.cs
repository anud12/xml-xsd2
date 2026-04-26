using Godot;
using NewGameProject.Runtime;
using Vector2 = Godot.Vector2;

public partial class Panel : Godot.Panel
{
    private NewGameProject.Runtime.Panel panel;

    public Panel(NewGameProject.Runtime.Panel panel)
    {
        this.panel = panel;
        AnchorTop = panel.Anchor.Y;
        AnchorLeft = panel.Anchor.X;
        OffsetTop = panel.Offset.top;
        OffsetBottom = panel.Offset.bottom;
        OffsetLeft = panel.Offset.left;
        OffsetRight = panel.Offset.right;
        SetCustomMinimumSize(new Vector2(panel.Size.Width, panel.Size.Height));
        GrowHorizontal = GrowDirection.Both;
        GrowVertical = GrowDirection.Both;

        // Debug: print incoming native panel values to help diagnose anchor/size mapping issues
        GD.Print($"DEBUG Panel: id={panel.Id} anchor=({panel.Anchor.X},{panel.Anchor.Y}) pivot=({panel.Pivot.X},{panel.Pivot.Y}) offset=({panel.Offset.top},{panel.Offset.bottom},{panel.Offset.left},{panel.Offset.right}) size=({panel.Size.Width},{panel.Size.Height}) background={(panel.Background ?? "null")}");


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
        panel.Children?.ToList().ForEach(child =>
        {
            AddChild(new Panel(child)
            {
                Name = child.Id
            });
        });
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        
    }

    public override void _EnterTree()
    {
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
    }
}