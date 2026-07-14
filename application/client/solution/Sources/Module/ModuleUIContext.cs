namespace NewGameProject.Module;

public class ModuleUIContext
{
    readonly Dictionary<string, byte[]> _archiveFiles = new();
    readonly Dictionary<string, Runtime.Panel> _panels = new();

    public void ProcessArchive(string zipPath)
    {
        _archiveFiles.Clear();
        _panels.Clear();
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

    public Dictionary<string, byte[]> GetArchiveFiles()
        => new(_archiveFiles);

    void ExecuteModules()
    {
        var modules = _archiveFiles
            .Where(kv => kv.Key.EndsWith("index.js"))
            .Select(kv => kv.Value).ToList();

        foreach (var bytes in modules)
            ExecuteModuleSource(System.Text.Encoding.UTF8.GetString(bytes));
    }

    void ExecuteModuleSource(string moduleJs)
    {
        var panels = ModuleEngine.ExecuteModule(moduleJs);
        foreach (var p in panels)
            if (!string.IsNullOrEmpty(p.Id))
                _panels[p.Id] = p;
    }
}
