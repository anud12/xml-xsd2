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
            // Safer manual marshaling: read only the fields needed and validate pointers before converting
            int offId = (int)Marshal.OffsetOf(typeof(NativePanel), "id");
            int offBackground = (int)Marshal.OffsetOf(typeof(NativePanel), "background");
            int offAnchor = (int)Marshal.OffsetOf(typeof(NativePanel), "anchor");
            int offPivot = (int)Marshal.OffsetOf(typeof(NativePanel), "pivot");
            int offOffset = (int)Marshal.OffsetOf(typeof(NativePanel), "offset");
            int offSize = (int)Marshal.OffsetOf(typeof(NativePanel), "size");
            int offChildren = (int)Marshal.OffsetOf(typeof(NativePanel), "children_json");

            IntPtr idPtr = Marshal.ReadIntPtr(ptr, offId);
            IntPtr backgroundPtr = Marshal.ReadIntPtr(ptr, offBackground);

            string background = null;
            if (backgroundPtr != IntPtr.Zero) background = Marshal.PtrToStringAnsi(backgroundPtr);
            var pid = idPtr != IntPtr.Zero ? Marshal.PtrToStringAnsi(idPtr) ?? string.Empty : string.Empty;

            var panel = new Panel { Id = pid, Background = background };
            // Populate numeric/layout fields
            var anchor = Marshal.PtrToStructure<AnchorFfi>(IntPtr.Add(ptr, offAnchor));
            var pivot = Marshal.PtrToStructure<AnchorFfi>(IntPtr.Add(ptr, offPivot));
            var offsetFfi = Marshal.PtrToStructure<OffsetFfi>(IntPtr.Add(ptr, offOffset));
            var sizeFfi = Marshal.PtrToStructure<SizeFfi>(IntPtr.Add(ptr, offSize));

            panel.Anchor = new Vector2 { X = anchor.x, Y = anchor.y };
            panel.Pivot = new Vector2 { X = pivot.x, Y = pivot.y };
            panel.Offset = new Offset
            {
                top = offsetFfi.top,
                bottom = offsetFfi.bottom,
                left = offsetFfi.left,
                right = offsetFfi.right
            };
            panel.Size = new Size { Height = sizeFfi.height, Width = sizeFfi.width };
            IntPtr childrenJsonPtr = Marshal.ReadIntPtr(ptr, offChildren);
            if (childrenJsonPtr != IntPtr.Zero)
            {
                var childrenJson = Marshal.PtrToStringAnsi(childrenJsonPtr);
                if (!string.IsNullOrEmpty(childrenJson))
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(childrenJson);
                    var childList = new List<Panel>();
                    foreach (var elem in doc.RootElement.EnumerateArray())
                    {
                        var child = new Panel
                        {
                            Id = elem.TryGetProperty("id", out var idProp) ? idProp.GetString() ?? "" : "",
                            Background = elem.TryGetProperty("background", out var bgProp) && bgProp.ValueKind != System.Text.Json.JsonValueKind.Null ? bgProp.GetString() : null,
                            Anchor = new Vector2
                            {
                                X = elem.TryGetProperty("anchor", out var anc) && anc.TryGetProperty("x", out var ax) ? ax.GetSingle() : 0f,
                                Y = elem.TryGetProperty("anchor", out var anc2) && anc2.TryGetProperty("y", out var ay) ? ay.GetSingle() : 0f,
                            },
                            Pivot = new Vector2
                            {
                                X = elem.TryGetProperty("pivot", out var piv) && piv.TryGetProperty("x", out var px) ? px.GetSingle() : 0f,
                                Y = elem.TryGetProperty("pivot", out var piv2) && piv2.TryGetProperty("y", out var py) ? py.GetSingle() : 0f,
                            },
                            Offset = new Offset
                            {
                                top = elem.TryGetProperty("offset", out var off) && off.TryGetProperty("top", out var ot) ? ot.GetSingle() : 0f,
                                bottom = elem.TryGetProperty("offset", out var off2) && off2.TryGetProperty("bottom", out var ob) ? ob.GetSingle() : 0f,
                                left = elem.TryGetProperty("offset", out var off3) && off3.TryGetProperty("left", out var ol) ? ol.GetSingle() : 0f,
                                right = elem.TryGetProperty("offset", out var off4) && off4.TryGetProperty("right", out var or) ? or.GetSingle() : 0f,
                            },
                            Size = new Size
                            {
                                Height = elem.TryGetProperty("size", out var sz) && sz.TryGetProperty("height", out var sh) ? sh.GetSingle() : 0f,
                                Width = elem.TryGetProperty("size", out var sz2) && sz2.TryGetProperty("width", out var sw) ? sw.GetSingle() : 0f,
                            },
                        };

                        // parse onClick if present
                        if (elem.TryGetProperty("onClick", out var onClickProp) && onClickProp.ValueKind == System.Text.Json.JsonValueKind.Object)
                        {
                            if (onClickProp.TryGetProperty("type", out var t) && t.GetString() == "emitAction")
                            {
                                var actionName = onClickProp.TryGetProperty("actionName", out var an) ? an.GetString() ?? "" : "";
                                child.OnClick = new PanelOnClickHandler { ActionName = actionName };
                            }
                        }
                        childList.Add(child);
                    }
                    panel.Children = childList.ToArray();
                }
            }
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

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePanel
    {
        public IntPtr id;
        public IntPtr background;
        public AnchorFfi anchor;
        public AnchorFfi pivot;
        public OffsetFfi offset;
        public SizeFfi size;
        public IntPtr children_json;
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

    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
    private static extern void runtime_emit_action([MarshalAs(UnmanagedType.LPStr)] string action);

    public static void emitAction(string action) => runtime_emit_action(action);
}
