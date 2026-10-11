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
        var ptr = runtime_fetch_panel_ids();
        if (ptr == IntPtr.Zero) return Array.Empty<string>();
        try
        {
            var json = Marshal.PtrToStringAnsi(ptr) ?? "[]";
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var ids = new List<string>();
            foreach (var id in doc.RootElement.EnumerateArray())
                ids.Add(id.GetString() ?? "");
            return ids.ToArray();
        }
        catch { return Array.Empty<string>(); }
        finally { runtime_free_string(ptr); }
    }

    public static Panel GetPanelById(string id)
    {
        var ptr = runtime_fetch_panel_json(id);
        if (ptr == IntPtr.Zero) return default;
        try
        {
            var json = Marshal.PtrToStringAnsi(ptr);
            if (json != null && PanelParser.TryParse(json, out var panel))
                return panel;
            return default;
        }
        finally { runtime_free_string(ptr); }
    }

    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr runtime_fetch_panel_json([MarshalAs(UnmanagedType.LPStr)] string id);

    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr runtime_fetch_panel_ids();

    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr runtime_fetch_action_ids();

    /// Registered action ids as a JSON array of strings, or an empty array.
    public static string[] GetActionIds()
    {
        var ptr = runtime_fetch_action_ids();
        if (ptr == IntPtr.Zero) return Array.Empty<string>();
        try
        {
            var json = Marshal.PtrToStringAnsi(ptr) ?? "[]";
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var ids = new List<string>();
            foreach (var id in doc.RootElement.EnumerateArray())
                ids.Add(id.GetString() ?? "");
            return ids.ToArray();
        }
        catch { return Array.Empty<string>(); }
        finally { runtime_free_string(ptr); }
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

    // Mirrors the Rust ActionArgs: count key strings (NUL-terminated, list of
    // pointers) + count f64 values, parallel and deterministic by key order.
    [StructLayout(LayoutKind.Sequential)]
    private struct ActionArgs
    {
        public int count;
        public IntPtr keys;
        public IntPtr values;
    }

    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
    private static extern void runtime_emit_action_args(
        [MarshalAs(UnmanagedType.LPStr)] string action,
        ref ActionArgs args);

    public static void emitAction(string action)
        => emitAction(action, "{}");

    /// Emits an action with an args payload (JSON object). The args are
    /// serialized into the native ActionArgs block and delivered to the
    /// action's ctx.args by the Rust runtime.
    public static void emitAction(string action, string argsJson)
    {
        var pairs = ParseArgsJson(argsJson);
        if (pairs.Count == 0)
        {
            runtime_emit_action(action);
            return;
        }

        var keys = new IntPtr[pairs.Count];
        var values = new double[pairs.Count];
        var nativeKeys = new IntPtr[pairs.Count];
        try
        {
            for (var i = 0; i < pairs.Count; i++)
            {
                nativeKeys[i] = Marshal.StringToHGlobalAnsi(pairs[i].Key);
                keys[i] = nativeKeys[i];
                values[i] = pairs[i].Value;
            }
            var keyArr = GCHandle.Alloc(keys, GCHandleType.Pinned);
            var valArr = GCHandle.Alloc(values, GCHandleType.Pinned);
            try
            {
                var block = new ActionArgs
                {
                    count = pairs.Count,
                    keys = keyArr.AddrOfPinnedObject(),
                    values = valArr.AddrOfPinnedObject(),
                };
                runtime_emit_action_args(action, ref block);
            }
            finally
            {
                keyArr.Free();
                valArr.Free();
            }
        }
        finally
        {
            for (var i = 0; i < nativeKeys.Length; i++)
                if (nativeKeys[i] != IntPtr.Zero) Marshal.FreeHGlobal(nativeKeys[i]);
        }
    }

    static List<(string Key, double Value)> ParseArgsJson(string argsJson)
    {
        var list = new List<(string, double)>();
        if (string.IsNullOrWhiteSpace(argsJson)) return list;
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(argsJson);
            if (doc.RootElement.ValueKind != System.Text.Json.JsonValueKind.Object)
                return list;
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                var v = prop.Value.ValueKind == System.Text.Json.JsonValueKind.Number
                    ? prop.Value.GetDouble()
                    : 0.0;
                list.Add((prop.Name, v));
            }
        }
        catch
        {
            // Unparseable args: emit the action with no args.
        }
        return list;
    }

    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
    private static extern void runtime_emit_action_for(
        [MarshalAs(UnmanagedType.LPStr)] string action,
        [MarshalAs(UnmanagedType.LPStr)] string actor);

    /// Emits an action bound to an actor (entity id). While that actor has a
    /// parked action plan, further actions for it are rejected by the native
    /// runtime: the plan is neither interrupted nor queued behind them.
    public static void emitActionFor(string action, string actor)
    {
        runtime_emit_action_for(action, actor);
    }

    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
    private static extern void runtime_ui_js_click(
        [MarshalAs(UnmanagedType.LPStr)] string id, int col, int row);

    /// Routes a click to a node whose legacy `onClick` handler is a JS
    /// function (the sim context keeps the handler; `col`/`row` are the
    /// resolved cursor cell the handler's `ctx.cursor` reports).
    public static void UiJsClick(string id, int col, int row)
    {
        runtime_ui_js_click(id, col, row);
    }

    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
    private static extern bool runtime_is_actor_interruptible(
        [MarshalAs(UnmanagedType.LPStr)] string actorId);

    /// True while the actor's parked action plan was marked interruptible via
    /// ctx.allowInterrupt(); a busy, interruptible actor accepts a new action,
    /// a busy, non-interruptible actor drops it.
    public static bool IsActorInterruptible(string actorId)
    {
        return runtime_is_actor_interruptible(actorId ?? string.Empty);
    }

    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
    private static extern bool runtime_is_actor_busy(
        [MarshalAs(UnmanagedType.LPStr)] string actorId);

    /// True while the actor has a parked action plan. A free actor has
    /// nothing queued: it is not running or waiting on any action.
    public static bool IsActorBusy(string actorId)
    {
        return runtime_is_actor_busy(actorId ?? string.Empty);
    }

    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr runtime_get_actor_active_action(
        [MarshalAs(UnmanagedType.LPStr)] string actorId);

    /// The name of the action whose plan is currently parked for this actor, or
    /// an empty string while the actor is free.
    public static string GetActorActiveAction(string actorId)
    {
        var ptr = runtime_get_actor_active_action(actorId ?? string.Empty);
        if (ptr == IntPtr.Zero) return string.Empty;
        try { return Marshal.PtrToStringAnsi(ptr) ?? string.Empty; }
        finally { runtime_free_string(ptr); }
    }

    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
    private static extern long runtime_run_iteration(long elapsedUnits);

    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
    private static extern long runtime_get_elapsed_time_units();

    public static long RunIteration(long elapsedUnits = 0)
    {
        var elapsed = runtime_get_elapsed_time_units() + elapsedUnits;
        NewGameProject.Module.BehaviorStore.Process(elapsed);
        return runtime_run_iteration(elapsedUnits);
    }

    public static long GetElapsedTimeUnits() => runtime_get_elapsed_time_units();

    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr runtime_get_entities_inside_area(
        [MarshalAs(UnmanagedType.LPStr)] string entityId,
        [MarshalAs(UnmanagedType.LPStr)] string areaId);

    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
    private static extern void runtime_free_inside_area(IntPtr ptr);

    /// The ids of the entities inside one *named* area of the entity (id
    /// ascending, self excluded, deduped across containers). The area name is
    /// required: it is resolved first, then the containment/overlap test runs.
    /// An entity with no such area — or in no container — yields an empty array.
    public static string[] GetEntitiesInsideArea(string? entityId, string? areaId)
    {
        if (entityId == null || areaId == null) return Array.Empty<string>();
        var ptr = runtime_get_entities_inside_area(entityId, areaId);
        if (ptr == IntPtr.Zero) return Array.Empty<string>();
        return ReadInsideArea(ptr);
    }

    private static string[] ReadInsideArea(IntPtr ptr)
    {
        try
        {
            var json = Marshal.PtrToStringAnsi(ptr) ?? "[]";
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var ids = new List<string>();
            if (doc.RootElement.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                foreach (var id in doc.RootElement.EnumerateArray())
                    ids.Add(id.GetString() ?? "");
            }
            return ids.ToArray();
        }
        catch { return Array.Empty<string>(); }
        finally { runtime_free_inside_area(ptr); }
    }

    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr runtime_get_entity_containers(
        [MarshalAs(UnmanagedType.LPStr)] string entityId);

    /// The ids of the containers the named entity is a member of (diagnostics).
    public static string[] GetEntityContainers(string? entityId)
    {
        if (entityId == null) return Array.Empty<string>();
        var ptr = runtime_get_entity_containers(entityId);
        if (ptr == IntPtr.Zero) return Array.Empty<string>();
        try
        {
            var json = Marshal.PtrToStringAnsi(ptr) ?? "[]";
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var ids = new List<string>();
            if (doc.RootElement.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                foreach (var id in doc.RootElement.EnumerateArray())
                    ids.Add(id.GetString() ?? "");
            }
            return ids.ToArray();
        }
        catch { return Array.Empty<string>(); }
        finally { runtime_free_inside_area(ptr); }
    }

    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr runtime_fetch_ui_state();

    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
    private static extern void runtime_free_ui_snapshot(IntPtr ptr);

    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr runtime_fetch_ui_delta();

    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
    private static extern void runtime_free_ui_delta(IntPtr ptr);

    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr runtime_fetch_ui_animations();

    /// Full UI tree as a binary slab (`UiAbi.UiSnapshot`); the returned
    /// pointer stays valid until <see cref="FreeUiState"/>.
    public static IntPtr FetchUiState()
    {
        return runtime_fetch_ui_state();
    }

    public static void FreeUiState(IntPtr ptr)
    {
        if (ptr != IntPtr.Zero) runtime_free_ui_snapshot(ptr);
    }

    /// Pending delta as a binary slab (`UiAbi.UiDelta`), or IntPtr.Zero when
    /// clean. Fetching consumes the pending delta; release with
    /// <see cref="FreeUiDelta"/>.
    public static IntPtr FetchUiDelta()
    {
        return runtime_fetch_ui_delta();
    }

    public static void FreeUiDelta(IntPtr ptr)
    {
        if (ptr != IntPtr.Zero) runtime_free_ui_delta(ptr);
    }

    public static string FetchUiAnimations()
    {
        var ptr = runtime_fetch_ui_animations();
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
