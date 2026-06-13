using System.IO.Compression;
using GdUnit4.Examples.Basics.Setup.Sources.UI;
using Godot;
using NewGameProject.Runtime;

public partial class Game : Node {
    public static string? ARCHIVE_DIR;
    public static bool RUN_RUNTIME_LOOP = true;
    public static bool SKIP_CREATE_ARCHIVE = false;
    public static bool TEST_MODE = false;
    bool _ready = false;
    int _frameCount = 0;

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
        }

        _ready = true;

        if (!TEST_MODE) {
            // Remove all existing children (cleanup from previous runs)
            while (GetChildCount() > 0) {
                var child = GetChild(0);
                RemoveChild(child);
                child.QueueFree();
            }

            // Create fresh RootNode with panels from current archive state
            var root = new RootNode();
            root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            AddChild(root);
        }

        RuntimeInterop.emitAction("increment");

        if (RUN_RUNTIME_LOOP) {
            new Thread(() => {
                var stopwatch = new System.Diagnostics.Stopwatch();
                const long cycleDurationMs = 250;

                while (RUN_RUNTIME_LOOP) {
                    stopwatch.Restart();
                    RuntimeInterop.RunIteration(10);
                    stopwatch.Stop();

                    long sleepTimeMs = cycleDurationMs - stopwatch.ElapsedMilliseconds;
                    Console.WriteLine("Sleep "+ sleepTimeMs);
                    if (sleepTimeMs > 0) {
                        Thread.Sleep((int)sleepTimeMs);
                    }
                }
            }).Start();
        }
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
