using System.Collections.Concurrent;
using System.IO.Compression;
using GdUnit4.Examples.Basics.Setup.Sources.UI;
using Godot;
using NewGameProject.Module;
using NewGameProject.Runtime;

public partial class Game : Node {
    // The runtime loop runs on a background thread, but Godot's GD.Print is only
    // safe from the main thread; printing from the worker thread interleaves and
    // misaligns console output. Runtime logs are buffered here (thread-safe) and
    // flushed on the main thread in _Process instead.
    static readonly ConcurrentQueue<string> _pendingLogs = new();
    public static string? ARCHIVE_DIR;
    public static string? MODULE_DIR;
    public static bool RUN_RUNTIME_LOOP = true;
    public static bool SKIP_CREATE_ARCHIVE = false;
    public static bool TEST_MODE = false;
    public static bool AUTO_HOT_RELOAD = false;
    static bool _runtimeRunning = false;
    bool _ready = false;
    int _frameCount = 0;
    ActionsWindow? _actionsWindow;
    ModuleWatcher? _moduleWatcher;
    bool _hotReloadPending = false;

    public override void _Ready() {
        // Buffer runtime logs; flushed on the main thread in _Process (see above).
        RuntimeInterop.RegisterLogger(_pendingLogs.Enqueue);
        // The module picker (SelectModule) sets this before loading the game
        // scene; fall back to the first module when the game is launched
        // directly.
        MODULE_DIR ??= ProjectSettings.GlobalizePath("res://Modules/TestTick");

        if (ARCHIVE_DIR == null && !SKIP_CREATE_ARCHIVE) {
            string zip = CreateArchive(MODULE_DIR);
            string db = RuntimeInterop.ProcessArchive(zip);
            if (db == null) {
                GD.PrintErr("Failed to load archive");
                return;
            }
            GD.Print($"Archive loaded: {db}");
            GD.Print($"Loaded {RuntimeInterop.GetPanelIds().Length} panels");
        }

        _ready = true;

        // UI layers: coreUI (hardcoded UI: settings, actions buttons) is the
        // base layer; moduleUI (the JS-module UI painted by RootNode) sits on
        // top of it, above the play area.
        var moduleUI = GetNode<Control>("uiLayers/moduleUI");
        var coreUI = GetNode<Control>("uiLayers/coreUI");
        if (!TEST_MODE) {
            // Remove all existing children (cleanup from previous runs)
            while (moduleUI.GetChildCount() > 0) {
                var child = moduleUI.GetChild(0);
                moduleUI.RemoveChild(child);
                child.QueueFree();
            }

            // Create fresh RootNode with panels from current archive state
            var root = new RootNode();
            root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            moduleUI.AddChild(root);
        }

        // Settings button is available in both game and test modes.
        GetNode<Button>("uiLayers/coreUI/SettingsButton").Pressed += OnSettingsButton;
        GetNode<Button>("uiLayers/coreUI/ActionsButton").Pressed += ToggleActionsWindow;
        GetNode<Button>("uiLayers/coreUI/ReloadButton").Pressed += DoHotReload;

        if (AUTO_HOT_RELOAD && !TEST_MODE) {
            _moduleWatcher = new ModuleWatcher(MODULE_DIR,
                () => _hotReloadPending = true);
            _moduleWatcher.Start();
        }

        RuntimeInterop.emitAction("increment");

        if (RUN_RUNTIME_LOOP && !_runtimeRunning) {
            _runtimeRunning = true;
            new Thread(() => {
                var stopwatch = new System.Diagnostics.Stopwatch();
                const long cycleDurationMs = 125;

                while (RUN_RUNTIME_LOOP) {
                    stopwatch.Restart();
                    RuntimeInterop.RunIteration(1);
                    stopwatch.Stop();

                    long sleepTimeMs = cycleDurationMs - stopwatch.ElapsedMilliseconds;
                    if (sleepTimeMs > 0) {
                        Thread.Sleep((int)sleepTimeMs);
                    }
                }
                _runtimeRunning = false;
            }).Start();
        }

    }

    public override void _Process(double delta) {
        // Flush runtime logs on the main thread so GD.Print output stays aligned.
        while (_pendingLogs.TryDequeue(out string line)) {
            GD.Print(line);
        }
        // Hot reloads raised by the file watcher arrive on a non-main thread;
        // the actual reload and UI repaint happen here on the main thread.
        if (_hotReloadPending) {
            _hotReloadPending = false;
            DoHotReload();
        }
    }

    public override void _ExitTree() {
        _moduleWatcher?.Dispose();
        _moduleWatcher = null;
    }

    /// Re-ships the module directory as an archive and hot-reloads it against
    /// the live runtime: the previous game state (entity values, elapsed
    /// time, in-flight action plans) is kept and the module's new
    /// declarations/actions take effect.
    void DoHotReload() {
        string dir = MODULE_DIR;
        if (dir == null || !Directory.Exists(dir)) {
            GD.PrintErr("Hot reload skipped: module directory missing");
            return;
        }
        string zip = ModuleArchive.CreateZipFromDir(dir);
        string? db = RuntimeInterop.ReloadArchive(zip);
        if (db == null) {
            GD.PrintErr("Hot reload aborted; the previous module stays live");
            return;
        }
        GD.Print($"Hot reload complete: {db}");
        GetNode<Control>("uiLayers/moduleUI")
            .GetChildren().OfType<RootNode>().FirstOrDefault()?.Paint();
        RuntimeInterop.emitAction("increment");
    }

    void OnSettingsButton() {
        GetTree().ChangeSceneToFile("res://Scenes/Settings/Settings.tscn");
    }

    void ToggleActionsWindow() {
        if (_actionsWindow != null && IsInstanceValid(_actionsWindow)) {
            _actionsWindow.QueueFree();
            _actionsWindow = null;
            return;
        }
        _actionsWindow = new ActionsWindow();
        GetNode<Control>("uiLayers/coreUI").AddChild(_actionsWindow);
    }

    string CreateArchive(string dir) {
        string z = Path.Combine(Path.GetTempPath(),
            $"mod_{Guid.NewGuid()}.zip");
        using var fs = new FileStream(z, FileMode.Create);
        using var a = new ZipArchive(fs,
            ZipArchiveMode.Create);
        foreach (var f in Directory.GetFiles(dir, "*.*",
                     SearchOption.AllDirectories)) {
            string n = Path.GetRelativePath(dir, f)
                .Replace('\\', '/');
            var e = a.CreateEntry(n);
            using var es = e.Open();
            using var s = File.OpenRead(f);
            s.CopyTo(es);
        }
        return z;
    }
}
