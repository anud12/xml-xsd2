using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using Xunit;
using Xunit.Gherkin.Quick;

namespace NewGameProject;

class State
{
}

[FeatureFile(@".*Features/step1/.*\.feature", FeatureFilePathType.Regex)]
public class InteropFeatures : Feature
{
    private State state = new();
    private string _currentArchivePath = Path.Combine(Path.GetTempPath(), "module.zip");


    private string FindRepoRoot()
    {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir != null)
        {
            if (dir.GetFiles("*.sln").Length > 0) return dir.FullName;
            dir = dir.Parent;
        }

        return Directory.GetCurrentDirectory();
    }

    [Given(@"I load the runtime")]
    public void I_load_the_runtime()
    {
        // Ensure a fresh archive is used for each scenario to avoid stale entries from previous runs.
        try
        {
            if (!string.IsNullOrEmpty(_currentArchivePath) && File.Exists(_currentArchivePath))
                File.Delete(_currentArchivePath);
            // Use a fresh unique archive name per scenario to avoid cross-test contamination.
            _currentArchivePath = Path.Combine(Path.GetTempPath(), "module_" + Guid.NewGuid().ToString() + ".zip");
        }
        catch (Exception ex)
        {
            throw new Exception("Could not create temporary archive: " + ex.Message);
        }
    }

    [Given(@"I have added {string} file as {string} to archive")]
    public void I_have_added_string_file_as_string_to_archive(string path, string name)
    {
        if (string.IsNullOrEmpty(_currentArchivePath))
            _currentArchivePath = Path.Combine(Directory.GetCurrentDirectory(), "module.zip");

        // Resolve source file path (support patterns like ./<directory>/file and relative paths)
        string sourcePath = path ?? string.Empty;
        if (sourcePath.StartsWith("./") || sourcePath.StartsWith(".\\"))
            sourcePath = sourcePath.Substring(2);

        sourcePath = sourcePath.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);

        if (sourcePath.Contains("<directory>"))
        {
            var filename = Path.GetFileName(sourcePath);
            var matches = Directory.GetFiles(FindRepoRoot(), filename, SearchOption.AllDirectories)
                .Where(p => p.Replace(Path.DirectorySeparatorChar, '/').Contains("/Features/")).ToArray();
            if (matches.Length == 0)
                matches = Directory.GetFiles(FindRepoRoot(), filename, SearchOption.AllDirectories);
            if (matches.Length == 0)
                throw new FileNotFoundException($"Could not find file {filename} under project.");
            sourcePath = matches[0];
        }
        else if (!Path.IsPathRooted(sourcePath) && !File.Exists(sourcePath))
        {
            var candidate = Path.Combine(FindRepoRoot(), sourcePath);
            if (File.Exists(candidate))
            {
                sourcePath = candidate;
            }
            else
            {
                var filename = Path.GetFileName(sourcePath);
                // If the caller supplied a relative subpath, prefer matches that end with that subpath
                var relPathNormalized = sourcePath.Replace('\\', '/').TrimStart('/');
                var matches = Directory.GetFiles(FindRepoRoot(), filename, SearchOption.AllDirectories)
                    .Where(p => p.Replace(Path.DirectorySeparatorChar, '/').EndsWith(relPathNormalized))
                    .ToArray();
                if (matches.Length == 0)
                {
                    matches = Directory.GetFiles(FindRepoRoot(), filename, SearchOption.AllDirectories);
                }

                if (matches.Length == 0)
                {
                    throw new FileNotFoundException($"Could not find file {filename} under project.");
                }

                sourcePath = matches[0];
            }
        }

        var content = File.ReadAllBytes(sourcePath);

        string destinationName = name ?? string.Empty;
        if (destinationName.StartsWith("./") || destinationName.StartsWith(".\\"))
            destinationName = destinationName.Substring(2);
        destinationName = destinationName.Replace('\\', '/');

        // By default, place the destination file in the same relative directory inside the archive
        // as the source file is located (relative to repo root). This allows multiple modules to coexist in one zip.
        string entryName = destinationName;
        try
        {
            var repoRoot = FindRepoRoot();
            var relPath = Path.GetRelativePath(repoRoot, sourcePath).Replace('\\', '/');
            var dir = Path.GetDirectoryName(relPath)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(dir))
            {
                entryName = dir.TrimEnd('/') + "/" + destinationName.TrimStart('/');
            }
        }
        catch
        {
            /* fallback to destinationName */
        }

        // Ensure archive exists
        if (!File.Exists(_currentArchivePath))
        {
            using (var fs = new FileStream(_currentArchivePath, FileMode.Create))
            using (var writer = new ZipArchive(fs, ZipArchiveMode.Create))
            {
            }
        }

        // Add or replace entry in the archive
        using (var fs = new FileStream(_currentArchivePath, FileMode.Open, FileAccess.ReadWrite))
        using (var archive = new ZipArchive(fs, ZipArchiveMode.Update))
        {
            var existing = archive.GetEntry(entryName);
            if (existing != null) existing.Delete();
            var entry = archive.CreateEntry(entryName);
            using (var es = entry.Open())
                es.Write(content, 0, content.Length);
        }
    }

    [When(@"I load current archive")]
    public void I_load_current_archive()
    {
        // Ensure native runtime DLL is accessible to the test process (copy to test output folder if needed)
        try
        {
            var projectRoot = FindRepoRoot();
            var dllSource = Path.Combine(projectRoot, "libxml_xsd2.dll");
            var asmDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? projectRoot;
            var dllDest = Path.Combine(asmDir, "libxml_xsd2.dll");
            if (File.Exists(dllSource))
            {
                try
                {
                    // Copy if missing or if source is newer than destination to ensure runtime changes are picked up by tests.
                    if (!File.Exists(dllDest) ||
                        File.GetLastWriteTimeUtc(dllSource) > File.GetLastWriteTimeUtc(dllDest))
                        File.Copy(dllSource, dllDest, true);
                }
                catch (Exception ex)
                {
                    throw new Exception("Could not copy native DLL: " + ex.Message);
                }
            }
        }
        catch (Exception ex)
        {
            throw new Exception("Could not find native DLL: " + ex.Message);
        }

        var dbPath = RuntimeInterop.ProcessArchive(_currentArchivePath);
        // store or log dbPath if needed for further assertions
    }

    [Then(@"assert that `GetPanelIds` returns {string}")]
    public void Assert_that_getPanelIds_returns(string expectedList)
    {
        var resultString = RuntimeInterop.GetPanelIds();
        var expectedListArray = expectedList.Split(",");
        Assert.Equal(expectedListArray, resultString);
    }

    [Then(@"assert that `GetPanelData` for {string} has id {string}")]
    public void Assert_that_getPanelIds_returns(string panelId, string expectedId)
    {
        var resultPanel = RuntimeInterop.GetPanelById(panelId);
        Assert.Equal(resultPanel.Id, expectedId);
    }
}