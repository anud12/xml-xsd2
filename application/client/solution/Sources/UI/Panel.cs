using GdUnit4.Examples.Basics.Setup.Sources.UI;
using Godot;
using NewGameProject.Runtime;
using NewGameProject.UI;
using Vector2 = Godot.Vector2;

public partial class Panel : Godot.Panel {
    NewGameProject.Runtime.Panel _panel;
    HoverOutline? _hoverOutline;
    BoxContainer? _gridOrder;
    List<BoxContainer>? _tracks;
    Control? _contentNode;


    public NewGameProject.Runtime.Panel ChildPanel {
        get {
            if (_panel.Children != null && _panel.Children.Length > 0) return _panel.Children[0];
            return _panel;
        }
    }

    public Panel(NewGameProject.Runtime.Panel panel) {
        Name = panel.Id;
        UniqueNameInOwner = true;
        Owner = GetParent();
        _panel = panel;
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

        if (panel.Hover.HasValue) {
            _hoverOutline = new HoverOutline(panel.Hover);
            _hoverOutline.Visible = false;
            MouseEntered += () => _hoverOutline.Visible = true;
            MouseExited += () => _hoverOutline.Visible = false;
            Resized += () => _hoverOutline.Resize();
            AddChild(_hoverOutline);
            _hoverOutline.Resize();
        }

        GD.Print(
            $"DEBUG Panel: id={panel.Id} anchor=({panel.Anchor.X},{panel.Anchor.Y}) pivot=({panel.Pivot.X},{panel.Pivot.Y}) offset=({panel.Offset.top},{panel.Offset.bottom},{panel.Offset.left},{panel.Offset.right}) size=({panel.Size.Width},{panel.Size.Height}) background={(panel.Background ?? "null")}");

        if (panel.OnClick.HasValue)
            GD.Print($"DEBUG Panel OnClick: id={panel.Id} actionName={panel.OnClick.Value.ActionName}");
        else
            GD.Print($"DEBUG Panel OnClick: id={panel.Id} NONE");

        Update(panel);
    }

    public void Update(NewGameProject.Runtime.Panel panel) {
        _panel = panel;
        UpdateContentNode(panel.Content);

        if (panel.Background != null) {
            var files = RuntimeInterop.GetFileFromArchive();
            if (files.TryGetValue(panel.Background, out var imageData)) {
                Image img = new Image();
                img.LoadPngFromBuffer(imageData);
                TextureFilter = TextureFilterEnum.Nearest;
                AddThemeStyleboxOverride("panel", new StyleBoxTexture {
                    Texture = ImageTexture.CreateFromImage(img),
                });
            }
        }

        UpdateChildren(panel);
    }

    void UpdateContentNode(PanelContent? content) {
        if (content == null) {
            if (_contentNode != null) {
                RemoveChild(_contentNode);
                _contentNode.QueueFree();
                _contentNode = null;
            }
            return;
        }

        if (_contentNode == null) {
            _contentNode = CreateContentNode(content);
            if (_contentNode != null) AddChild(_contentNode);
            return;
        }

        // Same type — update in place
        if (_contentNode.GetType() == ContentNodeForType(content)) {
            UpdateContentNodeInPlace(_contentNode, content);
            return;
        }

        // Different type — replace in the same position
        var index = _contentNode.GetIndex();
        var newNode = CreateContentNode(content);
        RemoveChild(_contentNode);
        _contentNode.QueueFree();
        _contentNode = newNode;
        if (_contentNode != null) {
            AddChild(_contentNode);
            MoveChild(_contentNode, index);
        }
    }

    static Type ContentNodeForType(PanelContent content) {
        return content switch {
            ConstantTextContent => typeof(ConstantTextContentNode),
            EntityTextValueContent => typeof(EntityTextValueContentNode),
            ConstantNumberContent => typeof(ConstantNumberContentNode),
            EntityNumberValueContent => typeof(EntityNumberValueContentNode),
            ContainerListViewContent => typeof(ContainerListViewContentNode),
            _ => typeof(Control)
        };
    }

    Control? CreateContentNode(PanelContent content) {
        return content switch {
            ConstantTextContent c => new ConstantTextContentNode(c),
            EntityTextValueContent e => new EntityTextValueContentNode(e),
            ConstantNumberContent n => new ConstantNumberContentNode(n),
            EntityNumberValueContent n => new EntityNumberValueContentNode(n),
            ContainerListViewContent c => new ContainerListViewContentNode(c),
            _ => null
        };
    }

    static void UpdateContentNodeInPlace(Control node, PanelContent content) {
        if (node is ConstantTextContentNode ctcn && content is ConstantTextContent ctc)
            ctcn.Text = ctc.Value;

        if (node is ConstantNumberContentNode cncn && content is ConstantNumberContent cnc)
            cncn.Text = cnc.Value.ToString();

        if (node is EntityTextValueContentNode etvcn && content is EntityTextValueContent etvc)
            etvcn.Text = RuntimeInterop.GetEntityTextMapValue(etvc.EntityId, etvc.Name);

        if (node is EntityNumberValueContentNode envcn && content is EntityNumberValueContent envc)
            envcn.Text = RuntimeInterop.GetEntityNumberMapValue(envc.EntityId, envc.Name);

        if (node is ContainerListViewContentNode clvcn && content is ContainerListViewContent clvc)
            clvcn.Refresh();
    }

    void UpdateChildren(NewGameProject.Runtime.Panel panel) {
        if (_gridOrder != null) {
            RemoveChild(_gridOrder);
            _gridOrder.QueueFree();
            _gridOrder = null;
        }
        _tracks = null;

        if (panel.Children == null)
            return;

        _gridOrder = new BoxContainer {
            Name = "gridOrder",
            Vertical = false,
        };
        _gridOrder.AddThemeConstantOverride("separation", 0);
        AddChild(_gridOrder);

        var numberOfTracks = panel.Layout?.Columns?.Length ?? 1;
        _tracks = new List<BoxContainer>();

        foreach (var i in Enumerable.Range(0, numberOfTracks)) {
            var track = new BoxContainer {
                Name = "track_" + i,
                Vertical = true,
            };
            track.AddThemeConstantOverride("separation", 0);
            _tracks.Add(track);
            _gridOrder.AddChild(track);
        }

        for (int i = 0; i < panel.Children.Length; i++) {
            var child = panel.Children[i];
            var p = new Panel(child) {
                Name = child.Id,
                UniqueNameInOwner = true
            };
            _tracks[i % numberOfTracks].AddChild(p);
            p.SetOwner(this);
        }
    }

    public override void _GuiInput(InputEvent @event) {
        if (@event is InputEventMouseButton mouseEvent) {
            if (mouseEvent.Pressed && mouseEvent.ButtonIndex == MouseButton.Left) {
                var actionName = _panel.OnClick?.ActionName;
                if (actionName != null) {
                    GD.Print($"{_panel.Id}: Emitting action: {actionName}");
                    RuntimeInterop.emitAction(actionName);
                }
                GetViewport().SetInputAsHandled();
            }
        }
    }

    public override void _Ready() { }

    public override void _EnterTree() { }

    public override void _Process(double delta) { }
}
