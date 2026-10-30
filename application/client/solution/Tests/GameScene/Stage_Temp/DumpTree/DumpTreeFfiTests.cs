using System;
using System.IO;
using GdUnit4.Examples.Basics.Setup.Sources.UI;
using Godot;
using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_temp.DumpTree;

[TestSuite]
public partial class DumpTreeFfiTests : Steps {
    const string ModuleRoot = "E:\\workspace\\xml-xsd2\\application\\client\\solution\\Modules\\TestTick";

    [TestCategory("Stage_temp")]
    [TestCase]
    [RequireGodotRuntime]
    public async Task Given_full_testtick_module_after_first_tick_dump_node_screenshots() {
        CleanupArchive();
        foreach (var file in Directory.GetFiles(ModuleRoot, "*.*", SearchOption.AllDirectories)) {
            var rel = Path.GetRelativePath(ModuleRoot, file).Replace('\\', '/');
            AddFileToArchive("../../../../Modules/TestTick/" + rel, rel);
        }
        ProcessArchive();

        var scene = await AttachUiScene();

        // The duplicate-label issue reproduces on the FIRST tick: the add-label
        // field window is created in Pass 1 and only freed at end-of-frame, so
        // during this frame both it and the host label are live in the tree.
        RuntimeInterop.RunIteration(1);
        await runner.SimulateFrames(1);

        var property = RuntimeInterop.ReadEntityNumberValue("game", "property");
        var addBtnText = scene.Window("add-btn").GetNodeOrNull<Label>("text")?.Text ?? "<none>";
        var addLabel = scene.GetWindowOrNull("add-label");
        Console.WriteLine($"=== FIRST TICK property='{property}' add-btn.text='{addBtnText}' add-label window={(addLabel != null ? "EXISTS" : "absent")} ===");

        // Dump the tree and a cropped screenshot per visible node so the
        // duplicate can be located visually.
        var outDir = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "..", "..", "..", "..",
            "Tests", "GameScene", "Stage_Temp", "DumpTree", "node_shots");
        outDir = Path.GetFullPath(outDir);
        scene.DumpTree();
        scene.DumpNodeScreenshots(outDir);
        Console.WriteLine($"=== node screenshots written to: {outDir} ===");
    }
}
