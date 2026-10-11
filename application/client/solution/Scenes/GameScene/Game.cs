using System.IO.Compression;
using GdUnit4.Examples.Basics.Setup.Sources.UI;
using Godot;
using NewGameProject.Runtime;

public partial class Game : Node {
    public static string? ARCHIVE_DIR;
    public static bool RUN_RUNTIME_LOOP = true;
    public static bool SKIP_CREATE_ARCHIVE = false;
    public static bool TEST_MODE = false;
    static bool _runtimeRunning = false;
    bool _ready = false;
    int _frameCount = 0;
    ActionsWindow? _actionsWindow;

    public override void _Ready() {
        RuntimeInterop.RegisterLogger(m => GD.Print(m));

        if (ARCHIVE_DIR == null && !SKIP_CREATE_ARCHIVE) {
            string zip = CreateArchive(@"E:\workspace\xml-xsd2\application\client\solution\MainModule");
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

        RuntimeInterop.emitAction("increment");

        if (RUN_RUNTIME_LOOP && !_runtimeRunning) {
            _runtimeRunning = true;
            new Thread(() => {
                var stopwatch = new System.Diagnostics.Stopwatch();
                const long cycleDurationMs = 250;

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
