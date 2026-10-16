using System.IO;
using NewGameProject.Runtime;

namespace NewGameProject.Tests.XUnit;

public partial class Steps {
    /// Re-processes the current archive through the hot reload FFI. Unlike
    /// <see cref="ProcessArchive"/> it does NOT clear state first: the
    /// previous game state (entity values, elapsed time, in-flight action
    /// plans) is kept and the module's new declarations take effect.
    public Steps ReloadCurrentArchive()
    {
        if (string.IsNullOrEmpty(_currentArchivePath) || !File.Exists(_currentArchivePath))
            throw new InvalidOperationException("No files have been added to archive yet. Use AddFileToArchive() first.");

        var result = RuntimeInterop.ReloadArchive(_currentArchivePath);
        if (result == null)
            throw new InvalidOperationException("Failed to hot reload archive: " + _currentArchivePath);

        return this;
    }
}
