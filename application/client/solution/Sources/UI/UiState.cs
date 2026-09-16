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
    Image
}

public class UiNodeData
{
    public string Id { get; set; } = "";
    public UiNodeKind Kind { get; set; }
    public string Value { get; set; } = "";
    public string OptionsJson { get; set; } = "{}";
    /// Field binding as a JSON object ({entity,map,name,fallback}) for nodes
    /// read through the Rust slab FFI; empty for non-field nodes.
    public string BindingJson { get; set; } = "";
    public List<string> Children { get; set; } = new();
    /// Archive path for image nodes (empty for all other kinds).
    public string Src { get; set; } = "";

    public override bool Equals(object? obj)
    {
        if (obj is not UiNodeData o) return false;
        return o.Id == Id && o.Kind == Kind && o.Value == Value
            && o.OptionsJson == OptionsJson && o.Src == Src
            && o.BindingJson == BindingJson
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
    public static List<UiNodeData> FetchState()
    {
        var ptr = RuntimeInterop.FetchUiState();
        if (ptr == IntPtr.Zero) return new List<UiNodeData>();
        try { return UiSlab.ReadNodes(ptr); }
        finally { RuntimeInterop.FreeUiState(ptr); }
    }

    public static UiDelta? FetchDelta()
    {
        var ptr = RuntimeInterop.FetchUiDelta();
        if (ptr == IntPtr.Zero) return null;
        try { return UiSlab.ReadDelta(ptr); }
        finally { RuntimeInterop.FreeUiDelta(ptr); }
    }

    /// The registered animation definition for the given name (from
    /// <c>runtime_fetch_ui_animations</c>), or null when unregistered.
    public static System.Text.Json.JsonElement? GetAnimation(string name)
    {
        var json = RuntimeInterop.FetchUiAnimations();
        if (string.IsNullOrEmpty(json)) return null;
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        return doc.RootElement.TryGetProperty(name, out var def)
            ? def.Clone()
            : null;
    }
}
