using System.IO.Compression;
using System.Runtime.InteropServices;

namespace NewGameProject.Runtime;

public static class RuntimeInterop
{
    // Adjust LIB_NAME if the produced DLL name differs (e.g., xml-xsd2 or xml_xsd2)
    private const string LIB_NAME = "libxml_xsd2";
    private static string ZIP_PATH = ""; 

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
    private static extern IntPtr get_panel_by_id_struct([MarshalAs(UnmanagedType.LPStr)] string id);

    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
    private static extern void runtime_free_panel(IntPtr p);

    public static Panel GetPanelById(string id)
    {
        IntPtr ptr = get_panel_by_id_struct(id);
        if (ptr == IntPtr.Zero) return default(Panel);
        try
        {
            // Marshal native PanelFfi struct into managed Panel
            var native = Marshal.PtrToStructure<NativePanel>(ptr);
            string background = null;
            if (native.background != IntPtr.Zero) background = Marshal.PtrToStringAnsi(native.background);
            var pid = Marshal.PtrToStringAnsi(native.id) ?? string.Empty;
            var panel = new Panel { Id = pid, Background = background };
            // Populate numeric/layout fields
            panel.Anchor = new Vector2 { X = native.anchor.x, Y = native.anchor.y };
            panel.Pivot = new Vector2 { X = native.pivot.x, Y = native.pivot.y };
            panel.Offset = new Offset
            {
                top = native.offset.top,
                bottom = native.offset.bottom,
                left = native.offset.left,
                right = native.offset.right
            };
            panel.Size = new Size { Height = native.size.height, Width = native.size.width };
            return panel;
        }
        finally
        {
            runtime_free_panel(ptr);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct AnchorFfi { public float x; public float y; }

    [StructLayout(LayoutKind.Sequential)]
    private struct OffsetFfi
    {
        public float top;
        public float bottom;
        public float left;
        public float right;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct SizeFfi { public float height; public float width; }

    private struct NativePanel
    {
        public IntPtr id;
        public IntPtr background;
        public AnchorFfi anchor;
        public AnchorFfi pivot;
        public OffsetFfi offset;
        public SizeFfi size;
        public IntPtr children_callback;
    }


    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr runtime_process_archive([MarshalAs(UnmanagedType.LPStr)] string path);

    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
    private static extern void runtime_free_string(IntPtr s);

    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
    private static extern bool runtime_export_state([MarshalAs(UnmanagedType.LPStr)] string path);

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

        // Open the zip file for reading
        using (ZipArchive archive = ZipFile.OpenRead(ZIP_PATH))
        {
            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                // Ignore directories, only grab files
                if (string.IsNullOrEmpty(entry.Name)) continue;

                using (Stream entryStream = entry.Open())
                using (MemoryStream ms = new MemoryStream())
                {
                    entryStream.CopyTo(ms);
                    fileData.Add(entry.FullName, ms.ToArray());
                }
            }
        }

        return fileData;
    } 

    public static bool ExportState(string path) => runtime_export_state(path);
}