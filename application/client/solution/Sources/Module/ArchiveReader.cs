using System.IO.Compression;

namespace NewGameProject.Module;

static class ArchiveReader
{
    internal static void Extract(string zipPath, Dictionary<string, byte[]> files)
    {
        using var archive = ZipFile.OpenRead(zipPath);
        foreach (var entry in archive.Entries)
        {
            if (string.IsNullOrEmpty(entry.Name)) continue;

            using var stream = entry.Open();
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            files[entry.FullName] = ms.ToArray();
        }
    }
}
