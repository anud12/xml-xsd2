using Godot;
using NewGameProject.Runtime;

namespace NewGameProject.UI;

/// <summary>
/// Renders a list of panels from a container's entities using a template lambda.
/// The container for all children is a BoxContainer whose orientation is controlled
/// by the <see cref="Vertical"/> flag.
/// </summary>
public partial class ContainerListViewContentNode : Control
{
    private readonly Runtime.ContainerListViewContent _content;
    private readonly BoxContainer _boxContainer;

    public bool Vertical { get; }

    public ContainerListViewContentNode(Runtime.ContainerListViewContent content, bool vertical = true)
    {
        Name = "containerListView";
        _content = content;
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

        var entityIds = GetEntityIds();
        for (int i = 0; i < entityIds.Length; i++)
        {
            var entityId = entityIds[i];
            var panelContent = GetContentForEntity(i);

            if (panelContent != null)
            {
                var childPanel = CreateChildPanel(entityId, i, panelContent);
                _boxContainer.AddChild(childPanel);
                childPanel.SetOwner(this);
            }
            else
            {
                var childPanel = CreateItemPanel(entityId, i);
                _boxContainer.AddChild(childPanel);
                childPanel.SetOwner(this);
            }
        }
    }

    string[] GetEntityIds()
    {
        var container = ContainerInterop.GetContainerById(_content.ContainerId);
        return container.Entities;
    }

    Runtime.PanelContent? GetContentForEntity(int index)
    {
        if (_content.TemplateResults != null && index >= 0 && index < _content.TemplateResults.Length)
            return _content.TemplateResults[index];
        return null;
    }

    Panel CreateItemPanel(string entityId, int index)
    {
        if (_content.Template != null)
            return (Panel)_content.Template(entityId, index);

        return new Panel(new Runtime.Panel
        {
            Id = $"item_{index}",
            Size = new Runtime.Size { Width = 80f, Height = 40f },
            Content = new Runtime.ConstantTextContent(entityId, "center")
        });
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
