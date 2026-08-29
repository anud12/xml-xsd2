using System.Text.Json;

namespace NewGameProject.Module;

static class PanelCollector
{
    static readonly List<string> _panels = new();

    public static void Clear() => _panels.Clear();

    public static void Register(string panelJson) => _panels.Add(panelJson);

    public static int Count => _panels.Count;

    public static Runtime.Panel[] ToPanels()
    {
        var result = new List<Runtime.Panel>();
        foreach (var json in _panels)
        {
            if (PanelParser.TryParse(json, out var panel))
                result.Add(panel);
        }
        LinkChildIds(result);
        return result.ToArray();
    }

    /// Resolves id-referenced children to the panels registered in the
    /// module (hostApi.ui.window children are ids, not nested objects).
    /// Children may be registered after their parent (the JS shim registers
    /// children before the parent), so ids are resolved by id and nested
    /// levels are iterated to a fixpoint.
    static void LinkChildIds(List<Runtime.Panel> panels)
    {
        // Resolve every id to the index of its authoritative (fully linked)
        // entry; the entry itself may still gain children, so re-scan.
        var byId = new Dictionary<string, int>();
        for (int i = 0; i < panels.Count; i++)
            if (!string.IsNullOrEmpty(panels[i].Id))
                byId[panels[i].Id] = i;

        // Link each parent's children; ids resolve regardless of order.
        for (int i = 0; i < panels.Count; i++)
        {
            var p = panels[i];
            if (p.ChildIds == null || p.ChildIds.Length == 0) continue;
            var linked = new List<Runtime.Panel>(
                p.Children ?? Array.Empty<Runtime.Panel>());
            foreach (var id in p.ChildIds)
            {
                if (!byId.TryGetValue(id, out var j)) continue;
                if (linked.Any(c => c.Id == id)) continue;
                linked.Add(panels[j]);
            }
            p.Children = linked.ToArray();
            p.ChildIds = null;
            panels[i] = p;
        }
    }
}
