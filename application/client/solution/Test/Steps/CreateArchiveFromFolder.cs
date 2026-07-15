using System;
using System.IO;
using System.IO.Compression;

namespace NewGameProject.Tests.XUnit;

public partial class Steps
{
    public string CreateArchiveFromFolder(string folderName)
    {
        var realPath = @"E:\workspace\xml-xsd2\application\client\Features\step1\add_two_panels\" + folderName;
        var prefix = folderName.ToLower();

        using (var fs = new FileStream(Path.Combine(Path.GetTempPath(), $"test_{prefix}_{Guid.NewGuid()}.zip"),
                   FileMode.Create))
        using (var archive = new ZipArchive(fs, ZipArchiveMode.Create))
        {
            foreach (var fileName in Directory.GetFiles(realPath, "*.*", SearchOption.AllDirectories))
            {
                var entryName = prefix + "/" + Path.GetRelativePath(realPath, fileName).Replace('\\', '/');

                var entry = archive.CreateEntry(entryName);
                using (var es = entry.Open())
                {
                    using (var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
                        stream.CopyTo(es);
                }
            }
        }

        return Path.Combine(Path.GetTempPath(), $"test_{prefix}_{Guid.NewGuid()}.zip");
    }

    
}