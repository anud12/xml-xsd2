using System.Runtime.InteropServices;
using System.Text.Json;

namespace NewGameProject.Runtime;

public static class EntityInterop
{
    private const string Library = "libxml_xsd2";
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr runtime_get_entity_snapshot([MarshalAs(UnmanagedType.LPUTF8Str)] string id);

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr runtime_get_entity_ids();

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    private static extern void runtime_free_string(IntPtr ptr);

    public static EntitySnapshot? GetEntityById(string id) => Read<EntitySnapshot>(runtime_get_entity_snapshot(id));

    public static string[] GetEntityIds() => Read<string[]>(runtime_get_entity_ids()) ?? [];

    private static T? Read<T>(IntPtr ptr) where T : class
    {
        if (ptr == IntPtr.Zero) return null;
        try { return JsonSerializer.Deserialize<T>(Marshal.PtrToStringUTF8(ptr)!, JsonOptions); }
        finally { runtime_free_string(ptr); }
    }
}

public sealed record EntitySnapshot(
    string Id,
    IReadOnlyDictionary<string, double> NumberMap,
    IReadOnlyDictionary<string, string> StringMap,
    IReadOnlyDictionary<string, double[][]> Polygons,
    IReadOnlyDictionary<string, string[]>? EntityListMap)
{
    public string[] Members(string list) => EntityListMap?.GetValueOrDefault(list) ?? [];
    public double X => Coordinate("xKey", "x");
    public double Y => Coordinate("yKey", "y");
    public double SpanX => NumberMap.GetValueOrDefault("spanX", 1.0);
    public double SpanY => NumberMap.GetValueOrDefault("spanY", 1.0);

    private double Coordinate(string key, string fallback)
    {
        var name = StringMap.GetValueOrDefault(key);
        return NumberMap.GetValueOrDefault(string.IsNullOrEmpty(name) ? fallback : name);
    }
}
