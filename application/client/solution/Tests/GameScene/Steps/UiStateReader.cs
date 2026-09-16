using System.Text.Json;
using Godot;
using NewGameProject.Runtime;
using NewGameProject.UI;

namespace NewGameProject.Tests.XUnit;

/// <summary>
/// Reads the .ui node store exposed by the runtime
/// (<see cref="RuntimeInterop.FetchUiState"/>, binary slab) and locates a
/// declared node by id. Assertions target the .ui options
/// (width/height/x/y/anchor/background/children).
/// </summary>
public static class UiStateReader
{
    static string KindStr(NewGameProject.UI.UiNodeKind kind) => kind switch
    {
        NewGameProject.UI.UiNodeKind.Text => "text",
        NewGameProject.UI.UiNodeKind.Field => "field",
        NewGameProject.UI.UiNodeKind.Window => "window",
        NewGameProject.UI.UiNodeKind.Image => "image",
        _ => "division"
    };

    /// <summary>Reads the UI node store (Rust slab FFI); returns the raw node elements.</summary>
    public static JsonElement[] GetNodes()
    {
        for (int attempt = 0; attempt < 50; attempt++)
        {
            var ptr = RuntimeInterop.FetchUiState();
            if (ptr != IntPtr.Zero)
            {
                var slabNodes = UiSlab.ReadNodes(ptr);
                RuntimeInterop.FreeUiState(ptr);
                if (slabNodes.Count > 0)
                {
                    var list = new List<JsonElement>();
                    foreach (var n in slabNodes)
                    {
                        var payload = new Dictionary<string, object>
                        {
                            ["id"] = n.Id,
                            ["kind"] = KindStr(n.Kind),
                            ["value"] = n.Value,
                            ["src"] = n.Src,
                            ["options"] = JsonDocument.Parse(n.OptionsJson).RootElement.Clone(),
                            ["children"] = n.Children
                        };
                        if (!string.IsNullOrEmpty(n.BindingJson))
                            payload["binding"] = JsonDocument.Parse(n.BindingJson).RootElement.Clone();
                        list.Add(JsonDocument.Parse(
                            JsonSerializer.Serialize(payload)).RootElement.Clone());
                    }
                    return list.ToArray();
                }
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
        if (node.ValueKind != JsonValueKind.Object) return null;
        if (node.TryGetProperty("binding", out var b) && b.ValueKind == JsonValueKind.Object)
            return b;
        if (node.TryGetProperty("options", out var o) && o.ValueKind == JsonValueKind.Object)
            return o;
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
