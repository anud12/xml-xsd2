using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;

namespace GdUnit4.Examples.Basics.Setup.Test.Ffi.Panel.Content;

[TestSuite]
public class TestClass : Steps {
    [TestCase]
    public void Given_panel_it_should_have_ConstantTextContent() {
        // Test fixed struct marshaling first
        RuntimeInterop.TestFixedStructMarshaling();
        
        // Test ConstantTextContent
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .AddFileToArchive("module/texture.exr", "texture.exr")
            .EnsureDllAccessible()
            .ProcessArchive();

        var constantPanel = RuntimeInterop.GetPanelById("panel");
        Assertions.AssertThat(constantPanel.Content).IsInstanceOf<ConstantTextContent>();
        Assertions.AssertThat(constantPanel.Content is EntityStringValueContent).IsEqual(false);
        
        var constantContent = constantPanel.Content as ConstantTextContent;
        Assertions.AssertThat(constantContent).IsNotNull();
        Assertions.AssertThat(constantContent.Value).IsEqual("Content");
        Assertions.AssertThat(constantContent.Align).IsEqual("center");


    }
}