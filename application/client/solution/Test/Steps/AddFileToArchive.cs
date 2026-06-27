using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace NewGameProject.Tests.XUnit;

public partial class Steps {
    private string? _currentArchivePath;

    public Steps AddFileToArchive(string argFilePathToBeAdded, string expectedFileName,
        [CallerFilePath] string callerPath = "") {
        var repoRoot = FindRepoRoot();

        // On Linux, [CallerFilePath] returns the Windows path baked into the PDB.
        // Extract the relative path from the solution directory, then resolve against
        // the assembly's actual location at runtime.
        string callerDir;
        var dirName = Path.GetDirectoryName(callerPath);
        if (!string.IsNullOrEmpty(dirName)
            && char.IsLetter(dirName[0]) && dirName.Length > 1 && dirName[1] == ':')
        {
            // Windows path like E:\workspace\xml-xsd2\app\client\solution\Test\Stage_1\Anchor\
            // Find the "/solution/" marker in the path, then take everything after it.
            var normalizedDir = dirName.Replace('\\', '/').Substring(2); // strip E:
            string relFromSolution;
            var solutionIndex = normalizedDir.IndexOf("/solution/");
            if (solutionIndex >= 0)
            {
                var afterFirst = normalizedDir.Substring(solutionIndex + 9); // skip "/solution/"
                var secondIndex = afterFirst.IndexOf("/solution/");
                if (secondIndex >= 0)
                    relFromSolution = afterFirst.Substring(secondIndex + 9); // after second "/solution/"
                else
                    relFromSolution = afterFirst;
            }
            else
            {
                relFromSolution = normalizedDir;
            }
            // Resolve against the assembly's actual directory
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                ?? Directory.GetCurrentDirectory();
            // Walk up to find the solution directory (contains .sln)
            var solutionDir = FindSolutionDir(assemblyDir);
            callerDir = Path.Combine(solutionDir, relFromSolution);
        }
        else
        {
            callerDir = dirName ?? repoRoot;
        }

        var filePathToBeAdded = Path.Combine(callerDir, argFilePathToBeAdded);
        var normalized = filePathToBeAdded.Replace('\\', Path.DirectorySeparatorChar);

        var sourcePath = Path.IsPathRooted(normalized) ? normalized : Path.Combine(repoRoot, normalized);

        Console.WriteLine($"DEBUG AddFileToArchive: callerPath={callerPath}");
        Console.WriteLine($"DEBUG AddFileToArchive: callerDir={callerDir}");
        Console.WriteLine($"DEBUG AddFileToArchive: repoRoot={repoRoot}");
        Console.WriteLine($"DEBUG AddFileToArchive: argFilePathToBeAdded={argFilePathToBeAdded}");
        Console.WriteLine($"DEBUG AddFileToArchive: filePathToBeAdded={filePathToBeAdded}");
        Console.WriteLine($"DEBUG AddFileToArchive: normalized={normalized}");
        Console.WriteLine($"DEBUG AddFileToArchive: sourcePath={sourcePath}");
        Console.WriteLine($"DEBUG AddFileToArchive: File.Exists={File.Exists(sourcePath)}");

        if (!File.Exists(sourcePath))
            throw new FileNotFoundException("File to add not found: " + sourcePath);

        var entryName = (expectedFileName ?? Path.GetFileName(sourcePath)).Replace('\\', '/').TrimStart('/');

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

    /// Walk up from the given directory to find the one containing a .sln file.
    private static string FindSolutionDir(string startDir)
    {
        var dir = new DirectoryInfo(startDir);
        while (dir != null)
        {
            if (dir.GetFiles("*.sln").Length > 0) return dir.FullName;
            dir = dir.Parent;
        }
        return startDir;
    }

    protected void CleanupArchive() {
        if (!string.IsNullOrEmpty(_currentArchivePath) && File.Exists(_currentArchivePath))
            File.Delete(_currentArchivePath);
        _currentArchivePath = null;
    }
}
