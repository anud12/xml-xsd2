namespace NewGameProject.Module;

public class ModuleUIContext
{
    readonly Dictionary<string, byte[]> _archiveFiles = new();
    readonly Dictionary<string, Runtime.Panel> _panels = new();

    public void ProcessArchive(string zipPath)
    {
        _archiveFiles.Clear();
        _panels.Clear();
        EffectStore.Clear();
        BehaviorStore.Clear();
        ArchiveReader.Extract(zipPath, _archiveFiles);
        ExecuteModules();
    }

    public void LoadModule(string modulePath)
    {
        ExecuteModuleSource(File.ReadAllText(modulePath));
    }

    public void LoadModuleSource(string moduleJs) => ExecuteModuleSource(moduleJs);

    public string[] GetPanelIds() => _panels.Keys.ToArray();

    public Runtime.Panel GetPanelById(string id)
        => _panels.TryGetValue(id, out var p) ? p : default;

    public Runtime.Panel[] GetAllPanels() => _panels.Values.ToArray();

    public void UpdatePanelBackground(string id, string background)
    {
        if (_panels.TryGetValue(id, out var p))
        {
            p.Background = background;
            _panels[id] = p;
        }
    }

    public Dictionary<string, byte[]> GetArchiveFiles()
        => new(_archiveFiles);

    void ExecuteModules()
    {
        var modules = _archiveFiles
            .Where(kv => kv.Key.EndsWith("index.js"))
            .Select(kv => kv.Value).ToList();
        if (modules.Count == 0) return;

        ModuleEngine.ArchiveFileSet = _archiveFiles.Keys.ToHashSet();

        // Every index.js in the archive is executed through the same engine
        // (shared collector) so panels from all modules are kept; the first
        // entry is the main entry from the manifest.
        var main = "index.js";
        if (_archiveFiles.TryGetValue("manifest.json", out var manifestBytes))
        {
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(
                    System.Text.Encoding.UTF8.GetString(manifestBytes));
                if (doc.RootElement.TryGetProperty("entry", out var entry)
                    && entry.ValueKind == System.Text.Json.JsonValueKind.String
                    && _archiveFiles.ContainsKey(entry.GetString()))
                    main = entry.GetString();
            }
            catch { }
        }

        var ordered = modules
            .OrderBy(m => _archiveFiles.First(kv => kv.Value.SequenceEqual(m)).Key == main ? 0 : 1)
            .ToList();

        var first = true;
        foreach (var bytes in ordered)
        {
            var panels = ModuleEngine.ExecuteModule(
                System.Text.Encoding.UTF8.GetString(bytes),
                clearCollector: first);
            first = false;
            foreach (var p in panels)
                if (!string.IsNullOrEmpty(p.Id))
                    _panels[p.Id] = p;
        }

        PanelNodeStore.RegisterAll(_panels.Values.ToArray(), _archiveFiles);
    }

    Runtime.Panel[] ExecuteModuleSource(string moduleJs)
    {
        var panels = ModuleEngine.ExecuteModule(moduleJs, clearCollector: false);
        foreach (var p in panels)
            if (!string.IsNullOrEmpty(p.Id))
                _panels[p.Id] = p;
        PanelNodeStore.RegisterAll(_panels.Values.ToArray(), _archiveFiles);
        return panels;
    }
}
