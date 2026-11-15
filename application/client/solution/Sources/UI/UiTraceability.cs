using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using GdUnit4;
using NJsonSchema;

namespace GdUnit4.Examples.Basics.Setup.Sources.UI;

/// <summary>
/// Records user UI interactions (clicks) for traceability and replay. Clicks are
/// captured on the main thread (a non-blocking enqueue); the "Logging" thread
/// drains the queue and appends each event to a <c>.replay</c> file (JSON lines)
/// that can later be <see cref="Replay"/>-ed to reproduce the interaction.
/// </summary>
/// <remarks>
/// The replay file is one JSON object per line, e.g.
/// <c>{"type":"ui.click","node":"buy","frame":0,"pointer":{"x":100,"y":200}}</c>.
/// Each line must validate against the embedded <c>UiReplayEvent.schema.json</c>
/// (Draft-07); <see cref="ValidateEvent"/> / <see cref="ValidateFile"/> enforce it,
/// and <see cref="ReadFile"/> validates every line it reads. <see cref="Replay"/>
/// re-drives every recorded click (pumping the UI to each recorded frame) so the
/// file is a faithful, self-contained record of what the user did and when.
/// </remarks>
public static class UiTraceability
{
    /// One recorded interaction. <see cref="UiFrame"/> is the UI frame (relative
    /// to the first event in the session; first event = 0);
    /// <see cref="PointerX"/>/<see cref="PointerY"/> is the cursor position when
    /// it fired.
    public readonly record struct TraceEvent(
        string Type, string Node, long UiFrame,
        double PointerX, double PointerY);

    static readonly ConcurrentQueue<TraceEvent> _events = new();
    static long _originFrame = -1;

    static readonly object _fileLock = new();
    static StreamWriter? _writer;
    static string? _openPath;

    // The compiled JSON schema for one event line (embedded resource, loaded once).
    static JsonSchema? _schema;
    static readonly object _schemaLock = new();
    const string SchemaResourceName = "UiReplayEvent.schema.json";

    /// Replay file path the Logging thread appends to. Null = recording disabled
    /// (events are drained and discarded so the queue cannot grow unbounded).
    /// Set from the main thread before interactions are captured. Setting it
    /// resets the frame origin so the first recorded event is frame 0.
    public static string? ReplayPath
    {
        get => _replayPath;
        set { _replayPath = value; _originFrame = -1; }
    }
    static string? _replayPath;

    /// Number of events the Logging thread has flushed to the replay file.
    public static long FlushedCount;

    /// Number of events captured but not yet written to the replay file.
    public static int PendingCount => _events.Count;

    /// Records a user click (main thread, non-blocking). The frame is the UI
    /// frame at the moment of the click, relative to the first event in the
    /// session (first event = frame 0).
    public static void RecordClick(string nodeId, Godot.Vector2 pointer)
    {
        long frame = RootNode.CurrentFrame;
        if (_originFrame < 0) _originFrame = frame;
        _events.Enqueue(new TraceEvent(
            "ui.click", nodeId, frame - _originFrame,
            Math.Round((double)pointer.X, 2), Math.Round((double)pointer.Y, 2)));
    }

    /// Drains pending events to the replay file and flushes. Runs on the "Logging"
    /// thread; returns the number of events written (0 when disabled or empty).
    public static int DrainAndWrite()
    {
        string? path = ReplayPath;
        if (path == null)
        {
            while (_events.TryDequeue(out _)) { }
            return 0;
        }
        EnsureOpen(path);
        var sb = new StringBuilder(2048);
        int n = 0;
        while (_events.TryDequeue(out var ev))
        {
            sb.Append(ToJson(ev)).Append('\n');
            n++;
        }
        if (n == 0) return 0;
        lock (_fileLock)
        {
            _writer!.Write(sb.ToString());
            _writer.Flush();
        }
        Interlocked.Add(ref FlushedCount, n);
        return n;
    }

    /// Flushes any remaining events and closes the replay file. Idempotent; called
    /// when the Logging thread stops (or by a test that wants a settled file).
    public static void Stop()
    {
        lock (_fileLock)
        {
            if (_writer == null) return;
            var sb = new StringBuilder();
            while (_events.TryDequeue(out var ev))
            {
                sb.Append(ToJson(ev)).Append('\n');
            }
            if (sb.Length > 0) _writer.Write(sb.ToString());
            _writer.Flush();
            _writer.Dispose();
            _writer = null;
            _openPath = null;
        }
    }

    /// Parses a <c>.replay</c> file back into events, validating every line
    /// against the JSON schema (see <see cref="ValidateEvent"/>). Throws on the
    /// first invalid line. Empty list when the file is missing. Opens with
    /// <see cref="FileShare.ReadWrite"/> so it can read while the Logging thread
    /// still holds the file open to append.
    public static List<TraceEvent> ReadFile(string path)
    {
        var result = new List<TraceEvent>();
        if (!File.Exists(path)) return result;
        foreach (var line in ReadLines(path))
        {
            ValidateEvent(line);
            using var doc = JsonDocument.Parse(line);
            var root = doc.RootElement;
            var pointer = root.GetProperty("pointer");
            result.Add(new TraceEvent(
                root.GetProperty("type").GetString() ?? "",
                root.GetProperty("node").GetString() ?? "",
                root.GetProperty("uiFrame").GetInt64(),
                pointer.GetProperty("x").GetDouble(),
                pointer.GetProperty("y").GetDouble()));
        }
        return result;
    }

