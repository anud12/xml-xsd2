using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.Json;
using NewGameProject.Runtime.Types;

public static class RuntimeInterop
{
    // Adjust LIB_NAME if the produced DLL name differs (e.g., xml-xsd2 or xml_xsd2)
    private const string LIB_NAME = "libxml_xsd2";


    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr get_panel_ids();

    public static string[] GetPanelIds()
    {
        var ptr = get_panel_ids();
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
    private static extern IntPtr get_panel_by_id([MarshalAs(UnmanagedType.LPStr)] string id);
    
    public static Panel GetPanelById(string id)
    {
        IntPtr ptr = get_panel_by_id(id);
        if (ptr == IntPtr.Zero) return default(Panel);
        try
        {
            // The native runtime now returns a JSON string for the panel object
            var s = Marshal.PtrToStringAnsi(ptr) ?? string.Empty;
            try
            {
                using var doc = JsonDocument.Parse(s);
                var root = doc.RootElement;
                var pid = root.GetProperty("id").GetString() ?? string.Empty;
                string background = null;
                if (root.TryGetProperty("background", out var b)) {
                    if (b.ValueKind == JsonValueKind.String) background = b.GetString();
                    else if (b.ValueKind == JsonValueKind.Null) background = null;
                    else { background = b.ToString(); }
                }
                return new Panel { Id = pid, Background = background };
            }
            catch (Exception)
            {
                // Fallback: treat returned string as plain id
                return new Panel { Id = s, Background = null };
            }
        }
        finally
        {
            runtime_free_string(ptr);
        }
    }


    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr runtime_process_archive([MarshalAs(UnmanagedType.LPStr)] string path);

    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
    private static extern void runtime_free_string(IntPtr s);

    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
    private static extern bool runtime_export_state([MarshalAs(UnmanagedType.LPStr)] string path);

    public static string ProcessArchive(string zipPath)
    {
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

    public static bool ExportState(string path) => runtime_export_state(path);
}