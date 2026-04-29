using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;

namespace NewGameProject.Tests.XUnit;

public partial class Steps {
    private string? _currentArchivePath;

    public Steps AddFileToArchive(string argFilePathToBeAdded, string expectedFileName,
        [CallerFilePath] string callerPath = "") {
        // Resolve source path relative to repo root if needed

        var repoRoot = FindRepoRoot();
        var filePathToBeAdded = Path.GetDirectoryName(callerPath) + "\\" + argFilePathToBeAdded;
        var normalized = filePathToBeAdded.Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar);

        var sourcePath = Path.IsPathRooted(normalized) ? normalized : Path.Combine(repoRoot, normalized);

        Console.WriteLine($"DEBUG AddFileToArchive: callerPath={callerPath}");
        Console.WriteLine($"DEBUG AddFileToArchive: argFilePathToBeAdded={argFilePathToBeAdded}");
        Console.WriteLine($"DEBUG AddFileToArchive: sourcePath={sourcePath}");
        Console.WriteLine($"DEBUG AddFileToArchive: File.Exists={File.Exists(sourcePath)}");
        
        if (!File.Exists(sourcePath))
            throw new FileNotFoundException("File to add not found: " + sourcePath);

        // Prepare entry name inside the zip (use forward slashes)

        var entryName = (expectedFileName ?? Path.GetFileName(sourcePath)).Replace('\\', '/').TrimStart('/');


        // Create a new archive if we don't have one yet

        if (string.IsNullOrEmpty(_currentArchivePath) || !File.Exists(_currentArchivePath)) {
            _currentArchivePath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.zip");

            using (var fs = new FileStream(_currentArchivePath, FileMode.Create, FileAccess.Write))

            using (var archive = new ZipArchive(fs, ZipArchiveMode.Create)) {
                var entry = archive.CreateEntry(entryName);

                using (var es = entry.Open())

                using (var stream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read))

                    stream.CopyTo(es);
            }
        }

        else {
            // Update existing archive; replace existing entry if needed

            using (var fs = new FileStream(_currentArchivePath, FileMode.Open, FileAccess.ReadWrite))

            using (var archive = new ZipArchive(fs, ZipArchiveMode.Update)) {
                var existing = archive.Entries.FirstOrDefault(e => e.FullName == entryName);

                if (existing != null) existing.Delete();


                var entry = archive.CreateEntry(entryName);

                using (var es = entry.Open())

                using (var stream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read))

                    stream.CopyTo(es);
            }
        }


        return this;
    }

    protected void CleanupArchive() {
        if (!string.IsNullOrEmpty(_currentArchivePath) && File.Exists(_currentArchivePath))
            File.Delete(_currentArchivePath);
        _currentArchivePath = null;
    }
}