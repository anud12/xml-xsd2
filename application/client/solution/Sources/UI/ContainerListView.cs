using Godot;
using NewGameProject.Runtime;

namespace NewGameProject.UI;

/// <summary>
/// Renders a list of panels from a container's entities using a template lambda.
/// The container for all children is a BoxContainer whose orientation is controlled
/// by the <see cref="ContainerListViewContentNode.Vertical"/> flag.
/// </summary>
public class ContainerListView
{
    readonly ContainerListViewContent _content;

    public ContainerListView(ContainerListViewContent content)
    {
        _content = content;
    }

    public string[] GetEntityIds()
    {
        var container = ContainerInterop.GetContainerById(_content.ContainerId);
        return container.Entities;
    }

    public Runtime.PanelContent? GetContentForEntity(int index)
    {
        if (_content.TemplateResults != null && index >= 0 && index < _content.TemplateResults.Length)
            return _content.TemplateResults[index];
        return null;
    }

    public Godot.Panel CreateItemPanel(string entityId, int index)
    {
        return _content.Template?.Invoke(entityId, index) ?? DefaultItemPanel(entityId, index);
    }

    static Godot.Panel DefaultItemPanel(string entityId, int index)
    {
        return new Panel(new Runtime.Panel
        {
            Id = $"item_{index}",
            Size = new Runtime.Size { Width = 80f, Height = 40f },
            Content = new Runtime.ConstantTextContent(entityId, "center")
        });
    }
}
