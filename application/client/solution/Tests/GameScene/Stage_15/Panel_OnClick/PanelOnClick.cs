using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using GdUnit4.Examples.Basics.Setup.Sources.UI;
using Godot;
using NewGameProject.Tests.XUnit;
using Vector2 = Godot.Vector2;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_15.Panel_OnClick;

[TestSuite]
public class PanelOnClick : Steps {
    // Replay: a recorded replay file is the only input; driving it must make the
    // recorded actions happen (no live clicking involved). The recorded node is a
    // panel nested under a window, so re-driving it fires the panel's onClick.
    [TestCategory("Stage_15")]
    [TestCase(Timeout = 30_000)]
    [RequireGodotRuntime]
    public async Task it_should_replay_a_replay_file_and_fire_the_actions() {
        try {
            AddFileToArchive("module/index.js", "index.js")
                .AddFileToArchive("module/manifest.json", "manifest.json")
                .EnsureDllAccessible()
                .ProcessArchive();

            await AttachUiScene();
            await runner.SimulateFrames(1);

            string expected = Path.Combine(TestDir(), "click-panel.expected.replay.jsonl");
            var events = UiTraceability.ReadFile(expected);
            Assertions.AssertThat(events.Count).IsEqual(2);

            ClearOutput();
            int replayed = await UiTraceability.Replay(events, async d => { await runner.SimulateFrames((uint)d); });
            await runner.SimulateFrames(1);
            Assertions.AssertThat(replayed).IsEqual(2);
            AssertRuntimeOutputContains("___From module buy fired line___");

            DebugSaveScreenshot("result.png");
        }
        catch (Exception e) {
            Assertions.AssertThat(true)
                .OverrideFailureMessage($"Error: {e.Message}\n{e.StackTrace}")
                .IsFalse();
        }
    }

    // Recording: clicking the nested panel emits a .replay.jsonl line whose node
    // is the panel's own id (not the host window's), with an ordered timestamp and
    // the cursor position, and that validates against the schema.
    [TestCategory("Stage_15")]
    [TestCase(Timeout = 30_000)]
    [RequireGodotRuntime]
    public async Task it_should_validate_the_emitted_replay_file() {
        try {
            var expected = UiTraceability.ReadFile(Path.Combine(TestDir(), "click-panel.expected.replay.jsonl"));
            string actualPath = Path.Combine(TestDir(), "click-panel.actual.replay.jsonl");
            UiTraceability.BeginRecording(actualPath);

            AddFileToArchive("module/index.js", "index.js")
                .AddFileToArchive("module/manifest.json", "manifest.json")
                .EnsureDllAccessible()
                .ProcessArchive();

            var scene = await AttachUiScene();
            var panel = scene.Window("click-panel");

            for (int i = 1; i <= expected.Count; i++)
            {
                Click(panel.GlobalPosition + new Vector2(5, 5));
                await runner.SimulateFrames(1);
            }
            var actual = await WaitForReplayEvents(actualPath, minCount: expected.Count);

            // the emitted sequence matches the expected (all fields per event, same count);
            // the node is the nested panel, not the host window
            UiTraceability.AssertTraceMatches(actual, expected);

            // schema-valid: the emitted file exists and every line is a valid event
            Assertions.AssertThat(File.Exists(actualPath)).IsTrue();
            Assertions.AssertThat(UiTraceability.ValidateFile(actualPath)).IsEqual(expected.Count);

            DebugSaveScreenshot("result.png");
        }
        catch (Exception e) {
            Assertions.AssertThat(true)
                .OverrideFailureMessage($"Error: {e.Message}\n{e.StackTrace}")
                .IsFalse();
        }
        finally {
            UiTraceability.EndRecording();
        }
    }

    // The schema rejects malformed lines (guards the replay input).
    [TestCategory("Stage_15")]
    [TestCase]
    [RequireGodotRuntime]
    public void it_should_reject_replay_lines_that_violate_the_schema() {
        try {
            // A well-formed line validates (no exception).
            UiTraceability.ValidateEvent(
                "{\"type\":\"ui.click\",\"node\":\"click-panel\",\"uiFrame\":12,\"pointer\":{\"x\":100.5,\"y\":200}}");

            // Each of these violates the schema and must be rejected.
            string[] invalid = {
                // missing required 'pointer'
                "{\"type\":\"ui.click\",\"node\":\"n\",\"uiFrame\":0}",
                // 'uiFrame' negative (minimum 0)
                "{\"type\":\"ui.click\",\"node\":\"n\",\"uiFrame\":-1,\"pointer\":{\"x\":0,\"y\":0}}",
                // 'pointer.x' wrong type (not a number)
                "{\"type\":\"ui.click\",\"node\":\"n\",\"uiFrame\":0,\"pointer\":{\"x\":\"0\",\"y\":0}}",
                // 'pointer' missing required 'y'
                "{\"type\":\"ui.click\",\"node\":\"n\",\"uiFrame\":0,\"pointer\":{\"x\":0}}",
                // 'type' not 'ui.click'
                "{\"type\":\"ui.tap\",\"node\":\"n\",\"uiFrame\":0,\"pointer\":{\"x\":0,\"y\":0}}",
                // 'node' empty (minLength 1)
                "{\"type\":\"ui.click\",\"node\":\"\",\"uiFrame\":0,\"pointer\":{\"x\":0,\"y\":0}}",
                // unexpected extra property at root (additionalProperties false)
                "{\"type\":\"ui.click\",\"node\":\"n\",\"uiFrame\":0,\"pointer\":{\"x\":0,\"y\":0},\"x\":1}",
                // not valid JSON at all
                "{not json",
            };
            foreach (var line in invalid)
                AssertInvalid(line);
        }
        catch (Exception e) {
            Assertions.AssertThat(true)
                .OverrideFailureMessage($"Error: {e.Message}\n{e.StackTrace}")
                .IsFalse();
        }
    }

    static void AssertInvalid(string line) {
        bool threw = false;
        try {
            UiTraceability.ValidateEvent(line);
        }
        catch (InvalidOperationException) {
            threw = true;
        }
        Assertions.AssertThat(threw)
            .OverrideFailureMessage($"Expected schema rejection for line: {line}")
            .IsTrue();
    }

    static string TestDir([CallerFilePath] string callerPath = "") => Path.GetDirectoryName(callerPath)!;

    static async Task<System.Collections.Generic.List<UiTraceability.TraceEvent>> WaitForReplayEvents(
        string path, int minCount, int timeoutMs = 10_000) {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        int previousCount = -1;
        while (sw.ElapsedMilliseconds < timeoutMs) {
            var evts = UiTraceability.ReadFile(path);
            if (evts.Count >= minCount && evts.Count == previousCount) return evts;
            previousCount = evts.Count;
            await Task.Delay(75);
        }
        return UiTraceability.ReadFile(path);
    }

    void Click(Vector2 globalPos) {
        var press = new InputEventMouseButton {
            Position = globalPos,
            GlobalPosition = globalPos,
            ButtonIndex = MouseButton.Left,
            Pressed = true,
            ButtonMask = MouseButtonMask.Left
        };
        runner.Scene().GetViewport().PushInput(press);
        var release = (InputEventMouseButton)press.Duplicate();
        release.Pressed = false;
        runner.Scene().GetViewport().PushInput(release);
    }
}
