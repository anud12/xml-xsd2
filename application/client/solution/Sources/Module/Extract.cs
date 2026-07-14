using System.Text.Json;

namespace NewGameProject.Module;

static class Extract
{
    internal static string? String(JsonElement elem, string prop)
    {
        if (elem.TryGetProperty(prop, out var p) && p.ValueKind == JsonValueKind.String)
            return p.GetString();

        if (elem.TryGetProperty(prop, out p) && p.ValueKind == JsonValueKind.Object
            && p.TryGetProperty("value", out var inner))
            return inner.GetString();

        return null;
    }

    internal static float? Float(JsonElement elem, string prop)
    {
        if (elem.TryGetProperty(prop, out var p) && p.ValueKind == JsonValueKind.Number)
            return p.GetSingle();

        if (elem.TryGetProperty(prop, out p) && p.ValueKind == JsonValueKind.Object
            && p.TryGetProperty("value", out var inner) && inner.ValueKind == JsonValueKind.Number)
            return inner.GetSingle();

        return null;
    }

    internal static double? Double(JsonElement elem, string prop)
    {
        if (elem.TryGetProperty(prop, out var p) && p.ValueKind == JsonValueKind.Number)
            return p.GetDouble();

        if (elem.TryGetProperty(prop, out p) && p.ValueKind == JsonValueKind.Object
            && p.TryGetProperty("value", out var inner) && inner.ValueKind == JsonValueKind.Number)
            return inner.GetDouble();

        return null;
    }

    internal static int? Int(JsonElement elem, string prop)
    {
        if (elem.TryGetProperty(prop, out var p) && p.ValueKind == JsonValueKind.Number)
            return p.GetInt32();

        if (elem.TryGetProperty(prop, out p) && p.ValueKind == JsonValueKind.Object
            && p.TryGetProperty("value", out var inner) && inner.ValueKind == JsonValueKind.Number)
            return inner.GetInt32();

        return null;
    }
}
