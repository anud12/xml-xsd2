using System.Runtime.ExceptionServices;
using CommandLine;
using GdUnit4.Examples.Basics.Setup.Sources.UI;
using Godot;
using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;
using Vector2 = Godot.Vector2;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_2.Content.EntityNumberValueContent;

[TestSuite]
public class TestClass : Steps {
    [TestCategory("Step_2")]
    [TestCase(Timeout = 1_000)]
    [RequireGodotRuntime]
    [GodotExceptionMonitor]
    public async Task Given_panel_it_should_have_content_from_entity_number_value() {
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .AddFileToArchive("module/texture.png", "texture.png")
            .EnsureDllAccessible()
            .ProcessArchive();

        var scene = await AttachUiScene();

        scene.AssertPanelThat("number-panel")
            .IsNonNull()
            .HasContentText("42");
        DebugSaveScreenshot("expected.png");
    }
}
