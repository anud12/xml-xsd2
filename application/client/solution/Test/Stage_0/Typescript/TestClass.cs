using GdUnit4.Examples.Basics.Setup.Sources.UI;
using Godot;
using NewGameProject.Tests.XUnit;
using System.Diagnostics;
using Vector2 = Godot.Vector2;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_0.Typescript;

[TestSuite]
public class TestClass : Steps {
    [TestCategory("Step_0")]
    [TestCase]
    [RequireGodotRuntime]
    public async Task EmittedModule_it_should_build_the_typescript_module_and_load_it() {
        try {
            var stageDir = BuildModule();

            AddFileToArchive("module/index.js", "index.js")
                .AddFileToArchive("module/panels/base.js", "panels/base.js")
                .AddFileToArchive("module/panels/offset.js", "panels/offset.js")
                .AddFileToArchive("module/manifest.json", "manifest.json")
                .EnsureDllAccessible()
                .ProcessArchive();

            var scene = await AttachUiScene();

            // "base" comes from the emitted panels/base.js and "offset" from
            // panels/offset.js: both are only reachable if the runtime followed
            // the relative ESM imports emitted by tsc.
            scene.AssertPanelThat("base").IsNonNull();
            scene.AssertPanelThat("offset").IsNonNull();

            DebugSaveScreenshot("result.png");
        }
        catch (Exception e) {
            Assertions.AssertThat(true)
                .OverrideFailureMessage($"Error: {e.Message}\n{e.StackTrace}")
                .IsFalse();
        }
    }

    private string BuildModule() {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir != null && dir.GetFiles("*.sln").Length == 0) {
            dir = dir.Parent;
        }
        var repoRoot = dir?.FullName ?? Directory.GetCurrentDirectory();
        var stageDir = Path.Combine(repoRoot, "Test", "Stage_0", "Typescript");

        var result = RunNpx(stageDir, "tsc --project src/tsconfig.json");
        Assertions.AssertThat(result)
            .OverrideFailureMessage($"tsc failed to build the stage module (dir={stageDir})")
            .IsTrue();
        Assertions.AssertThat(File.Exists(Path.Combine(stageDir, "module", "index.js")))
            .OverrideFailureMessage("tsc did not emit module/index.js")
            .IsTrue();
        Assertions.AssertThat(File.Exists(Path.Combine(stageDir, "module", "panels", "base.js")))
            .OverrideFailureMessage("tsc did not emit module/panels/base.js")
            .IsTrue();
        Assertions.AssertThat(File.Exists(Path.Combine(stageDir, "module", "panels", "offset.js")))
            .OverrideFailureMessage("tsc did not emit module/panels/offset.js")
            .IsTrue();
        return stageDir;
    }

    private static bool RunNpx(string workingDir, string arguments) {
        var npx = OperatingSystem.IsWindows() ? "npx.cmd" : "npx";
        var psi = new ProcessStartInfo(npx, arguments) {
            WorkingDirectory = workingDir,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        using var process = Process.Start(psi);
        if (process == null) return false;

        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        Console.WriteLine($"DEBUG tsc exit={process.ExitCode} stdout={stdout} stderr={stderr}");
        return process.ExitCode == 0;
    }
}
