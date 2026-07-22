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
        Name = "content";
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

        UpdateExistingChildren(existingChildren, entityIds);
        AddNewChildren(existingChildren.Count, entityIds);
        RemoveExcessChildren(existingChildren, entityIds.Length);
    }

    void UpdateExistingChildren(Godot.Collections.Array<Node> existingChildren, string[] entityIds)
    {
        for (int i = 0; i < Math.Min(existingChildren.Count, entityIds.Length); i++)
        {
            var childPanel = (Panel)existingChildren[i];
            UpdateChildPanel(childPanel, entityIds[i], i);
        }
    }

    void UpdateChildPanel(Panel childPanel, string entityId, int index)
    {
        var templatePanel = GetTemplatePanelForEntity(index);
        if (templatePanel != null)
        {
            childPanel.Update(templatePanel.Value);
            return;
        }

        childPanel.Update(new Runtime.Panel
        {
            Id = $"item_{index}",
            Size = new Runtime.Size { Width = 80f, Height = 40f },
            Content = new Runtime.ConstantTextContent(entityId, "center")
        });
    }

    void AddNewChildren(int existingCount, string[] entityIds)
    {
        for (int i = existingCount; i < entityIds.Length; i++)
        {
            var childPanel = CreatePanelForEntity(entityIds[i], i);
            _boxContainer.AddChild(childPanel);
            childPanel.SetOwner(this);
        }
    }

    void RemoveExcessChildren(Godot.Collections.Array<Node> existingChildren, int desiredCount)
    {
        for (int i = desiredCount; i < existingChildren.Count; i++)
        {
            _boxContainer.RemoveChild(existingChildren[i]);
            existingChildren[i].QueueFree();
        }
    }

    Panel CreatePanelForEntity(string entityId, int index)
    {
        var templatePanel = GetTemplatePanelForEntity(index);
        if (templatePanel != null)
            return new Panel(templatePanel.Value) { Name = templatePanel.Value.Id, UniqueNameInOwner = true };
        return CreateItemPanel(entityId, index);
    }

    string[] GetEntityIds()
    {
        var container = ContainerInterop.GetContainerById(_content.ContainerId);
        return container.Entities;
    }

    Runtime.Panel? GetTemplatePanelForEntity(int index)
    {
        if (_content.TemplateResults != null && index >= 0 && index < _content.TemplateResults.Length)
            return _content.TemplateResults[index];
        return null;
    }

    Panel CreateItemPanel(string entityId, int index)
    {
        return new Panel(new Runtime.Panel
        {
            Id = $"item_{index}",
            Size = new Runtime.Size { Width = 80f, Height = 40f },
            Content = new Runtime.ConstantTextContent(entityId, "center")
        });
    }

    public override void _Ready()
    {
        UpdateContent(_content);
    }
}
