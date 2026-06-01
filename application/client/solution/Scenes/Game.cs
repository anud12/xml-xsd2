using System.IO.Compression;
using GdUnit4.Examples.Basics.Setup.Sources.UI;
using Godot;
using NewGameProject.Runtime;

public partial class Game : Node {
    public static string? ARCHIVE_DIR; 
    public static bool RUN_RUNTIME_LOOP = true;
    public static bool SKIP_CREATE_ARCHIVE = false;
    bool _ready = false;
    int _frameCount = 0;

    public override void _Ready() {
        RuntimeInterop.RegisterLogger(m => GD.Print(m));
        string zip = CreateArchive(@"E:\workspace\xml-xsd2\application\client\solution\MainModule");
        string db;
        if (ARCHIVE_DIR != null) {
            db = RuntimeInterop.ProcessArchive(ARCHIVE_DIR);
        }
        else {
            db = RuntimeInterop.ProcessArchive(zip);
        }
        
        if (db != null) {
            _ready = true;
            GD.Print($"Archive loaded: {db}");
            var root = new RootNode();
            root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            AddChild(root);
            RuntimeInterop.emitAction("increment");
        }
        else {
            GD.PrintErr("Failed to load archive");
        }

        new Thread(() => {
            while (RUN_RUNTIME_LOOP) {
                RuntimeInterop.RunIteration();
            }
        }).Start();
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