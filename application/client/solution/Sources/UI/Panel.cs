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
    }
}