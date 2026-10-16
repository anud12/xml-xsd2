using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using GdUnit4;
using Godot;
using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;

namespace GdUnit4.Examples.Basics.Setup.Test.Ffi.Module.TestTick;

[TestSuite]
public partial class TestTickModuleTests : Steps
{
    [TestCase]
    [RequireGodotRuntime]
    public void Given_test_tick_module_when_processed_its_declarations_are_live()
    {
        // The module ships TypeScript source compiled to dist/index.js; this
        // exercises the compiled entry (manifest entry = "dist/index.js") end
        // to end: zipped, processed by the Rust runtime, and its declarations
        // (entity / container / action) become live.
        string moduleDir = ProjectSettings.GlobalizePath("res://Modules/TestTick");
        EnsureRuntimeDll(moduleDir);
        string zip = ZipDir(moduleDir);
        try
        {
            RuntimeInterop.ClearState();
            string db = RuntimeInterop.ProcessArchive(zip);
            Assertions.AssertThat(db).IsNotNull();
            RuntimeInterop.RunIteration(1);

            Assertions.AssertThat(double.Parse(
                RuntimeInterop.GetEntityNumberMapValue("node-1", "column"))).IsEqual(0.0);
            Assertions.AssertThat(double.Parse(
                RuntimeInterop.GetEntityNumberMapValue("node-1", "row"))).IsEqual(0.0);
            Assertions.AssertThat(ContainerInterop.GetContainerIds()).Contains("grid-1");
            Assertions.AssertThat(RuntimeInterop.GetActionIds()).Contains("move-to-cursor");
        }
        finally
        {
            File.Delete(zip);
        }
    }

    /// Copies the freshly built native runtime next to the test assembly so the
    /// direct P/Invoke calls below can resolve it (mirrors the ProcessArchive
    /// step, which copies it before processing).
    static void EnsureRuntimeDll(string moduleDir)
    {
        // moduleDir = <solution>/Modules/TestTick -> solution is two levels up.
        string solutionDir = Path.GetDirectoryName(
            Path.GetDirectoryName(moduleDir)!)!;
        string asmDir = Path.GetDirectoryName(
            Assembly.GetExecutingAssembly().Location) ?? solutionDir;
        string dllSource = Path.Combine(solutionDir, "libxml_xsd2.dll");
        string dllDest = Path.Combine(asmDir, "libxml_xsd2.dll");
        if (File.Exists(dllSource) &&
            (!File.Exists(dllDest) ||
             File.GetLastWriteTimeUtc(dllSource) > File.GetLastWriteTimeUtc(dllDest)))
        {
            File.Copy(dllSource, dllDest, true);
        }
    }

    static string ZipDir(string dir)
    {
        string zip = Path.Combine(Path.GetTempPath(), $"mod_{Guid.NewGuid()}.zip");
        using var fs = new FileStream(zip, FileMode.Create);
        using var archive = new ZipArchive(fs, ZipArchiveMode.Create);
        foreach (var file in Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories))
        {
            string entryName = Path.GetRelativePath(dir, file).Replace('\\', '/');
            var entry = archive.CreateEntry(entryName);
            using var es = entry.Open();
            using var s = File.OpenRead(file);
            s.CopyTo(es);
        }
        return zip;
    }
}
