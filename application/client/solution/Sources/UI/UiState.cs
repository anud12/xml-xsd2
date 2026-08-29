using System.Runtime.InteropServices;
using System.Text.Json;
using NewGameProject.Runtime;

namespace NewGameProject.UI;

public enum UiNodeKind
{
    Division,
    Text,
    Field,
    Window,
    Image,
    Canvas
}

public class UiNodeData
{
    public string Id { get; set; } = "";
    public UiNodeKind Kind { get; set; }
    public string Value { get; set; } = "";
    public string OptionsJson { get; set; } = "{}";
    public List<string> Children { get; set; } = new();
    /// Archive path for image nodes (empty for all other kinds).
    public string Src { get; set; } = "";

    public override bool Equals(object? obj)
    {
        if (obj is not UiNodeData o) return false;
        return o.Id == Id && o.Kind == Kind && o.Value == Value
            && o.OptionsJson == OptionsJson && o.Src == Src
            && o.Children.Count == Children.Count
            && o.Children.SequenceEqual(Children);
    }
}

public class UiDeltaOp
{
    public string Op { get; set; } = "";
    public UiNodeData Node { get; set; } = null!;
    public string Id { get; set; } = "";
}

public class UiDelta
{
    public List<UiDeltaOp> Ops { get; set; } = new();
}

public static class UiState
{
    static UiNodeKind ParseKind(string? s) => s switch
    {
        "text" => UiNodeKind.Text,
        "field" => UiNodeKind.Field,
        "window" => UiNodeKind.Window,
        "image" => UiNodeKind.Image,
        "canvas" => UiNodeKind.Canvas,
        _ => UiNodeKind.Division
    };

    public static List<UiNodeData> ParseNodes(string json)
    {
        var doc = System.Text.Json.JsonDocument.Parse(json);
        var list = new List<UiNodeData>();
        foreach (var el in doc.RootElement.GetProperty("nodes").EnumerateArray())
            list.Add(ParseNode(el));
        return list;
    }

    static UiNodeData ParseNode(System.Text.Json.JsonElement el)
    {
        var node = new UiNodeData
        {
            Id = el.GetProperty("id").GetString() ?? "",
            Kind = ParseKind(el.TryGetProperty("kind", out var k) ? k.GetString() : null),
        };
        if (el.TryGetProperty("value", out var v)) node.Value = v.GetString() ?? "";
        if (el.TryGetProperty("src", out var s)) node.Src = s.GetString() ?? "";
        if (el.TryGetProperty("options", out var o) && o.ValueKind == System.Text.Json.JsonValueKind.Object)
            node.OptionsJson = o.GetRawText();
        if (el.TryGetProperty("children", out var c))
            foreach (var ch in c.EnumerateArray())
                node.Children.Add(ch.GetString() ?? "");
        return node;
    }

    public static UiDelta? ParseDelta(string? json)
    {
        if (string.IsNullOrEmpty(json)) return null;
        var doc = System.Text.Json.JsonDocument.Parse(json);
        var delta = new UiDelta();
        foreach (var el in doc.RootElement.GetProperty("ops").EnumerateArray())
        {
            var op = new UiDeltaOp { Op = el.GetProperty("op").GetString() ?? "" };
            if (el.TryGetProperty("node", out var n)) op.Node = ParseNode(n);
            if (el.TryGetProperty("id", out var id)) op.Id = id.GetString() ?? "";
            delta.Ops.Add(op);
        }
        return delta;
    }

    public static List<UiNodeData> FetchState()
    {
        var csNodes = NewGameProject.Module.PanelNodeStore.Fetch();
        if (csNodes.Count > 0) return csNodes;
        var json = RuntimeInterop.FetchUiState();
        if (string.IsNullOrEmpty(json)) return new List<UiNodeData>();
        return ParseNodes(json);
    }

    public static UiDelta? FetchDelta()
    {
        var json = RuntimeInterop.FetchUiDelta();
        return ParseDelta(json);
    }

    /// The registered animation definition for the given name (from
    /// <c>runtime_fetch_ui_animations</c>), or null when unregistered.
    public static System.Text.Json.JsonElement? GetAnimation(string name)
    {
        var csJson = NewGameProject.Module.PanelNodeStore.GetAnimationJson(name);
        if (!string.IsNullOrEmpty(csJson))
            return JsonDocument.Parse(csJson).RootElement.Clone();
        var json = RuntimeInterop.FetchUiAnimations();
        if (string.IsNullOrEmpty(json)) return null;
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        return doc.RootElement.TryGetProperty(name, out var def)
            ? def.Clone()
            : null;
    }
}
