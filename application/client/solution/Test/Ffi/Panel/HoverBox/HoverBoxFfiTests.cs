using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;

namespace GdUnit4.Examples.Basics.Setup.Test.Ffi.Panel.HoverBox;

[TestSuite]
public class HoverBoxFfiTests : Steps {
    [TestCase]
    public void Given_panel_it_should_apply_hoverBox() {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .AddFileToArchive("module/texture.exr", "texture.exr")
            .AddFileToArchive("module/hover.exr", "hover.exr")
            .ProcessArchive();

        var panel = RuntimeInterop.GetPanelById("hover");
        Assertions.AssertThat(panel.HoverBox.HasValue).IsTrue();
        Assertions.AssertThat(panel.HoverBox.Value.Texture).IsEqual("hover.exr");
        Assertions.AssertThat(panel.HoverBox.Value.Thickness).IsEqual(5);
    }
}
