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

    public Panel(NewGameProject.Runtime.Panel panel, bool isRoot = true) {
        Name = panel.Id;
        UniqueNameInOwner = true;
        Owner = GetParent();
        this.panel = panel;
        AnchorTop = panel.Anchor.Y;
        AnchorBottom = panel.Anchor.Y;
        AnchorLeft = panel.Anchor.X;
        AnchorRight = panel.Anchor.X;
        if (isRoot) {
            OffsetTop = panel.Offset.top - panel.Size.Height / 2f;
            OffsetBottom = panel.Offset.top + panel.Size.Height / 2f;
            OffsetLeft = panel.Offset.left - panel.Size.Width / 2f;
            OffsetRight = panel.Offset.left + panel.Size.Width / 2f;
        } else {
            OffsetTop = panel.Offset.top;
            OffsetBottom = panel.Offset.bottom;
            OffsetLeft = panel.Offset.left;
            OffsetRight = panel.Offset.right;
        }
        // Compute rect size from anchors and offsets
        float rectWidth = (OffsetRight - OffsetLeft);
        float rectHeight = (OffsetBottom - OffsetTop);
        
        // Determine grow direction: expand outward when rect is zero/negative
        if (rectWidth <= 0) GrowHorizontal = GrowDirection.End;
        else GrowHorizontal = GrowDirection.Both;
        
        if (rectHeight <= 0) GrowVertical = GrowDirection.End;
        else GrowVertical = GrowDirection.Both;
        
        SetCustomMinimumSize(new Vector2(panel.Size.Width, panel.Size.Height));
        ClipContents = true;

        // Debug: print incoming native panel values to help diagnose anchor/size mapping issues
        GD.Print(
            $"DEBUG Panel: id={panel.Id} anchor=({panel.Anchor.X},{panel.Anchor.Y}) pivot=({panel.Pivot.X},{panel.Pivot.Y}) offset=({panel.Offset.top},{panel.Offset.bottom},{panel.Offset.left},{panel.Offset.right}) size=({panel.Size.Width},{panel.Size.Height}) background={(panel.Background ?? "null")}");

        // Debug: print OnClick handler info
        if (panel.OnClick.HasValue) {
            GD.Print($"DEBUG Panel OnClick: id={panel.Id} actionName={panel.OnClick.Value.ActionName}");
        }
        else {
            GD.Print($"DEBUG Panel OnClick: id={panel.Id} NONE");
        }

        //if panel.Content is instance of ConstantTextContent, add a RichTextLabel
        if (panel.Content is ConstantTextContent constantTextContent) {
            AddChild(new ConstantTextContentNode(constantTextContent));
        }

        if (panel.Content is EntityTextValueContent entityTextValueContent) {
            AddChild(new EntityTextValueContentNode(entityTextValueContent));
        }

        if (panel.Content is ConstantNumberContent constantNumberContent) {
            AddChild(new ConstantNumberContentNode(constantNumberContent));
        }

        if (panel.Content is EntityNumberValueContent entityNumberValueContent) {
            AddChild(new EntityNumberValueContentNode(entityNumberValueContent));
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
            var hasLayout = panel.Layout.HasValue && panel.Layout.Value.Columns != null && panel.Layout.Value.Columns.Length > 0;
            if (hasLayout) {
                // Grid layout mode: children are placed in grid container with tracks
                var gridOrderContainer = new BoxContainer {
                    Name = "gridOrder",
                    Vertical = false,
                };
                gridOrderContainer.AddThemeConstantOverride("separation", 0);
                AddChild(gridOrderContainer);

                var numberOfTracks = panel.Layout.Value.Columns.Length;

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
                    var p = new Panel(child, isRoot: false) {
                        Name = child.Id,
                        UniqueNameInOwner = true
                    };
                    tracks.ElementAt(i % numberOfTracks).AddChild(p);
                    p.SetOwner(this);
                }
            } else {
          // Free-positioning mode: children are direct children positioned by their own anchor/offset
                  float cumulativeHeight = 0;
                  foreach (var origChild in panel.Children) {
                     var child = origChild;
                     if (child.Offset.top == 0 && child.Offset.left == 0 && child.Offset.bottom == 0 && child.Offset.right == 0) {
                         child.Offset.top = cumulativeHeight;
                         child.Offset.bottom = cumulativeHeight + child.Size.Height;
                         cumulativeHeight += child.Size.Height;
                     }
                     var p = new Panel(child, isRoot: false) {
                         Name = child.Id,
                         UniqueNameInOwner = true
                     };
                     AddChild(p);
                     p.SetOwner(this);
                 }
            }
        }
    }


    public override void _GuiInput(InputEvent @event) {
        if (@event is InputEventMouseButton mouseEvent) {
            if (mouseEvent.Pressed && mouseEvent.ButtonIndex == MouseButton.Left) {
                var actionName = panel.OnClick?.ActionName;
                if (actionName != null) {
                    GD.Print($"{panel.Id}: Emitting action: {actionName}");
                    RuntimeInterop.emitAction(actionName);
                }
                // Forward click to children
                foreach (var child in GetChildren()) {
                    if (child is Panel childPanel) {
                        childPanel._GuiInput(mouseEvent.Duplicate() as InputEventMouseButton);
                    }
                }
            }
        }

        if (@event is InputEventMouseMotion mouseMotion) {
            Vector2 localPos = mouseMotion.Position;
            AddChild(new Label() { Text = "" + localPos.ToString() + "" });
            GetViewport().SetInputAsHandled();
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