    /// Validates every line of a <c>.replay</c> file against the JSON schema.
    /// Returns the number of events checked; throws on the first invalid line.
    public static int ValidateFile(string path)
    {
        int n = 0;
        if (File.Exists(path))
            foreach (var line in ReadLines(path))
            {
                ValidateEvent(line);
                n++;
            }
        return n;
    }

    /// Validates one event line against the schema. Throws
    /// <see cref="InvalidOperationException"/> when the line is not valid JSON
    /// or does not match the schema (the message includes the line).
    public static void ValidateEvent(string line)
    {
        System.Collections.Generic.ICollection<NJsonSchema.Validation.ValidationError> errors;
        try
        {
            errors = Schema().Validate(line);
        }
        catch (Exception e)
        {
            throw new InvalidOperationException($"Replay line is not a valid event: {line}", e);
        }
        if (errors.Count > 0)
            throw new InvalidOperationException(
                "Replay event failed schema validation ("
                + string.Join("; ", errors.Select(x => x.ToString())) + $"): {line}");
    }

    /// Loads (and compiles, once) the embedded replay-event JSON schema.
    static JsonSchema Schema()
    {
        if (_schema != null) return _schema;
        lock (_schemaLock)
        {
            if (_schema != null) return _schema;
            var stream = typeof(UiTraceability).Assembly.GetManifestResourceStream(SchemaResourceName)
                ?? throw new InvalidOperationException(
                    $"Embedded resource '{SchemaResourceName}' not found.");
            using (stream)
            using (var reader = new StreamReader(stream))
            {
                _schema = JsonSchema.FromJsonAsync(reader.ReadToEnd()).GetAwaiter().GetResult();
            }
            return _schema;
        }
    }

    /// The non-empty lines of a <c>.replay</c> file (empty array when none).
    static string[] ReadLines(string path)
    {
        lock (_fileLock)
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var sr = new StreamReader(fs, new UTF8Encoding(false));
            var lines = new List<string>();
            foreach (var raw in sr.ReadToEnd().Split('\n'))
            {
                var line = raw.Trim();
                if (line.Length > 0) lines.Add(line);
            }
            return lines.ToArray();
        }
    }

    /// Re-drives every recorded interaction (replay). Between clicks the UI is
    /// pumped forward to the next recorded frame via <paramref name="pumpFrames"/>
    /// (e.g. <c>runner.SimulateFrames</c>), so the replay is frame-accurate.
    /// Returns the number of clicks re-driven.
    public static async Task<int> Replay(
        IEnumerable<TraceEvent> events,
        Func<int, Task> pumpFrames)
    {
        int n = 0;
        long prev = 0;
        foreach (var ev in events)
        {
            if (ev.Type != "ui.click") continue;
            long delta = ev.UiFrame - prev;
            if (delta > 0) await pumpFrames((int)delta);
            prev = ev.UiFrame;
            NewGameProject.Runtime.RuntimeInterop.UiJsClick(ev.Node, 0, 0);
            n++;
        }
        return n;
    }

    static void EnsureOpen(string path)
    {
        lock (_fileLock)
        {
            if (_writer != null && _openPath == path) return;
            if (_writer != null) { _writer.Dispose(); _writer = null; }
            // FileShare.ReadWrite lets the test (or any reader) open the file
            // for reading while the Logging thread holds it open to append
            // (a reader's default FileShare.Read would otherwise reject the
            // writer's write access and the read would fail with a sharing
            // violation).
            _writer = new StreamWriter(
                new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite),
                new UTF8Encoding(false), 65536);
            _openPath = path;
        }
    }

    /// Asserts that the actual trace matches the expected trace (all fields per
    /// event, same count). Pointer is compared within 1px tolerance.
    public static void AssertTraceMatches(List<TraceEvent> actual, List<TraceEvent> expected)
    {
        Assertions.AssertThat(actual.Count).IsEqual(expected.Count);
        const double ptrTol = 1.0;
        for (int i = 0; i < expected.Count; i++)
        {
            Assertions.AssertThat(actual[i].Type).IsEqual(expected[i].Type);
            Assertions.AssertThat(actual[i].Node).IsEqual(expected[i].Node);
            Assertions.AssertThat(actual[i].UiFrame).IsEqual(expected[i].UiFrame);
            Assertions.AssertThat(Math.Abs(actual[i].PointerX - expected[i].PointerX) <= ptrTol).IsTrue();
            Assertions.AssertThat(Math.Abs(actual[i].PointerY - expected[i].PointerY) <= ptrTol).IsTrue();
        }
    }

    static string ToJson(TraceEvent ev)
    {
        return "{\"type\":\"" + ev.Type
            + "\",\"node\":\"" + ev.Node
            + "\",\"uiFrame\":" + ev.UiFrame
            + ",\"pointer\":{\"x\":" + ev.PointerX.ToString("R", CultureInfo.InvariantCulture)
            + ",\"y\":" + ev.PointerY.ToString("R", CultureInfo.InvariantCulture) + "}}";
    }
}
