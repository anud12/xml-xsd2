using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;

namespace NewGameProject.Tests.XUnit;

public partial class Steps
{
    public Steps ProcessArchive()
    {
        if (string.IsNullOrEmpty(_currentArchivePath) || !File.Exists(_currentArchivePath))
            throw new InvalidOperationException("No files have been added to archive yet. Use AddFileToArchive() first.");

        var repoRoot = FindRepoRoot();
        var dllSource = Path.Combine(repoRoot, "libxml_xsd2.dll");
        var asmDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? repoRoot;
        var dllDest = Path.Combine(asmDir, "libxml_xsd2.dll");

        // Copy native DLL to output directory if needed
        if (File.Exists(dllSource))
        {
            try
            {
                if (!File.Exists(dllDest) ||
                    File.GetLastWriteTimeUtc(dllSource) > File.GetLastWriteTimeUtc(dllDest))
                    File.Copy(dllSource, dllDest, true);
            }
            catch (Exception ex)
            {
                throw new Exception("Could not copy native DLL: " + ex.Message);
            }
        }

        // Process the archive through the runtime interop
        var dbPath = RuntimeInterop.ProcessArchive(_currentArchivePath);
        return this;
    }

}
