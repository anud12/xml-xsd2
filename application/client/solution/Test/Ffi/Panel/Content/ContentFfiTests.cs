using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;

namespace GdUnit4.Examples.Basics.Setup.Test.Ffi.Panel.Content;

[TestSuite]
public class TestClass : Steps {
    [TestCase]
    public void Given_panel_it_should_have_ConstantTextContent() {
        // Test ConstantTextContent
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .AddFileToArchive("module/texture.exr", "texture.exr")
            .EnsureDllAccessible()
            .ProcessArchive();

        var constantPanel = RuntimeInterop.GetPanelById("panel");
        Assertions.AssertThat(constantPanel.Content).IsInstanceOf<ConstantTextContent>();
        Assertions.AssertThat(constantPanel.Content is ConstantTextContent).IsEqual(true);
        Assertions.AssertThat(constantPanel.Content is EntityStringValueContent).IsEqual(false);
        
        var constantContent = constantPanel.Content as ConstantTextContent;
        Assertions.AssertThat(constantContent).IsNotNull();
        Assertions.AssertThat(constantContent.Value).IsEqual("Content");
        Assertions.AssertThat(constantContent.Align).IsEqual("center");


    }

    [TestCase]
    public void Given_entity_panel_it_should_have_entityStringValueContent() {
        // Test EntityStringValueContent
        CleanupArchive();
        AddFileToArchive("module_entity/index.js", "index.js")
            .AddFileToArchive("module_entity/manifest.json", "manifest.json")
            .AddFileToArchive("module_entity/texture.exr", "texture.exr")
            .EnsureDllAccessible()
            .ProcessArchive();

        var entityPanel = RuntimeInterop.GetPanelById("panel_entity");
        Assertions.AssertThat(entityPanel.Content).IsInstanceOf<EntityStringValueContent>();
        Assertions.AssertThat(entityPanel.Content is EntityStringValueContent).IsEqual(true);
        Assertions.AssertThat(entityPanel.Content is ConstantTextContent).IsEqual(false);
        
        var entityContent = entityPanel.Content as EntityStringValueContent;
        Assertions.AssertThat(entityContent).IsNotNull();
        Assertions.AssertThat(entityContent.Name).IsEqual("playerName");
        Assertions.AssertThat(entityContent.Align).IsEqual("center");
    }
}