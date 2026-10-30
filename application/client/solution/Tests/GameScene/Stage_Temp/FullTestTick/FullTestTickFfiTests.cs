using System;
using System.IO;
using GdUnit4.Examples.Basics.Setup.Sources.UI;
using Godot;
using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_temp.FullTestTick;

[TestSuite]
public partial class FullTestTickFfiTests : Steps {
    const string ModuleRoot = "E:\\workspace\\xml-xsd2\\application\\client\\solution\\Modules\\TestTick";

    [TestCategory("Stage_temp")]
    [TestCase]
    [RequireGodotRuntime]
    public async Task Given_full_testtick_module_when_ticks_run_add_btn_label_stays_clean() {
        CleanupArchive();
        // Load the real, compiled TestTick module (all files, correct in-archive paths).
        foreach (var file in Directory.GetFiles(ModuleRoot, "*.*", SearchOption.AllDirectories)) {
            var rel = Path.GetRelativePath(ModuleRoot, file).Replace('\\', '/');
            AddFileToArchive("../../../../Modules/TestTick/" + rel, rel);
        }
        ProcessArchive();

        var scene = await AttachUiScene();

        // Advance time so the 1ms interest/value ticks and the 10ms land tick fire
        // many times, stressing the add-btn label re-render.
        for (int i = 0; i < 40; i++) {
            RuntimeInterop.RunIteration(10);
            await runner.SimulateFrames(1);
        }

        var lands = ContainerInterop.GetContainerById("lands");
        var label = scene.Window("add-btn").GetNodeOrNull<Label>("text");
        var labelText = label?.Text ?? "<none>";
        var property = RuntimeInterop.ReadEntityNumberValue("game", "property");
        var childExists = scene.GetWindowOrNull("add-label") != null;
        Console.WriteLine($"DEBUG FullTestTick lands={lands.Entities.Length} add-btn label='{labelText}' property='{property}' add-labelWindowExists={childExists}");

        // The add-btn label must show exactly the current property value as a
        // single clean number — never a concatenation of previous values.
        bool labelClean = double.TryParse(labelText, out var lv)
            && double.TryParse(property, out var pv)
            && lv == pv;
        Assertions.AssertThat(labelClean)
            .OverrideFailureMessage($"add-btn label '{labelText}' != current property '{property}' (text is not cleared / is accumulating)")
            .IsTrue();
    }

    [TestCategory("Stage_temp")]
    [TestCase]
    [RequireGodotRuntime]
    public async Task Given_full_module_when_add_btn_clicked_repeatedly_label_stays_clean() {
        CleanupArchive();
        foreach (var file in Directory.GetFiles(ModuleRoot, "*.*", SearchOption.AllDirectories)) {
            var rel = Path.GetRelativePath(ModuleRoot, file).Replace('\\', '/');
            AddFileToArchive("../../../../Modules/TestTick/" + rel, rel);
        }
        ProcessArchive();

        var scene = await AttachUiScene();

        // Let the 1ms interest tick drift property, and simulate the user rapidly
        // clicking the add-btn (each click emits the `increment` action).
        for (int i = 0; i < 30; i++) {
            RuntimeInterop.RunIteration(10);
            RuntimeInterop.UiJsClick("add-btn", 0, 0);
            await runner.SimulateFrames(1);
        }

        var label = scene.Window("add-btn").GetNodeOrNull<Label>("text");
        var labelText = label?.Text ?? "<none>";
        var property = RuntimeInterop.ReadEntityNumberValue("game", "property");
        Console.WriteLine($"DEBUG FullTestTick-click add-btn label='{labelText}' property='{property}'");

        bool labelClean = double.TryParse(labelText, out var lv)
            && double.TryParse(property, out var pv)
            && lv == pv;
        Assertions.AssertThat(labelClean)
            .OverrideFailureMessage($"add-btn label '{labelText}' != current property '{property}' after clicks (text is not cleared / is accumulating)")
            .IsTrue();
    }
}
