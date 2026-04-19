using System;
using System.Runtime.InteropServices;

public static class RuntimeInterop
{
    // Adjust LIB_NAME if the produced DLL name differs (e.g., xml-xsd2 or xml_xsd2)
    private const string LIB_NAME = "libxml_xsd2";


    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr get_panel_names();

    public static string[] GetPanelNames()
    {
        var ptr = get_panel_names();
        if (ptr == IntPtr.Zero) return Array.Empty<string>();
        var result = new System.Collections.Generic.List<string>();
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