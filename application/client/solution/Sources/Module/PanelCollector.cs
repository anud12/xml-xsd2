using System.Text.Json;

namespace NewGameProject.Module;

static class PanelCollector
{
    static readonly List<string> _panels = new();

    public static void Clear() => _panels.Clear();

    public static void Register(string panelJson) => _panels.Add(panelJson);

    public static Runtime.Panel[] ToPanels()
    {
        var result = new List<Runtime.Panel>();
        foreach (var json in _panels)
        {
            if (PanelParser.TryParse(json, out var panel))
                result.Add(panel);
        }
        return result.ToArray();
    }
}
