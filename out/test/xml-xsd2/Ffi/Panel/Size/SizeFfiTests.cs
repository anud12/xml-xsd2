using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;

namespace GdUnit4.Examples.Basics.Setup.Test.Ffi.Panel.Size;

[TestSuite]
public class SizeFfiTests : Steps {
    [TestCase]
    public void Given_panel_it_should_apply_size() {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .AddFileToArchive("module/texture.exr", "texture.exr")
            .ProcessArchive();

        var panel = RuntimeInterop.GetPanelById("center");
        Assertions.AssertThat(panel.Size.Height).IsEqual(100f);
        Assertions.AssertThat(panel.Size.Width).IsEqual(100f);
    }
}