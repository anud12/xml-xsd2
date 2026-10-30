using NewGameProject.Tests.XUnit;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_0.Expression.ListReadOnlyExpression.Map;

[TestSuite]
public class TestClass : Steps
{
    [TestCategory("Step_0")]
    [TestCase]
    public void map_returns_a_list_of_the_mapped_element_type()
    {
        // The assertion lives in src/index.js (JSDoc), which the solution
        // tsconfig (checkJs) covers. The configured lint (`tsc --noEmit`)
        // validates it; a wrong `map` signature makes the lint fail.
        var solutionDir = ResolveSolutionDir();
        var project = Path.Combine(solutionDir, "tsconfig.json");
        Assertions.AssertThat(File.Exists(project))
            .OverrideFailureMessage($"solution tsconfig not found at {project}")
            .IsTrue();

        var nodeExe = ResolveNode();
        var tscJs = ResolveTsc(solutionDir);
        Assertions.AssertThat(nodeExe != null && tscJs != null)
            .OverrideFailureMessage($"Could not locate node (={nodeExe}) and tsc (={tscJs}) to run the lint")
            .IsTrue();

        var ok = RunTsc(nodeExe!, tscJs!, solutionDir, project);
        Assertions.AssertThat(ok)
            .OverrideFailureMessage("lint (tsc --noEmit) reported type errors: ListReadOnlyExpression.map does not return ListReadOnlyExpression<U> of the mapped element type")
            .IsTrue();
    }

    // <Map> is 6 levels below the solution root:
    // solution/Tests/GameScene/Stage_0/Expression/ListReadOnlyExpression/Map
    private static string ResolveSolutionDir([CallerFilePath] string callerFile = "")
        => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(callerFile)!, "..", "..", "..", "..", "..", ".."));

    // Locates node.exe: PATH first, then the conventional install dirs.
    private static string ResolveNode()
    {
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

    // Prefers the solution's local TypeScript (what `npx tsc` resolves to),
    // then falls back to the npm global / node install dirs.
    private static string ResolveTsc(string solutionDir)
    {
        var local = Path.Combine(solutionDir, "node_modules", "typescript", "lib", "tsc.js");
        if (File.Exists(local)) return local;

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

    private static bool RunTsc(string nodeExe, string tscJs, string workingDir, string project)
    {
        var psi = new ProcessStartInfo(nodeExe)
        {
            Arguments = $"\"{tscJs}\" --noEmit --project \"{project}\"",
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

        Console.WriteLine($"DEBUG lint tsc exit={process.ExitCode} stderr={stderr}");
        return process.ExitCode == 0;
    }
}
