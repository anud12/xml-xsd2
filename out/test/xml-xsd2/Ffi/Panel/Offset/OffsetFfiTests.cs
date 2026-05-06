using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;

namespace GdUnit4.Examples.Basics.Setup.Test.Ffi.Panel.Offset;

[TestSuite]
public class OffsetFfiTests : Steps {
    [TestCase]
    public void Given_panel_it_should_apply_offset_in_a_diamond_shape() {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .AddFileToArchive("module/texture.exr", "texture.exr")
            .ProcessArchive();

        var top = RuntimeInterop.GetPanelById("top");
        Assertions.AssertThat(top.Offset.top).IsEqual(-100f);
        Assertions.AssertThat(top.Offset.bottom).IsEqual(-100f);
        Assertions.AssertThat(top.Offset.left).IsEqual(0f);
        Assertions.AssertThat(top.Offset.right).IsEqual(0f);

        var left = RuntimeInterop.GetPanelById("left");
        Assertions.AssertThat(left.Offset.top).IsEqual(0f);
        Assertions.AssertThat(left.Offset.bottom).IsEqual(0f);
        Assertions.AssertThat(left.Offset.left).IsEqual(-100f);
        Assertions.AssertThat(left.Offset.right).IsEqual(-100f);

        var bottom = RuntimeInterop.GetPanelById("bottom");
        Assertions.AssertThat(bottom.Offset.top).IsEqual(100f);
        Assertions.AssertThat(bottom.Offset.bottom).IsEqual(100f);
        Assertions.AssertThat(bottom.Offset.left).IsEqual(0f);
        Assertions.AssertThat(bottom.Offset.right).IsEqual(0f);

        var right = RuntimeInterop.GetPanelById("right");
        Assertions.AssertThat(right.Offset.top).IsEqual(0f);
        Assertions.AssertThat(right.Offset.bottom).IsEqual(0f);
        Assertions.AssertThat(right.Offset.left).IsEqual(100f);
        Assertions.AssertThat(right.Offset.right).IsEqual(100f);
    }
}