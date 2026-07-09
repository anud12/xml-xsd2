using System.Runtime.InteropServices;
using System.Text.Json;

namespace NewGameProject.Runtime;

public static class ContainerInterop
{
    private const string LIB_NAME = "libxml_xsd2";

    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr get_container_ids();

    public static string[] GetContainerIds()
    {
        var ptr = get_container_ids();
        if (ptr == IntPtr.Zero) return Array.Empty<string>();
        var result = new List<string>();
        int offset = 0;
        while (true)
        {
            IntPtr strPtr = Marshal.ReadIntPtr(ptr, offset);
            if (strPtr == IntPtr.Zero) break;
            var s = Marshal.PtrToStringAnsi(strPtr);
            result.Add(s ?? string.Empty);
            offset += IntPtr.Size;
        }
        return result.ToArray();
    }

    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr get_container_by_id([MarshalAs(UnmanagedType.LPStr)] string id);

    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
    private static extern void runtime_free_container(IntPtr p);

    public static Container GetContainerById(string id)
    {
        IntPtr ptr = get_container_by_id(id);
        if (ptr == IntPtr.Zero) return default;
        try
        {
            string json = Marshal.PtrToStringAnsi(ptr) ?? string.Empty;
            return ParseContainer(json);
        }
        finally
        {
            runtime_free_container(ptr);
        }
    }

    private static Container ParseContainer(string json)
    {
        if (string.IsNullOrEmpty(json)) return default;

        using var doc = System.Text.Json.JsonDocument.Parse(json);
        var root = doc.RootElement;

        var container = new Container
        {
            Id = root.TryGetProperty("id", out var idProp) ? idProp.GetString() ?? string.Empty : string.Empty,
            Entities = ParseEntityArray(root),
            GetXForEntityId = ParseEntityNumberMap(root, "getX"),
            GetYForEntityId = ParseEntityNumberMap(root, "getY"),
            GetSpanXForEntityId = ParseEntityNumberMap(root, "getSpanX"),
            GetSpanYForEntityId = ParseEntityNumberMap(root, "getSpanY"),
        };

        if (root.TryGetProperty("textMap", out var textMapProp) && textMapProp.ValueKind == System.Text.Json.JsonValueKind.Object)
        {
            container.TextMap = ParseStringMap(textMapProp);
        }

        if (root.TryGetProperty("numberMap", out var numberMapProp) && numberMapProp.ValueKind == System.Text.Json.JsonValueKind.Object)
        {
            container.NumberMap = ParseNumberMap(numberMapProp);
        }

        if (root.TryGetProperty("sizeX", out var sizeXProp) && sizeXProp.ValueKind == System.Text.Json.JsonValueKind.Object)
        {
            container.SizeX = ParseAxisSize(sizeXProp);
        }

        if (root.TryGetProperty("sizeY", out var sizeYProp) && sizeYProp.ValueKind == System.Text.Json.JsonValueKind.Object)
        {
            container.SizeY = ParseAxisSize(sizeYProp);
        }

        return container;
    }

    private static string[] ParseEntityArray(JsonElement root)
    {
        if (!root.TryGetProperty("entities", out var entitiesProp))
            return Array.Empty<string>();

        if (entitiesProp.ValueKind != System.Text.Json.JsonValueKind.Object)
            return Array.Empty<string>();

        if (!entitiesProp.TryGetProperty("entity", out var entityArrayProp))
            return Array.Empty<string>();

        if (entityArrayProp.ValueKind != System.Text.Json.JsonValueKind.Array)
            return Array.Empty<string>();

        var result = new List<string>();
        foreach (var elem in entityArrayProp.EnumerateArray())
        {
            var entityId = elem.GetString();
            if (entityId != null)
                result.Add(entityId);
        }
        return result.ToArray();
    }

    private static Dictionary<string, string>? ParseStringMap(JsonElement element)
    {
        var map = new Dictionary<string, string>();
        foreach (var prop in element.EnumerateObject())
        {
            var value = prop.Value.GetString();
            if (value != null)
                map[prop.Name] = value;
        }
        return map.Count > 0 ? map : null;
    }

