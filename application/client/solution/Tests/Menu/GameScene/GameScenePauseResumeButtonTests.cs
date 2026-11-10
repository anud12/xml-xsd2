using GdUnit4;
using Godot;
using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;

namespace GdUnit4.Examples.Basics.Setup.Test.Menu.GameScenePause;

[TestSuite]
public partial class TestClass : Steps
{
    [TestCase]
    [RequireGodotRuntime]
    public async Task Given_game_scene_it_should_show_a_pause_button_top_left()
    {
        await AttachUiScene();
        var scene = runner.Scene();
        var pauseButton = scene.GetNodeOrNull<Button>("uiLayers/coreUI/PauseResumeButton");
        Assertions.AssertThat(pauseButton).IsNotNull();
        if (pauseButton is null) return;

        Assertions.AssertThat(pauseButton.Text).IsEqual("Pause");
        Assertions.AssertThat(pauseButton.Position.X).IsEqual(310f);
        Assertions.AssertThat(pauseButton.Position.Y).IsEqual(8f);
        ClearSimulatedMouse();
    }

    [TestCase]
    [RequireGodotRuntime]
    public async Task Given_running_runtime_pausing_should_freeze_the_clock_and_resume_should_advance_it()
    {
        await AttachUiScene();
        var scene = runner.Scene();
        var pauseButton = scene.GetNodeOrNull<Button>("uiLayers/coreUI/PauseResumeButton");
        if (pauseButton is null)
        {
            Assertions.AssertThat(true).OverrideFailureMessage("PauseResumeButton not found in game scene").IsFalse();
            return;
        }

        // Start from a known running state so the test is independent of any
        // prior test's pause flag.
        RuntimeInterop.Resume();
        Assertions.AssertThat(RuntimeInterop.Paused).IsFalse();
        Assertions.AssertThat(pauseButton.Text).IsEqual("Pause");

        // While running, the clock advances.
        long before = RuntimeInterop.GetElapsedTimeUnits();
        RuntimeInterop.RunIteration(5);
        long advancedWhileRunning = RuntimeInterop.GetElapsedTimeUnits() - before;
        Assertions.AssertThat(advancedWhileRunning).IsEqual(5L);

        // Pause via the button: the label flips to "Resume" and the runtime
        // reports paused.
        pauseButton.EmitSignal("pressed");
        await runner.SimulateFrames(1);
        Assertions.AssertThat(RuntimeInterop.Paused).IsTrue();
        Assertions.AssertThat(pauseButton.Text).IsEqual("Resume");

        // While paused, iterations are a no-op: elapsed time is frozen.
        long frozen = RuntimeInterop.GetElapsedTimeUnits();
        RuntimeInterop.RunIteration(10);
        Assertions.AssertThat(RuntimeInterop.GetElapsedTimeUnits()).IsEqual(frozen);

        // Resume via the button: the label flips back to "Pause" and the clock
        // advances again.
        pauseButton.EmitSignal("pressed");
        await runner.SimulateFrames(1);
        Assertions.AssertThat(RuntimeInterop.Paused).IsFalse();
        Assertions.AssertThat(pauseButton.Text).IsEqual("Pause");

        long afterResume = RuntimeInterop.GetElapsedTimeUnits();
        RuntimeInterop.RunIteration(7);
        long advancedAfterResume = RuntimeInterop.GetElapsedTimeUnits() - afterResume;
        Assertions.AssertThat(advancedAfterResume).IsEqual(7L);

        // Leave the runtime running for subsequent tests.
        RuntimeInterop.Resume();
        ClearSimulatedMouse();
    }
}
