using System;
using System.IO;

namespace NewGameProject.Tests.XUnit;

public partial class Steps
{
    private string? _dllDest;

    public Steps EnsureDllAccessible()
    {
        if (_dllDest != null && File.Exists(_dllDest))
            return this;

        var repoRoot = typeof(ModuleScenarios).Assembly.Location;
        var dllSource = Path.Combine(repoRoot, "libxml_xsd2.dll");
        _dllDest = Path.Combine(
            Path.GetDirectoryName(typeof(ModuleScenarios).Assembly.Location) ?? Directory.GetCurrentDirectory(),
            "libxml_xsd2.dll");

        if (File.Exists(dllSource))
        {
            try
            {
                if (!File.Exists(_dllDest) ||
                    File.GetLastWriteTimeUtc(dllSource) > File.GetLastWriteTimeUtc(_dllDest ?? string.Empty))
                    File.Copy(dllSource, _dllDest, true);
            }
            catch (Exception ex)
            {
                throw new Exception("Could not copy native DLL: " + ex.Message);
            }
        }

        return this;
    }
}