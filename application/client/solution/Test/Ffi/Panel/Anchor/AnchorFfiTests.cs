using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;

namespace GdUnit4.Examples.Basics.Setup.Test.Ffi.Panel.Anchor;

[TestSuite]
public class AnchorFfiTests : Steps
{
    [AfterTest]
    public void TearDown() => CleanupArchive();

    [TestCase]
    public void Given_panel_it_should_apply_anchors()
    {
        AddFileToArchive("module/index.js",      "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .AddFileToArchive("module/texture.exr",   "texture.exr")
            .ProcessArchive();

        var panel = RuntimeInterop.GetPanelById("center");
        Assertions.AssertThat(panel.Anchor.X).IsEqual(0.5f);
        Assertions.AssertThat(panel.Anchor.Y).IsEqual(0.5f);
    }
}
