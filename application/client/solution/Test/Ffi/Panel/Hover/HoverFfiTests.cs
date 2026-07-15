using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;

namespace GdUnit4.Examples.Basics.Setup.Test.Ffi.Panel.Hover;

[TestSuite]
public class HoverFfiTests : Steps {
    [TestCase]
    public void Given_panel_it_should_apply_hover() {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .AddFileToArchive("module/texture.png", "texture.png")
            .AddFileToArchive("module/hover.png", "hover.png")
            .ProcessArchive();

        var panel = RuntimeInterop.GetPanelById("hover");
        Assertions.AssertThat(panel.Hover.HasValue).IsTrue();
        Assertions.AssertThat(panel.Hover.Value.Texture).IsEqual("hover.png");
        Assertions.AssertThat(panel.Hover.Value.Thickness).IsEqual(5);
    }

    [TestCase]
    public void Given_panel_without_hover_it_should_be_none() {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .AddFileToArchive("module/texture.png", "texture.png")
            .ProcessArchive();

        var panel = RuntimeInterop.GetPanelById("no-hover");
        Assertions.AssertThat(panel.Hover.HasValue).IsFalse();
    }
}
