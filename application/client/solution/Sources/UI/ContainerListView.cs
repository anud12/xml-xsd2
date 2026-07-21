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
    public string ContainerId { get; }
    public Runtime.PanelTemplateDelegate Template { get; }
    public Runtime.PanelContent[]? TemplateResults { get; }

    public ContainerListView(string containerId, Runtime.PanelTemplateDelegate template)
    {
        ContainerId = containerId;
        Template = template;
        TemplateResults = null;
    }

    public ContainerListView(string containerId, Runtime.PanelTemplateDelegate template, Runtime.PanelContent[]? templateResults)
    {
        ContainerId = containerId;
        Template = template;
        TemplateResults = templateResults;
    }

    public string[] GetEntityIds()
    {
        var container = ContainerInterop.GetContainerById(ContainerId);
        return container.Entities;
    }

    public Runtime.PanelContent? GetContentForEntity(int index)
    {
        if (TemplateResults != null && index >= 0 && index < TemplateResults.Length)
            return TemplateResults[index];
        return null;
    }
}
