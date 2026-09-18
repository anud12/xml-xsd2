using GdUnit4.Examples.Basics.Setup.Sources.UI;
using Godot;
using NewGameProject.Tests.XUnit;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
        var stageDir = Path.Combine(repoRoot, "Tests", "GameScene", "Stage_0", "Typescript");

        // Build with the TypeScript compiler directly (node.exe tsc.js) instead
        // of npx: the Godot test-host process does not inherit the shell PATH,
        // so npx.cmd cannot resolve node/tsc there. Resolving both binaries
        // explicitly makes the build work in that context.
        var nodeExe = ResolveNode();
        var tscJs = ResolveTsc();
        if (nodeExe == null || tscJs == null)
        {
            Assertions.AssertThat(false)
                .OverrideFailureMessage($"Could not locate node.exe (={nodeExe}) and tsc.js (={tscJs}) to build the stage module")
                .IsTrue();
        }

        var result = RunTsc(nodeExe, tscJs, stageDir, "src/tsconfig.json");
        Assertions.AssertThat(result)
            .OverrideFailureMessage($"tsc failed to build the stage module (dir={stageDir}, node={nodeExe}, tsc={tscJs})")
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

    // Locates node.exe: PATH first, then the conventional install dirs.
    private static string ResolveNode() {
        var exe = OperatingSystem.IsWindows() ? "node.exe" : "node";
        foreach (var dir in (System.Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            try
            {
                var p = Path.Combine(dir, exe);
                if (File.Exists(p)) return p;
            }
            catch { /* ignore malformed PATH entries */ }
        }
        var candidates = OperatingSystem.IsWindows()
            ? new[] { @"C:\Program Files\nodejs\node.exe" }
            : new[] { "/usr/local/bin/node", "/usr/bin/node" };
        return candidates.FirstOrDefault(File.Exists);
    }

    // Locates the TypeScript compiler entrypoint (lib/tsc.js). npx normally
    // resolves it, but the Godot host has no PATH, so probe the npm global
    // root, the node install dir, and common local caches.
    private static string ResolveTsc() {
        var candidates = new List<string>();
        var home = System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile);
        var npmGlobal = OperatingSystem.IsWindows()
            ? Path.Combine(home, "AppData", "Roaming", "npm", "node_modules")
            : Path.Combine(home, ".npm", "lib", "node_modules");
        candidates.Add(npmGlobal);
        candidates.Add(@"C:\Program Files\nodejs\node_modules");
        var nodeDir = Path.GetDirectoryName(ResolveNode());
        if (!string.IsNullOrEmpty(nodeDir)) candidates.Add(Path.Combine(nodeDir, "node_modules"));

        foreach (var searchDir in candidates.Distinct())
        {
            if (!Directory.Exists(searchDir)) continue;
            var match = Directory
                .GetFiles(searchDir, "tsc.js", SearchOption.AllDirectories)
                .FirstOrDefault(f => f.Contains(Path.Combine("typescript", "lib")));
            if (match != null) return match;
        }
        return null;
    }

    private static bool RunTsc(string nodeExe, string tscJs, string workingDir, string project) {
        var psi = new ProcessStartInfo(nodeExe) {
            Arguments = $"\"{tscJs}\" --project \"{project}\"",
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
