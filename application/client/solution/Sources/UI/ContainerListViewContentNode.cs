using Godot;
using NewGameProject.Runtime;

namespace NewGameProject.UI;

/// <summary>
/// Renders a list of panels from a container's entities using a template lambda.
/// The container for all children is a BoxContainer whose orientation is controlled
/// by the <see cref="Vertical"/> flag.
/// </summary>
public partial class ContainerListViewContentNode : Control, IContentNode
{
    private readonly Runtime.ContainerListViewContent _content;
    private readonly BoxContainer _boxContainer;

    public bool Vertical { get; }

    public ContainerListViewContentNode(Runtime.ContainerListViewContent content)
    {
        Name = "containerListView";
        _content = content;
        Vertical = content.Vertical;

        SetAnchorsPreset(LayoutPreset.FullRect);

        _boxContainer = new BoxContainer
        {
            Name = "boxContainer",
            Vertical = Vertical
        };
        _boxContainer.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(_boxContainer);
        MouseFilter = MouseFilterEnum.Pass;
    }

    public void UpdateContent(PanelContent content)
    {
        var entityIds = GetEntityIds();
        var existingChildren = _boxContainer.GetChildren();

        // Update existing children in place
        int existingCount = existingChildren.Count;
        for (int i = 0; i < Math.Min(existingCount, entityIds.Length); i++)
        {
            var childPanel = (Panel)existingChildren[i];
            var panelContent = GetContentForEntity(i);

            if (panelContent != null)
            {
                childPanel.Update(new Runtime.Panel
                {
                    Id = $"item_{i}",
                    Size = new Runtime.Size { Width = 80f, Height = 40f },
                    Content = panelContent
                });
            }
            else
            {
                childPanel.Update(new Runtime.Panel
                {
                    Id = $"item_{i}",
                    Size = new Runtime.Size { Width = 80f, Height = 40f },
                    Content = new Runtime.ConstantTextContent(entityIds[i], "center")
                });
            }
        }

        // Add new children for new entities
        for (int i = existingCount; i < entityIds.Length; i++)
        {
            var entityId = entityIds[i];
            var panelContent = GetContentForEntity(i);

            Panel childPanel;
            if (panelContent != null)
            {
                childPanel = CreateChildPanel(entityId, i, panelContent);
            }
            else
            {
                childPanel = CreateItemPanel(entityId, i);
            }

            _boxContainer.AddChild(childPanel);
            childPanel.SetOwner(this);
        }

        // Remove excess children
        for (int i = entityIds.Length; i < existingCount; i++)
        {
            _boxContainer.RemoveChild(existingChildren[i]);
            existingChildren[i].QueueFree();
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
        UpdateContent(_content);
    }
}