    private static Dictionary<string, double>? ParseNumberMap(JsonElement element)
    {
        var map = new Dictionary<string, double>();
        foreach (var prop in element.EnumerateObject())
        {
            map[prop.Name] = prop.Value.GetDouble();
        }
        return map.Count > 0 ? map : null;
    }

    private static Dictionary<string, double>? ParseEntityNumberMap(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var prop))
            return null;

        if (prop.ValueKind != System.Text.Json.JsonValueKind.Object)
            return null;

        return ParseNumberMap(prop);
    }

    private static AxisSize ParseAxisSize(JsonElement element)
    {
        return new AxisSize
        {
            Value = element.TryGetProperty("value", out var valueProp) ? valueProp.GetDouble() : 0,
            OutOfBounds = ParseOutOfBoundsRule(element)
        };
    }

    private static OutOfBoundsRule ParseOutOfBoundsRule(JsonElement element)
    {
        if (!element.TryGetProperty("outOfBounds", out var ruleProp))
            return OutOfBoundsRule.Unbound;

        var rule = ruleProp.GetString() ?? "unbound";
        return rule switch
        {
            "clamp" => OutOfBoundsRule.Clamp,
            "wrap" => OutOfBoundsRule.Wrap,
            _ => OutOfBoundsRule.Unbound
        };
    }

    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr get_container_text_map_value(
        [MarshalAs(UnmanagedType.LPStr)] string containerId,
        [MarshalAs(UnmanagedType.LPStr)] string key);

    public static string GetContainerTextMapValue(string containerId, string key)
    {
        var ptr = get_container_text_map_value(containerId, key);
        if (ptr == IntPtr.Zero) return string.Empty;
        try
        {
            var s = Marshal.PtrToStringAnsi(ptr) ?? string.Empty;
            return new string(s.Where(c => !char.IsControl(c) || c == '\r' || c == '\n' || c == '\t').ToArray());
        }
        finally
        {
            runtime_free_string(ptr);
        }
    }

    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr get_container_number_map_value(
        [MarshalAs(UnmanagedType.LPStr)] string containerId,
        [MarshalAs(UnmanagedType.LPStr)] string key);

    public static string GetContainerNumberMapValue(string containerId, string key)
    {
        var ptr = get_container_number_map_value(containerId, key);
        if (ptr == IntPtr.Zero) return string.Empty;
        try
        {
            var s = Marshal.PtrToStringAnsi(ptr) ?? string.Empty;
            return new string(s.Where(c => !char.IsControl(c) || c == '\r' || c == '\n' || c == '\t').ToArray());
        }
        finally
        {
            runtime_free_string(ptr);
        }
    }

    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr get_container_entity_at(
        [MarshalAs(UnmanagedType.LPStr)] string containerId,
        double x,
        double y);

    public static string? GetContainerEntityAt(string containerId, double x, double y)
    {
        var ptr = get_container_entity_at(containerId, x, y);
        if (ptr == IntPtr.Zero) return null;
        try
        {
            return Marshal.PtrToStringAnsi(ptr) ?? null;
        }
        finally
        {
            runtime_free_string(ptr);
        }
    }

    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr get_container_entities_json(
        [MarshalAs(UnmanagedType.LPStr)] string containerId);

    public static string[]? GetContainerEntities(string containerId)
    {
        var ptr = get_container_entities_json(containerId);
        if (ptr == IntPtr.Zero) return null;
        try
        {
            var json = Marshal.PtrToStringAnsi(ptr) ?? string.Empty;
            if (string.IsNullOrEmpty(json)) return Array.Empty<string>();

            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var result = new List<string>();
            foreach (var elem in doc.RootElement.EnumerateArray())
            {
                var entityId = elem.GetString() ?? string.Empty;
                result.Add(entityId);
            }
            return result.ToArray();
        }
        finally
        {
            runtime_free_string(ptr);
        }
    }

    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
    private static extern void runtime_free_string(IntPtr s);
}
