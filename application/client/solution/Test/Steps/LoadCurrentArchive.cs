using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using NewGameProject.Runtime;

namespace NewGameProject.Tests.XUnit;

public partial class Steps {
    private List<string>? LogLines;
    public Steps ProcessArchive()
    {
        LogLines = new List<string>();
        RuntimeInterop.ClearLogger();
        RuntimeInterop.RegisterLogger(message => LogLines.Add(message));
        
        
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
            catch (IOException ex) when (ex.Message.Contains("being used by another process"))
            {
                // DLL is locked, but it should already exist from a previous test run
                // This is okay - just continue with the existing DLL
            }
            catch (Exception ex)
            {
                throw new Exception("Could not copy native DLL: " + ex.Message);
            }
        }

        // Clear previous state and process the archive into the Rust runtime
        RuntimeInterop.ClearState();
        var result = RuntimeInterop.ProcessArchive(_currentArchivePath);
        if (result == null)
            throw new InvalidOperationException("Failed to process archive: " + _currentArchivePath);

        return this;
    }

}
