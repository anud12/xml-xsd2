using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Linq;
using NewGameProject.Module;

namespace NewGameProject.Runtime;

public static class RuntimeInterop
{
    private const string LIB_NAME = "libxml_xsd2";
    public static string ZIP_PATH = "";

    public static string ZipPath => ZIP_PATH;

    public static string[] GetPanelIds()
    {
        return ModuleContextProvider.Context.GetPanelIds();
    }

    public static Panel GetPanelById(string id)
    {
        return ModuleContextProvider.Context.GetPanelById(id);
    }

    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr runtime_process_archive([MarshalAs(UnmanagedType.LPStr)] string path);

    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
    private static extern void runtime_free_string(IntPtr s);

    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
    private static extern bool runtime_export_state([MarshalAs(UnmanagedType.LPStr)] string path);

    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
    private static extern void runtime_clear_state();

    public static void ClearState() => runtime_clear_state();

    [Obsolete("Kept for backward compatibility")]
    public static void TestFixedStructMarshaling() { }

    public static string ProcessArchive(string zipPath)
    {
        ZIP_PATH = zipPath;
        IntPtr ptr = runtime_process_archive(zipPath);
        if (ptr == IntPtr.Zero) return null;
        try
        {
            return Marshal.PtrToStringAnsi(ptr);
        }
        finally
        {
            runtime_free_string(ptr);
        }
    }

