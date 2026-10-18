using GdUnit4.Examples.Basics.Setup.Sources.UI;
using Godot;
using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;
using Vector2 = Godot.Vector2;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_2.Animation.GetAnimation;

[TestSuite]
public partial class TestClass : Steps {
    [TestCategory("Step_2")]
    [TestCase]
    [RequireGodotRuntime]
    public async Task Given_get_animation_reference_must_drive_panel_background() {
        try {
            AddFileToArchive("module/index.js", "index.js")
                .AddFileToArchive("module/manifest.json", "manifest.json")
                .AddFileToArchive("module/frame_1.png", "frame_1.png")
                .AddFileToArchive("module/frame_2.png", "frame_2.png")
                .AddFileToArchive("module/frame_3.png", "frame_3.png")
                .AddFileToArchive("module/frame_4.png", "frame_4.png")
                .AddFileToArchive("module/frame_5.png", "frame_5.png")
                .EnsureDllAccessible()
                .ProcessArchive();

            var scene = await AttachUiScene();

            scene.AssertPanelThat("seqPanel").IsNonNull();

            RuntimeInterop.RunIteration(1);
            await runner.SimulateFrames(1);
            scene.AssertPanelThat("seqPanel").BackgroundMatches("module/frame_1.png");

            RuntimeInterop.RunIteration(1);
            await runner.SimulateFrames(1);
            scene.AssertPanelThat("seqPanel").BackgroundMatches("module/frame_2.png");

            RuntimeInterop.RunIteration(1);
            await runner.SimulateFrames(1);
            scene.AssertPanelThat("seqPanel").BackgroundMatches("module/frame_3.png");

            RuntimeInterop.RunIteration(1);
            await runner.SimulateFrames(1);
            scene.AssertPanelThat("seqPanel").BackgroundMatches("module/frame_4.png");

            RuntimeInterop.RunIteration(1);
            await runner.SimulateFrames(1);
            scene.AssertPanelThat("seqPanel").BackgroundMatches("module/frame_5.png");
        }
        catch (Exception e) {
            Assertions.AssertThat(true)
                .OverrideFailureMessage($"Error: {e.Message}\n{e.StackTrace}")
                .IsFalse();
        }
    }
}
