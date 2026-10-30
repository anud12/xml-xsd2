using GdUnit4;
using NewGameProject.Tests.XUnit;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_7.EntityListLayout;

[TestSuite]
public partial class ContainerEntityListLayoutFfiTests : Steps {

    [TestCategory("Stage_7")]
    [TestCase]
    [RequireGodotRuntime]
    public async Task Given_entity_list_vertical_false_when_painted_the_flow_container_is_a_row() {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .ProcessArchive();

        var scene = await AttachUiScene();

        // vertical: false must drive a horizontal (left-to-right) box.
        scene.AssertPanelThat("row-list")
            .IsHorizontal()
            .HasLength(3);

        // The default (no vertical) must remain a vertical (top-to-bottom) box.
        scene.AssertPanelThat("col-list")
            .IsVertical()
            .HasLength(3);
    }
}