    public static Dictionary<string, byte[]> GetFileFromArchive()
    {
        var fileData = new Dictionary<string, byte[]>();
        if (!string.IsNullOrEmpty(ZIP_PATH))
        {
            using (ZipArchive archive = ZipFile.OpenRead(ZIP_PATH))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    if (string.IsNullOrEmpty(entry.Name)) continue;

                    using (Stream entryStream = entry.Open())
                    using (MemoryStream ms = new MemoryStream())
                    {
                        entryStream.CopyTo(ms);
                        fileData.Add(entry.FullName, ms.ToArray());
                    }
                }
            }
        }
        foreach (var kv in Module.PanelNodeStore.GetFiles())
            fileData[kv.Key] = kv.Value;
        return fileData;
    }

    public static bool ExportState(string path) => runtime_export_state(path);

    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr get_entity_text_map_value(
        [MarshalAs(UnmanagedType.LPStr)] string entityId,
        [MarshalAs(UnmanagedType.LPStr)] string key);

    public static string GetEntityTextMapValue(string? entityId, string name)
    {
        if (entityId == null) return string.Empty;
        var ptr = get_entity_text_map_value(entityId, name);
        if (ptr == IntPtr.Zero) return string.Empty;
        try
        {
            var s = Marshal.PtrToStringAnsi(ptr) ?? string.Empty;
            var sanitized = new string(s.Where(c => !char.IsControl(c) || c == '\r' || c == '\n' || c == '\t').ToArray());
            return sanitized;
        }
        finally { runtime_free_string(ptr); }
    }

    public static string ReadEntityTextValue(string? entityId, string name)
    {
        if (entityId == null) return string.Empty;
        var ptr = get_entity_text_map_value(entityId, name);
        if (ptr == IntPtr.Zero) return string.Empty;
        try
        {
            var s = Marshal.PtrToStringAnsi(ptr) ?? string.Empty;
            var sanitized = new string(s.Where(c => !char.IsControl(c) || c == '\r' || c == '\n' || c == '\t').ToArray());
            return sanitized;
        }
        finally { runtime_free_string(ptr); }
    }

    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr get_entity_number_map_value(
        [MarshalAs(UnmanagedType.LPStr)] string entityId,
        [MarshalAs(UnmanagedType.LPStr)] string key);

    public static string GetEntityNumberMapValue(string? entityId, string name)
    {
        if (entityId == null) return string.Empty;
        var ptr = get_entity_number_map_value(entityId, name);
        if (ptr == IntPtr.Zero) return string.Empty;
        try
        {
            var s = Marshal.PtrToStringAnsi(ptr) ?? string.Empty;
            var sanitized = new string(s.Where(c => !char.IsControl(c) || c == '\r' || c == '\n' || c == '\t').ToArray());
            return sanitized;
        }
        finally { runtime_free_string(ptr); }
    }

    public static string ReadEntityNumberValue(string? entityId, string name)
    {
        if (entityId == null) return string.Empty;
        var ptr = get_entity_number_map_value(entityId, name);
        if (ptr == IntPtr.Zero) return string.Empty;
        try
        {
            var s = Marshal.PtrToStringAnsi(ptr) ?? string.Empty;
            var sanitized = new string(s.Where(c => !char.IsControl(c) || c == '\r' || c == '\n' || c == '\t').ToArray());
            return sanitized;
        }
        finally { runtime_free_string(ptr); }
    }

    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
    private static extern void runtime_set_entity_number_map_value(
        [MarshalAs(UnmanagedType.LPStr)] string entityId,
        [MarshalAs(UnmanagedType.LPStr)] string key,
        [MarshalAs(UnmanagedType.LPStr)] string value);

    public static void SetEntityNumberMapValue(string? entityId, string key, double value)
    {
        runtime_set_entity_number_map_value(entityId, key, value.ToString());
    }

    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
    private static extern void runtime_set_entity_text_map_value(
        [MarshalAs(UnmanagedType.LPStr)] string entityId,
        [MarshalAs(UnmanagedType.LPStr)] string key,
        [MarshalAs(UnmanagedType.LPStr)] string value);

    public static void SetEntityTextMapValue(string? entityId, string key, string? value)
    {
        runtime_set_entity_text_map_value(entityId, key, value ?? string.Empty);
    }

    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
    private static extern void runtime_emit_action([MarshalAs(UnmanagedType.LPStr)] string action);

    public static void emitAction(string action)
    {
        // C#-side Jint handlers take precedence (the native runtime does not
        // understand stopPropagation); fall back to the native emit when no
        // C# handler exists so non-module archives still work.
        var handled = NewGameProject.Module.PanelNodeStore.TryEmitAction(action);
        if (handled)
            return;
        runtime_emit_action(action);
    }

    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
    private static extern long runtime_run_iteration(long elapsedUnits);

    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
    private static extern long runtime_get_elapsed_time_units();

    public static long RunIteration(long elapsedUnits = 0)
    {
        var elapsed = runtime_get_elapsed_time_units() + elapsedUnits;
        NewGameProject.Module.EffectStore.Process(elapsed);
        NewGameProject.Module.BehaviorStore.Process(elapsed);
        return runtime_run_iteration(elapsedUnits);
    }

    public static long GetElapsedTimeUnits() => runtime_get_elapsed_time_units();

    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr runtime_fetch_ui_state();

    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr runtime_fetch_ui_delta();

    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr runtime_fetch_ui_animations();

    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr runtime_fetch_world_state();

    public static string FetchUiState()
    {
        var ptr = runtime_fetch_ui_state();
        if (ptr == IntPtr.Zero) return string.Empty;
        try { return Marshal.PtrToStringAnsi(ptr) ?? string.Empty; }
        finally { runtime_free_string(ptr); }
    }

    public static string FetchUiDelta()
    {
        var ptr = runtime_fetch_ui_delta();
        if (ptr == IntPtr.Zero) return string.Empty;
        try { return Marshal.PtrToStringAnsi(ptr) ?? string.Empty; }
        finally { runtime_free_string(ptr); }
    }

    public static string FetchUiAnimations()
    {
        var ptr = runtime_fetch_ui_animations();
        if (ptr == IntPtr.Zero) return string.Empty;
        try { return Marshal.PtrToStringAnsi(ptr) ?? string.Empty; }
        finally { runtime_free_string(ptr); }
    }

    public static string FetchWorldState()
    {
        var ptr = runtime_fetch_world_state();
        if (ptr == IntPtr.Zero) return string.Empty;
        try { return Marshal.PtrToStringAnsi(ptr) ?? string.Empty; }
        finally { runtime_free_string(ptr); }
    }

    private delegate void LogCallback([MarshalAs(UnmanagedType.LPStr)] string message);
    private static Action<string>? userLogCallback;
    private static LogCallback? nativeLogCallback;

    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
    private static extern void register_logger(IntPtr callback);

    public static bool HasLogger => userLogCallback != null;

    public static void Log(string message) => userLogCallback?.Invoke(message);

    public static void ClearLogger()
    {
        userLogCallback = null;
        nativeLogCallback = null;
    }

    public static void RegisterLogger(Action<string> callback)
    {
        if (userLogCallback != null)
            return;

        userLogCallback = callback;
        nativeLogCallback = (message) => userLogCallback?.Invoke(message);
        IntPtr funcPtr = Marshal.GetFunctionPointerForDelegate(nativeLogCallback);
        register_logger(funcPtr);
    }
}
