using Castle.Components.DictionaryAdapter.Xml;
using Godot;
using NewGameProject.Runtime;
using Vector2 = Godot.Vector2;

public partial class Panel : Godot.Panel {
    private NewGameProject.Runtime.Panel panel;
    public NewGameProject.Runtime.Panel ChildPanel {
        get {
            if (panel.Children != null && panel.Children.Length > 0) return panel.Children[0];
            return panel;
        }
    }
    
    public Panel(NewGameProject.Runtime.Panel panel) {
        Name = panel.Id;
        UniqueNameInOwner = true;
        Owner = GetParent();
        this.panel = panel;
        AnchorTop = panel.Anchor.Y;
        AnchorBottom = panel.Anchor.Y;
        AnchorLeft = panel.Anchor.X;
        AnchorRight = panel.Anchor.X;
        OffsetTop = panel.Offset.top;
        OffsetBottom = panel.Offset.bottom;
        OffsetLeft = panel.Offset.left;
        OffsetRight = panel.Offset.right;
        SetCustomMinimumSize(new Vector2(panel.Size.Width, panel.Size.Height));
        GrowHorizontal = GrowDirection.Both;
        GrowVertical = GrowDirection.Both;
        ClipContents = true;

        // Debug: print incoming native panel values to help diagnose anchor/size mapping issues
        GD.Print(
            $"DEBUG Panel: id={panel.Id} anchor=({panel.Anchor.X},{panel.Anchor.Y}) pivot=({panel.Pivot.X},{panel.Pivot.Y}) offset=({panel.Offset.top},{panel.Offset.bottom},{panel.Offset.left},{panel.Offset.right}) size=({panel.Size.Width},{panel.Size.Height}) background={(panel.Background ?? "null")}");
        
        // Debug: print OnClick handler info
        if (panel.OnClick.HasValue) {
            GD.Print($"DEBUG Panel OnClick: id={panel.Id} actionName={panel.OnClick.Value.ActionName}");
        } else {
            GD.Print($"DEBUG Panel OnClick: id={panel.Id} NONE");
        }
        //if panel.Content is instance of ConstantTextContent, add a RichTextLabel
        if (panel.Content is ConstantTextContent constantTextContent) {
            var richTextLabel = new RichTextLabel() {
                Name = "content",
                FitContent = true,
                Text = constantTextContent.Value,
            };
            richTextLabel.SetJustificationFlags(TextServer.JustificationFlag.None);
            richTextLabel.SetAutowrapMode(TextServer.AutowrapMode.WordSmart);
            richTextLabel.SetAnchorsPreset(LayoutPreset.FullRect);
            AddChild(richTextLabel);
        }

        if (panel.Background != null) {
            var Files = RuntimeInterop.GetFileFromArchive();
            if (Files.TryGetValue(panel.Background, out var imageData)) {
                Image img = new Image();
                img.LoadExrFromBuffer(imageData);
                TextureFilter = TextureFilterEnum.Nearest;
                AddThemeStyleboxOverride("panel", new StyleBoxTexture {
                    Texture = ImageTexture.CreateFromImage(img),
                });
            }
        }


        if (panel.Children != null) {
            var gridOrderContainer = new BoxContainer {
                Name = "gridOrder",
                Vertical = false,
            };
            gridOrderContainer.AddThemeConstantOverride("separation", 0);
            AddChild(gridOrderContainer);

            var numberOfTracks = panel.Layout?.Columns?.Length ?? 1;

            var tracks = new List<BoxContainer>();
            foreach (var i in Enumerable.Range(0, numberOfTracks)) {
                var trackElement = new BoxContainer {
                    Name = "track_" + i,
                    Vertical = true,
                    
                };
                trackElement.AddThemeConstantOverride("separation", 0);
                tracks.Add(trackElement);
                gridOrderContainer.AddChild(trackElement);
            }

            for (int i = 0; i < panel.Children.Length; i++) {
                var child = panel.Children[i];
                var p = new Panel(child) {
                    Name = child.Id,
                    UniqueNameInOwner = true
                };
                tracks.ElementAt(i % numberOfTracks).AddChild(p);
                p.SetOwner(this);
            }
        }
    }

    

    public override void _GuiInput(InputEvent @event) {
        // Check if the event is a mouse button click
        if (@event is InputEventMouseButton mouseEvent) {
            // .Pressed ensures we trigger on 'down', and Mask checks for Left Click
            if (mouseEvent.Pressed && mouseEvent.ButtonIndex == MouseButton.Left) {
                var actionName = panel.OnClick?.ActionName;
                if (actionName != null) {
                    GD.Print($"{panel.Id}: Emitting action: {actionName}");
                    RuntimeInterop.emitAction(actionName);
                }
                // Optional: Stop the event from bubbling up to parent nodes
                GetViewport().SetInputAsHandled();
            }
        }

        if (@event is InputEventMouseMotion mouseMotion) {
            Vector2 localPos = mouseMotion.Position;
            AddChild(new Label() { Text = "" + localPos.ToString() + "" });
            GetViewport().SetInputAsHandled();
            // Do something with the specific mouse position inside the panel
        }
    }


    // Called when the node enters the scene tree for the first time.
    public override void _Ready() {
    }

    public override void _EnterTree() {
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta) {
    }
}