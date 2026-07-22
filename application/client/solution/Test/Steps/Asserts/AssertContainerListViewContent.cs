using GdUnit4;
using Godot;
using NewGameProject.UI;

namespace NewGameProject.Tests.XUnit;

/// <summary>
/// Fluent assertion helper for <see cref="ContainerListViewContentNode"/> nodes.
/// </summary>
/// <remarks>
/// Used to verify the structure and content of container list views, typically invoked
/// via <see cref="Steps.AssertPanel.HasContainerListViewContent"/>. Supports method chaining.
/// </remarks>
/// <example>
/// AssertPanelThat(panel)
///     .HasContainerListViewContent(content => {
///         content.IsVertical();
///         content.HasLength(3);
///         content.HasTemplates(
///             p => p.HasContentText("1"),
///             p => p.HasContentText("2"),
///             p => p.HasContentText("3")
///         );
///     });
/// </example>
public class AssertContainerListViewContent(ContainerListViewContentNode content, string path = "/") {
    private ContainerListViewContentNode content = content;

    /// <summary>
    /// Asserts that the container list view is oriented vertically.
    /// </summary>
    /// <param name="path">Path used in failure messages to identify the node in the tree.</param>
    /// <returns>This instance for chaining.</returns>
    public AssertContainerListViewContent IsVertical() {
        Assertions.AssertBool(content.Vertical)
            .OverrideFailureMessage($"Container list at \"{path}\" is not vertical")
            .IsTrue();
        return this;
    }

    /// <summary>
    /// Asserts that the container list's <c>boxContainer</c> child has the expected number of child panels.
    /// </summary>
    /// <param name="i">Expected number of child panels in the list.</param>
    /// <returns>This instance for chaining.</returns>
    public AssertContainerListViewContent HasLength(int i) {
        var boxContainer = content.GetNode<BoxContainer>("boxContainer");
        Assertions.AssertInt(boxContainer.GetChildCount())
            .OverrideFailureMessage($"Container list at \"{path}\" has {boxContainer.GetChildCount()} children, expected {i}")
            .IsEqual(i);
        return this;
    }

    /// <summary>
    /// Asserts that each child panel in the container list matches the corresponding assertion action.
    /// </summary>
    /// <param name="actions">
    /// A series of callbacks, each receiving an <see cref="Steps.AssertPanel"/> instance
    /// for one child panel in order. The number of actions must match the number of child panels.
    /// </param>
    /// <returns>This instance for chaining.</returns>
    /// <remarks>
    /// Validates that the container has exactly <c>actions.Length</c> children, then invokes
    /// each action with an <see cref="Steps.AssertPanel"/> wrapper for the corresponding child.
    /// </remarks>
    /// <example>
    /// content.HasTemplates(
    ///     p => p.HasContentText("1"),
    ///     p => p.HasContentText("2"),
    ///     p => p.HasContentText("3")
    /// );
    /// </example>
    public AssertContainerListViewContent HasTemplates(params Action<Steps.AssertPanel>[] actions) {
        var boxContainer = content.GetNode<BoxContainer>("boxContainer");
        HasLength(boxContainer.GetChildCount());

        for (var i = 0; i < actions.Length; i++) {
            var child = boxContainer.GetChild<Panel>(i);
            actions[i].Invoke(new Steps.AssertPanel(child, $"{path}/{i}"));
        }
        return this;
    }
}