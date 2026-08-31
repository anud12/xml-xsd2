using System.Text.Json;
using Godot;
using NewGameProject.Runtime;

namespace NewGameProject.Tests.XUnit;

/// <summary>
/// Reads the NEW .ui node store exposed by the runtime
/// (<see cref="RuntimeInterop.FetchUiState"/>, one-shot JSON) and locates a
/// declared node by id. The legacy <c>registerPanel</c> Jint path is no
/// longer exercised by the migrated fixtures, so assertions target the
/// .ui options (width/height/x/y/anchor/background/children) instead of
/// the legacy <c>Runtime.Panel</c> struct.
/// </summary>
public static class UiStateReader
{
    /// <summary>Parses the UI state JSON; returns the raw node elements.</summary>
    public static JsonElement[] GetNodes()
    {
        for (int attempt = 0; attempt < 50; attempt++)
        {
            var csNodes = NewGameProject.Module.PanelNodeStore.Fetch();
            if (csNodes.Count > 0)
            {
                var list = new List<JsonElement>();
                foreach (var n in csNodes)
                {
                    var kindStr = n.Kind switch
                    {
                        NewGameProject.UI.UiNodeKind.Text => "text",
                        NewGameProject.UI.UiNodeKind.Field => "field",
                        NewGameProject.UI.UiNodeKind.Window => "window",
                        NewGameProject.UI.UiNodeKind.Image => "image",
                        NewGameProject.UI.UiNodeKind.Canvas => "canvas",
                        _ => "division"
                    };
                    var optionsEl = JsonDocument.Parse(n.OptionsJson).RootElement.Clone();
                    var payload = new Dictionary<string, object>
                    {
                        ["id"] = n.Id,
                        ["kind"] = kindStr,
                        ["value"] = n.Value,
                        ["src"] = n.Src,
                        ["options"] = optionsEl,
                        ["children"] = n.Children
                    };
                    var nodeJson = JsonSerializer.Serialize(payload);
                    using var nd = JsonDocument.Parse(nodeJson);
                    list.Add(nd.RootElement.Clone());
                }
                return list.ToArray();
            }
            var json = RuntimeInterop.FetchUiState();
            if (string.IsNullOrEmpty(json)) continue;
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("nodes", out var nodes)
                && nodes.ValueKind == JsonValueKind.Array
                && nodes.GetArrayLength() > 0)
            {
                var list = new List<JsonElement>();
                foreach (var el in nodes.EnumerateArray())
                    list.Add(el.Clone());
                return list.ToArray();
            }
            System.Threading.Thread.Sleep(50);
        }
        GD.PushError("UiStateReader: UI state did not populate after waiting");
        return Array.Empty<JsonElement>();
    }

    /// <summary>
    /// Finds the .ui node with the given id, or a JSON null element when
    /// the node is not declared.
    /// </summary>
    public static JsonElement GetNode(string id)
    {
        foreach (var node in GetNodes())
            if (node.ValueKind == JsonValueKind.Object
                && node.TryGetProperty("id", out var nid)
                && nid.GetString() == id)
                return node;
        return default;
    }

    /// <summary>
    /// The node's <c>options</c> object (or null when absent). For
    /// <c>field</c> nodes the binding object (<c>binding</c>) is returned,
    /// since it carries the field's entity/map/name/fallback.
    /// </summary>
    public static JsonElement? GetOptions(string id)
    {
        var node = GetNode(id);
        if (node.ValueKind != JsonValueKind.Object)
        {
            System.IO.File.AppendAllText(@"C:\Users\acriha\AppData\Local\Temp\opencode\dbg-anim.log", $"[READER] {id} node not object: {node.ValueKind}\n");
            return null;
        }
        if (node.TryGetProperty("binding", out var b) && b.ValueKind == JsonValueKind.Object)
            return b;
        if (node.TryGetProperty("options", out var o) && o.ValueKind == JsonValueKind.Object)
            return o;
        System.IO.File.AppendAllText(@"C:\Users\acriha\AppData\Local\Temp\opencode\dbg-anim.log", $"[READER] {id} node raw={node.GetRawText()}\n");
        return null;
    }

    /// <summary>Optional number in the node's options (e.g. width, x).</summary>
    public static double? GetNumberOption(string id, string prop)
    {
        var o = GetOptions(id);
        return o.HasValue
            && o.Value.TryGetProperty(prop, out var v)
            && v.ValueKind == JsonValueKind.Number
            ? v.GetDouble()
            : null;
    }

    /// <summary>Optional string in the node's options (e.g. anchor).</summary>
    public static string? GetStringOption(string id, string prop)
    {
        var o = GetOptions(id);
        return o.HasValue
            && o.Value.TryGetProperty(prop, out var v)
            && v.ValueKind == JsonValueKind.String
            ? v.GetString()
            : null;
    }

    /// <summary>The node's <c>options.background</c> element (or null).</summary>
    public static JsonElement? GetBackground(string id)
    {
        var o = GetOptions(id);
        return o.HasValue && o.Value.TryGetProperty("background", out var b)
            ? b
            : null;
    }

    /// <summary>The node's <c>options.onHover</c> element (or null).</summary>
    public static JsonElement? GetHover(string id)
    {
        var o = GetOptions(id);
        return o.HasValue && o.Value.TryGetProperty("onHover", out var h)
            ? h
            : null;
    }

    /// <summary>The declared child ids of the node (empty when absent).</summary>
    public static List<string> GetChildren(string id)
    {
        var result = new List<string>();
        var node = GetNode(id);
        if (node.ValueKind != JsonValueKind.Object
            || !node.TryGetProperty("children", out var c)
            || c.ValueKind != JsonValueKind.Array)
            return result;
        foreach (var ch in c.EnumerateArray())
            result.Add(ch.GetString() ?? "");
        return result;
    }

    /// <summary>The node's <c>kind</c> string (e.g. "window", "field").</summary>
    public static string GetKind(string id)
    {
        var node = GetNode(id);
        return node.ValueKind != JsonValueKind.Object
            ? ""
            : node.TryGetProperty("kind", out var k) ? k.GetString() ?? "" : "";
    }
}
