using GdUnit4.Examples.Basics.Setup.Sources.UI;
using Godot;
using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;
using NewGameProject.UI;
using Vector2 = Godot.Vector2;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_8.ContainerListViewReconcile;

[TestSuite]
public class TestClass : Steps
{
    [TestCategory("Step_8")]
    [TestCase]
    [RequireGodotRuntime]
    public async Task Given_container_list_view_it_should_reuse_existing_children_on_update()
    {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .AddFileToArchive("module/texture.png", "texture.png")
            .EnsureDllAccessible()
            .ProcessArchive();

        var scene = LoadTestScene();
        var rootNode = new RootNode();
        var idList = RuntimeInterop.GetPanelIds();

        scene.AddChild(rootNode);
        rootNode.SetSize(new Vector2 { X = 1000, Y = 1000 });
        rootNode.SetAnchorsPreset(Control.LayoutPreset.Center);
        await runner.SimulateFrames(1);

        var panel = rootNode.GetNode<Panel>(idList[0]);
        var containerListViewNode = panel.GetNode<ContainerListViewContentNode>("containerListView");
        var boxContainer = containerListViewNode.GetNode<BoxContainer>("boxContainer");

        // Capture the initial child panels
        var initialChildren = boxContainer.GetChildren();
        Assertions.AssertThat(initialChildren.Count)
            .OverrideFailureMessage($"Expected 2 child panels but found {initialChildren.Count}")
            .IsEqual(2);

        var firstChild = initialChildren[0];
        var secondChild = initialChildren[1];

        // Trigger UpdateContent again — should reuse existing children
        containerListViewNode.UpdateContent(new ContainerListViewContent("items-container", vertical: true));

        await runner.SimulateFrames(1);

        var updatedChildren = boxContainer.GetChildren();
        Assertions.AssertThat(updatedChildren.Count)
            .OverrideFailureMessage($"Expected 2 child panels after update but found {updatedChildren.Count}")
            .IsEqual(2);

        // Verify the same Panel instances are reused (not replaced)
        Assertions.AssertThat(ReferenceEquals(updatedChildren[0], firstChild))
            .OverrideFailureMessage("First child should be the same node instance")
            .IsTrue();

        Assertions.AssertThat(ReferenceEquals(updatedChildren[1], secondChild))
            .OverrideFailureMessage("Second child should be the same node instance")
            .IsTrue();
    }
}
