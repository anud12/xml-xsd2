using System;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace NewGameProject.Tests.XUnit;

public partial class Steps
{
    
    private string? _currentArchivePath;
    
    public Steps AddFileToArchive(string path, string name)
    {
        // Initialize the archive if this is the first file being added
        if (string.IsNullOrEmpty(_currentArchivePath))
            _currentArchivePath = Path.Combine(Path.GetTempPath(), "module" + Guid.NewGuid() + ".zip");

        var repoRoot = FindRepoRoot();

        // Resolve source file path
        string sourcePath = path ?? string.Empty;
        if (sourcePath.StartsWith("./") || sourcePath.StartsWith(".\\"))
            sourcePath = sourcePath.Substring(2);

        // Normalize path separators
        sourcePath = sourcePath.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);

        // Handle wildcard patterns like <directory>
        if (sourcePath.Contains("<directory>"))
        {
            var filename = Path.GetFileName(sourcePath);
            var matches = Directory.GetFiles(repoRoot, filename, SearchOption.AllDirectories)
                .Where(p => p.Replace(Path.DirectorySeparatorChar, '/').Contains("/Features/"))
                .ToArray();

            if (matches.Length == 0)
                matches = Directory.GetFiles(repoRoot, filename, SearchOption.AllDirectories);

            if (matches.Length == 0)
                throw new FileNotFoundException($"Could not find file {filename} under project.");

            sourcePath = matches[0];
        }
        // Handle relative paths
        else if (!Path.IsPathRooted(sourcePath))
        {
            var candidate = Path.Combine(repoRoot, sourcePath);
            if (File.Exists(candidate))
            {
                sourcePath = candidate;
            }
            else
            {
                var filename = Path.GetFileName(sourcePath);
                // If the caller supplied a relative subpath, prefer matches that end with that subpath
                var relPathNormalized = sourcePath.Replace('\\', '/').TrimStart('/');
                var matches = Directory.GetFiles(repoRoot, filename, SearchOption.AllDirectories)
                    .Where(p => p.Replace(Path.DirectorySeparatorChar, '/').EndsWith(relPathNormalized))
                    .ToArray();

                if (matches.Length == 0)
                {
                    matches = Directory.GetFiles(repoRoot, filename, SearchOption.AllDirectories);
                }

                if (matches.Length == 0)
                {
                    throw new FileNotFoundException($"Could not find file {filename} under project.");
                }

                sourcePath = matches[0];
            }
        }

        // Read file content from disk
        var content = File.ReadAllBytes(sourcePath);

        string destinationName = name ?? string.Empty;
        // Normalize destination name for consistency across platforms
        destinationName = destinationName.Replace('\\', '/');

        // Determine entry name inside the archive
        // By default, place the destination file in the same relative directory inside the archive
        // as the source file is located (relative to repo root). This allows multiple modules to coexist.
        string entryName = destinationName;
        try
        {
            // If the caller provided a path with a directory component (e.g., "modules/index.js"),
            // preserve that directory inside the archive so the runtime can resolve entry points correctly.
            var normalizedPathParam = (path ?? string.Empty).Replace('\\', '/');
            if (normalizedPathParam.StartsWith("./")) normalizedPathParam = normalizedPathParam.Substring(2);
            normalizedPathParam = normalizedPathParam.TrimStart('/');
            string? paramDir = null;
            var slashPos = normalizedPathParam.LastIndexOf('/');
            if (slashPos >= 0)
                paramDir = normalizedPathParam.Substring(0, slashPos);

            if (!string.IsNullOrEmpty(paramDir))
            {
                entryName = paramDir.TrimEnd('/') + "/" + destinationName.TrimStart('/');
            }
            else
            {
                // Fallback: compute path relative to repo root so module files sit under a consistent directory.
                var relPath = Path.GetRelativePath(repoRoot, sourcePath).Replace('\\', '/');
                var dir = Path.GetDirectoryName(relPath)?.Replace('\\', '/');
                if (!string.IsNullOrEmpty(dir))
                    entryName = dir.TrimEnd('/') + "/" + destinationName.TrimStart('/');
            }
        }
        catch
        {
            /* fallback to destinationName */
        }

        // Create empty archive if it doesn't exist
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

        return this;
    }
}