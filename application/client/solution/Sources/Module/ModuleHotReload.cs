using System.IO;
using System.IO.Compression;
using System.Threading;

namespace NewGameProject.Module;

/// One file of a module directory: relative name + identity of its contents
/// (length + last write time), enough to decide whether the module changed.
public sealed record ModuleFile(string Name, long Length, DateTime LastWriteUtc);

/// Pure helpers to ship a module directory as an archive and to detect that
/// the module changed (the inputs of a hot reload).
public static class ModuleArchive
{
    /// Zips every file of the module directory into a new archive (entries are
    /// the files' paths relative to the directory, forward-slash separated).
    public static string CreateZipFromDir(string dir, string? zipPath = null)
    {
        zipPath ??= Path.Combine(Path.GetTempPath(), $"mod_{Guid.NewGuid()}.zip");
        using var fs = new FileStream(zipPath, FileMode.Create);
        using var zip = new ZipArchive(fs, ZipArchiveMode.Create);
        foreach (var file in Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories))
        {
            string name = Path.GetRelativePath(dir, file).Replace('\\', '/');
            var entry = zip.CreateEntry(name);
            using var es = entry.Open();
            using var src = File.OpenRead(file);
            src.CopyTo(es);
        }
        return zipPath;
    }

    /// The module's files ordered by relative name.
    public static List<ModuleFile> Snapshot(string dir)
    {
        var list = new List<ModuleFile>();
        if (!Directory.Exists(dir)) return list;
        foreach (var file in Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories)
                     .OrderBy(p => Path.GetRelativePath(dir, p), StringComparer.Ordinal))
        {
            list.Add(new ModuleFile(
                Path.GetRelativePath(dir, file).Replace('\\', '/'),
                new FileInfo(file).Length,
                File.GetLastWriteTimeUtc(file)));
        }
        return list;
    }

    /// True when the two snapshots differ: a file was added, removed, resized
    /// or rewritten.
    public static bool HasChanged(IReadOnlyList<ModuleFile> before, IReadOnlyList<ModuleFile> after)
    {
        if (before.Count != after.Count) return true;
        for (int i = 0; i < before.Count; i++)
        {
            if (before[i].Name != after[i].Name ||
                before[i].Length != after[i].Length ||
                before[i].LastWriteUtc != after[i].LastWriteUtc)
                return true;
        }
        return false;
    }
}

/// Watches a module directory and invokes <paramref name="reload"/> whenever
/// the module changed. File events only raise a flag; a slow poll compares
/// snapshots and fires the reload once the changes settle (debounce), so a
/// burst of saves produces a single reload.
public sealed class ModuleWatcher : IDisposable
{
    readonly string _dir;
    readonly Action _reload;
    readonly int _pollMs;
    FileSystemWatcher? _watcher;
    Timer? _timer;
    List<ModuleFile> _baseline;
    int _pending;

    public ModuleWatcher(string dir, Action reload, int pollMs = 250)
    {
        _dir = dir;
        _reload = reload;
        _pollMs = pollMs;
        _baseline = ModuleArchive.Snapshot(dir);
    }

    public void Start()
    {
        if (!Directory.Exists(_dir) || _watcher != null) return;
        _watcher = new FileSystemWatcher(_dir)
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite |
                           NotifyFilters.Size | NotifyFilters.CreationTime,
        };
        void Flag(object sender, FileSystemEventArgs e) => MarkChanged();
        _watcher.Changed += Flag;
        _watcher.Created += Flag;
        _watcher.Deleted += Flag;
        _watcher.Renamed += Flag;
        _watcher.EnableRaisingEvents = true;
        _timer = new Timer(_ => Poll(), null, _pollMs, _pollMs);
    }

    void MarkChanged() => Interlocked.Exchange(ref _pending, 1);

    void Poll()
    {
        if (Interlocked.CompareExchange(ref _pending, 0, 1) != 1) return;
        var current = ModuleArchive.Snapshot(_dir);
        if (!ModuleArchive.HasChanged(_baseline, current)) return;
        _baseline = current;
        _reload();
    }

    public void Stop()
    {
        _timer?.Dispose();
        _timer = null;
        if (_watcher != null)
        {
            _watcher.EnableRaisingEvents = false;
            _watcher.Dispose();
            _watcher = null;
        }
    }

    public void Dispose() => Stop();
}
