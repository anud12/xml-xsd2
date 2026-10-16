using System.IO.Compression;
using NewGameProject.Module;
using NewGameProject.Tests.XUnit;

namespace GdUnit4.Examples.Basics.Setup.Test.Ffi.Module.HotReload;

[TestSuite]
public class ModuleArchiveTests : Steps
{
    [TestCategory("HotReload")]
    [TestCase]
    [RequireGodotRuntime]
    public void CreateZipFromDir_zips_every_file_with_relative_slash_names()
    {
        using var dir = MakeTempModule(new()
        {
            ["index.js"] = "export default () => {}",
            ["manifest.json"] = "{ \"entry\": \"index.js\" }",
            ["maps/texture.png"] = "binary-ish-bytes",
        });
        string zip = ModuleArchive.CreateZipFromDir(dir.Path);
        try
        {
            using var z = ZipFile.OpenRead(zip);
            var names = z.Entries.Select(e => e.FullName).OrderBy(n => n).ToArray();
            Assertions.AssertThat(names).IsEqual(new[] { "index.js", "manifest.json", "maps/texture.png" });
        }
        finally
        {
            File.Delete(zip);
        }
    }

    [TestCategory("HotReload")]
    [TestCase]
    [RequireGodotRuntime]
    public void HasChanged_is_false_for_identical_snapshots()
    {
        using var dir = MakeTempModule(new() { ["index.js"] = "v1" });
        var before = ModuleArchive.Snapshot(dir.Path);
        var after = ModuleArchive.Snapshot(dir.Path);
        Assertions.AssertThat(ModuleArchive.HasChanged(before, after)).IsFalse();
    }

    [TestCategory("HotReload")]
    [TestCase]
    [RequireGodotRuntime]
    public void HasChanged_detects_add_remove_and_rewrite()
    {
        using var dir = MakeTempModule(new() { ["index.js"] = "v1" });
        var before = ModuleArchive.Snapshot(dir.Path);

        File.WriteAllText(Path.Combine(dir.Path, "extra.js"), "new file");
        Assertions.AssertThat(ModuleArchive.HasChanged(before, ModuleArchive.Snapshot(dir.Path))).IsTrue();

        File.Delete(Path.Combine(dir.Path, "extra.js"));
        File.WriteAllText(Path.Combine(dir.Path, "index.js"), "v2-changed-content");
        Assertions.AssertThat(ModuleArchive.HasChanged(before, ModuleArchive.Snapshot(dir.Path))).IsTrue();
    }

    sealed record TempModule(string Path) : IDisposable
    {
        public void Dispose() => Directory.Delete(Path, true);
    }

    static TempModule MakeTempModule(Dictionary<string, string> files)
    {
        string path = Path.Combine(Path.GetTempPath(), $"modtest_{Guid.NewGuid()}");
        foreach (var (name, content) in files)
        {
            string full = Path.Combine(path, name.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(full)!);
            File.WriteAllText(full, content);
        }
        return new TempModule(path);
    }
}
