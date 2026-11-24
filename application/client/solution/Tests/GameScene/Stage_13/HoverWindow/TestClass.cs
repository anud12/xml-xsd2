using GdUnit4.Examples.Basics.Setup.Sources.UI;
using Godot;
using NewGameProject.Tests.XUnit;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_13.HoverWindow;

[TestSuite]
public partial class TestClass : Steps
{
    [TestCategory("Step_13")]
    [TestCase]
    [RequireGodotRuntime]
    public async Task Hover_window_is_reconciled_on_enter_and_exit()
    {
        try
        {
            AddFileToArchive("module/index.js", "index.js")
                .AddFileToArchive("module/manifest.json", "manifest.json")
                .AddFileToArchive("module/hover.png", "hover.png")
                .EnsureDllAccessible().ProcessArchive();
            SimulateMouse(new Vector2(0, 0));
            var scene = await AttachUiScene();
            var source = scene.Window("hover-source");
            var outline = source.GetNode<HoverOutline>("HoverOutline");
            Assertions.AssertThat(scene.Root.FindWindowById("hover-source:hover-window")).IsNull();
            await runner.SimulateFrames(5);
            AssertScreenshot("hover_before.png");
            HoverAt(source.GlobalPosition + new Vector2(5, 5));
            await runner.SimulateFrames(10);
            Assertions.AssertThat(outline.Visible).IsTrue();
            var derived = scene.Root.FindWindowById("hover-source:hover-window");
            Assertions.AssertThat(derived).IsNotNull();
            if (derived != null) await AssertPanelThat(derived).IsPositionEqual(300, 300).AssertRenderMatches("hover_visible.png");
            // A plain hover window must not acquire a pin affordance or respond to P.
            Assertions.AssertThat(derived?.GetNodeOrNull<Label>("__pinIndicator")).IsNull();
            Assertions.AssertThat(scene.Root.HandlePinKey(Key.P)).IsFalse();
            HoverAt(new Vector2(0, 0));
            await runner.SimulateFrames(10);
            AssertScreenshot("hover_after.png");
            Assertions.AssertThat(outline.Visible).IsFalse();
            Assertions.AssertThat(scene.Root.FindWindowById("hover-source:hover-window")).IsNull();
        }
        finally { ClearSimulatedMouse(); }
    }
}
