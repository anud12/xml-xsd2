using Godot;
using NewGameProject.Runtime;

namespace NewGameProject.UI;

/// <summary>
/// A Control node that displays entities from a container using a <see cref="ContainerListView"/>.
/// Wraps child panels in a BoxContainer. The <see cref="Vertical"/> flag determines whether
/// the BoxContainer lays out children vertically (default) or horizontally.
/// </summary>
public partial class ContainerListViewContentNode : Control
{
    private readonly ContainerListView _listView;
    private readonly BoxContainer _boxContainer;

    public bool Vertical { get; }

    public ContainerListViewContentNode(ContainerListView listView, bool vertical = true)
    {
        Name = "containerListView";
        _listView = listView;
        Vertical = vertical;

        SetAnchorsPreset(LayoutPreset.FullRect);

        _boxContainer = new BoxContainer
        {
            Name = "boxContainer",
            Vertical = vertical
        };
        _boxContainer.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(_boxContainer);
    }

    public void Refresh()
    {
        foreach (Node child in _boxContainer.GetChildren())
        {
            _boxContainer.RemoveChild(child);
            child.QueueFree();
        }

        var entityIds = _listView.GetEntityIds();
        for (int i = 0; i < entityIds.Length; i++)
        {
            var content = _listView.GetContentForEntity(i);

            if (content != null)
            {
                var childPanel = CreateChildPanel(entityIds[i], i, content);
                _boxContainer.AddChild(childPanel);
                childPanel.SetOwner(this);
            }
            else
            {
                var childPanel = _listView.CreateItemPanel(entityIds[i], i);
                _boxContainer.AddChild(childPanel);
                childPanel.SetOwner(this);
            }
        }
    }

    static Panel CreateChildPanel(string entityId, int index, Runtime.PanelContent content)
    {
        return new Panel(new Runtime.Panel
        {
            Id = $"item_{index}",
            Size = new Runtime.Size { Width = 80f, Height = 40f },
            Content = content
        });
    }

    public override void _Ready()
    {
        Refresh();
    }
}
