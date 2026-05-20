using System;
using System.Diagnostics;
using System.IO;
using Godot;
using GdUnit4.Examples.Basics.Setup.Sources.UI;
using NewGameProject.Runtime;

public partial class Main : Node {
    private double _tickAccumulator = 0;
    private const double TickInterval = 0.1;
    private bool _firstFrame = true;
    private bool _iterationPending = false;

    public override void _Ready() {
        var modulePath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "module.zip"));

        if (!File.Exists(modulePath)) {
            GD.PrintErr($@"
**********************************************************
*  Missing module archive
*
*  Expected file not found:
*    {modulePath}
*
*  Place module.zip in the project root directory:
*    {Path.GetDirectoryName(modulePath)}
*
*  Aborting startup.
**********************************************************");
            return;
        }

        GD.Print($"[Main] Loading module from: {modulePath}");
        RuntimeInterop.ProcessArchive(modulePath);

        var rootNode = new RootNode();
        AddChild(rootNode);
        GD.Print("[Main] Runtime started.");
        Task.Run(() => {
            try {
                ulong gametime = 0L;
                const int intervalMs = 100;
                while (true) {
                    var start = Stopwatch.GetTimestamp();
                    RuntimeInterop.setGameTime(gametime);
                    RuntimeInterop.RunIteration();
                    var elapsed = (Stopwatch.GetTimestamp() - start) / (double)Stopwatch.Frequency;
                    var sleepMs = (int)((intervalMs / 1000.0 - elapsed) * 1000);
                    GD.Print(sleepMs);
                    if (sleepMs > 0) {
                        Thread.Sleep(sleepMs);
                    }
                    gametime += intervalMs;
                }
            }
            catch (Exception ex) {
                GD.PrintErr($"Thread died due to managed exception: {ex.Message}\n{ex.StackTrace}");
            }
        });
    }
}