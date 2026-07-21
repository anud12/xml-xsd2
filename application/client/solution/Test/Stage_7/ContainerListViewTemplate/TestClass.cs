using System.Runtime.ExceptionServices;
using GdUnit4.Examples.Basics.Setup.Sources.UI;
using Godot;
using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;
using NewGameProject.UI;
using Vector2 = Godot.Vector2;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_7.ContainerListViewTemplate;

[TestSuite]
public class TestClass : Steps
{
    [TestCategory("Step_7")]
    [TestCase]
    [RequireGodotRuntime]
    public async Task Given_container_list_view_template_it_renders_child_panels_with_entity_number_value()
    {
        try
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
            Assertions.AssertThat(panel).IsNotNull();

            // Verify ContainerListViewContentNode exists
            var containerListViewNode = panel.GetNode<ContainerListViewContentNode>("containerListView");
            Assertions.AssertThat(containerListViewNode)
                .OverrideFailureMessage("containerListView node not found")
                .IsNotNull();

            // Verify BoxContainer exists with children
            var boxContainer = containerListViewNode.GetNode<BoxContainer>("boxContainer");
            Assertions.AssertThat(boxContainer)
                .OverrideFailureMessage("boxContainer node not found")
                .IsNotNull();

            var children = boxContainer.GetChildren();
            Assertions.AssertThat(children.Count)
                .OverrideFailureMessage($"Expected 2 child panels but found {children.Count}")
                .IsEqual(2);

            // Verify first child has content RichTextLabel
            var firstChild = children[0] as Panel;
            Assertions.AssertThat(firstChild)
                .OverrideFailureMessage("First child is not a Panel")
                .IsNotNull();

            var firstContent = firstChild.GetNode<RichTextLabel>("content");
            Assertions.AssertThat(firstContent)
                .OverrideFailureMessage("First child has no content RichTextLabel node")
                .IsNotNull();
            Assertions.AssertThat(firstContent.Text)
                .OverrideFailureMessage($"First child content text should be '10' but is '{firstContent.Text}'")
                .IsEqual("10");

            // Verify second child has content RichTextLabel
            var secondChild = children[1] as Panel;
            Assertions.AssertThat(secondChild)
                .OverrideFailureMessage("Second child is not a Panel")
                .IsNotNull();

            var secondContent = secondChild.GetNode<RichTextLabel>("content");
            Assertions.AssertThat(secondContent)
                .OverrideFailureMessage("Second child has no content RichTextLabel node")
                .IsNotNull();
            Assertions.AssertThat(secondContent.Text)
                .OverrideFailureMessage($"Second child content text should be '20' but is '{secondContent.Text}'")
                .IsEqual("20");
        }
        catch (Exception e)
        {
            Assertions.AssertThat(true)
                .OverrideFailureMessage($"Error: {e.Message}\n{e.StackTrace}")
                .IsFalse();
            runner.Scene()?.GetTree().Quit();
        }
    }
}
