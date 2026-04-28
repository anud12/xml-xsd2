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
        try { System.IO.File.WriteAllText("E:\\workspace\\test_log.txt", $"GetPanelById called with id={id}\nStruct size: {Marshal.SizeOf<NativePanel>()}\n"); } catch { }
        IntPtr ptr = get_panel_by_id_struct(id);
        var ffiReturnMsg = $"FFI returned ptr={ptr.ToInt64():X}";
        try { System.IO.File.AppendAllText("E:\\workspace\\test_log.txt", ffiReturnMsg + "\n"); } catch { }
        if (ptr == IntPtr.Zero) return default(Panel);
        
        try
        {
            // Try marshaling the entire struct at once
            var nativePanel = Marshal.PtrToStructure<NativePanel>(ptr);
            System.Diagnostics.Debug.WriteLine($"[GetPanelById] Marshaled struct successfully");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[GetPanelById] Exception during marshaling: {ex.Message}");
            return default(Panel);
        }
        
        try
        {
            // Calculate offsets
            int offId = (int)Marshal.OffsetOf(typeof(NativePanel), "id");
            int offBackground = (int)Marshal.OffsetOf(typeof(NativePanel), "background");
            int offPanelJson = (int)Marshal.OffsetOf(typeof(NativePanel), "panel_json");
            int offChildren = (int)Marshal.OffsetOf(typeof(NativePanel), "children_json");
            
            try { 
                System.IO.File.WriteAllText("E:\\workspace\\offsets_check.txt", 
                    $"offId={offId}, offBackground={offBackground}, offPanelJson={offPanelJson}, offChildren={offChildren}");
            } catch { }
            
            // Try marshaling the entire struct at once to compare
            NativePanel nativePanel = Marshal.PtrToStructure<NativePanel>(ptr);
            
            // Also read raw bytes from the struct to see what's really there
            int structSize = Marshal.SizeOf<NativePanel>();
            byte[] rawBytes = new byte[structSize];
            Marshal.Copy(ptr, rawBytes, 0, structSize);
            
            var sizeMsg = $"Struct size: {structSize} bytes";
            try { System.IO.File.AppendAllText("E:\\workspace\\test_log.txt", sizeMsg + "\n"); } catch { }
            
            var ptrMsg = $"Reading struct from ptr={ptr.ToInt64():X}";
            try { System.IO.File.AppendAllText("E:\\workspace\\test_log.txt", ptrMsg + "\n"); } catch { }
            
            // Extract IntPtr values manually from the raw bytes at correct offsets
            IntPtr id_manual = Marshal.ReadIntPtr(ptr, 0);
            IntPtr bg_manual = Marshal.ReadIntPtr(ptr, 8);
            IntPtr children_manual = Marshal.ReadIntPtr(ptr, 56);
            IntPtr panelJson_manual = Marshal.ReadIntPtr(ptr, 64);
            
            // Convert hex bytes to readable format
            string hexBytes = "";
            for (int i = 0; i < Math.Min(80, rawBytes.Length); i += 8) {
                if (i + 8 <= rawBytes.Length) {
                    long value = BitConverter.ToInt64(rawBytes, i);
                    hexBytes += $"@{i:D2}:0x{value:X016}  ";
                }
            }
            
            var log_str = $"Unmarshaled struct via PtrToStructure: id={nativePanel.id.ToInt64()}, bg={nativePanel.background.ToInt64()}, panelJson={nativePanel.panel_json.ToInt64()}, children={nativePanel.children_json.ToInt64()}\n" +
                          $"Manual reads from offsets: id={id_manual.ToInt64()}, bg={bg_manual.ToInt64()}, children={children_manual.ToInt64()}, panelJson={panelJson_manual.ToInt64()}\n" +
                          $"Hex bytes: {hexBytes}";
            try { System.IO.File.AppendAllText("E:\\workspace\\test_log.txt", log_str + "\n"); } catch { }
            
            // Now read the pointer fields
            try
            {
                var beforeBg = $"About to read background from ptr={nativePanel.background.ToInt64()}";
                try { System.IO.File.AppendAllText("E:\\workspace\\test_log.txt", beforeBg + "\n"); } catch { }
                string background = SafePtrToStringAnsi(nativePanel.background);
                var afterBg = $"Successfully read background";
                try { System.IO.File.AppendAllText("E:\\workspace\\test_log.txt", afterBg + "\n"); } catch { }
            }
            catch (Exception exBg)
            {
                var errBg = $"Error reading background: {exBg.Message}";
                try { System.IO.File.AppendAllText("E:\\workspace\\test_log.txt", errBg + "\n"); } catch { }
                // Continue anyway
            }
            
            string bgFinal = SafePtrToStringAnsi(nativePanel.background);
            string pid = SafePtrToStringAnsi(nativePanel.id) ?? string.Empty;

            var panel = new Panel { Id = pid, Background = bgFinal };
            // Populate numeric/layout fields
            panel.Anchor = new Vector2 { X = nativePanel.anchor.x, Y = nativePanel.anchor.y };
            panel.Pivot = new Vector2 { X = nativePanel.pivot.x, Y = nativePanel.pivot.y };
            panel.Offset = new Offset
            {
                top = nativePanel.offset.top,
                bottom = nativePanel.offset.bottom,
                left = nativePanel.offset.left,
                right = nativePanel.offset.right
            };
            panel.Size = new Size { Height = nativePanel.size.height, Width = nativePanel.size.width };
            
            // Parse onClick from panel_json if present
            if (nativePanel.panel_json != IntPtr.Zero)
            {
                var before_read = $"Attempting to read panelJson from ptr={nativePanel.panel_json.ToInt64()}";
                try { System.IO.File.AppendAllText("E:\\workspace\\test_log.txt", before_read + "\n"); } catch { }
                try
                {
                    var panelJson = SafePtrToStringAnsi(nativePanel.panel_json);
                    var after_read = $"Read panelJson: {panelJson}";
                    try { System.IO.File.AppendAllText("E:\\workspace\\test_log.txt", after_read + "\n"); } catch { }
                    System.Diagnostics.Debug.WriteLine($"[GetPanelById] Successfully read panelJson for id '{pid}': {panelJson}");
                    if (!string.IsNullOrEmpty(panelJson))
                    {
                        try
                        {
                            using var doc = System.Text.Json.JsonDocument.Parse(panelJson);
                            if (doc.RootElement.TryGetProperty("onClick", out var onClickProp) && onClickProp.ValueKind == System.Text.Json.JsonValueKind.Object)
                            {
                                if (onClickProp.TryGetProperty("type", out var t) && t.GetString() == "emitAction")
                                {
                                    var actionName = onClickProp.TryGetProperty("actionName", out var an) ? an.GetString() ?? "" : "";
                                    panel.OnClick = new PanelOnClickHandler { ActionName = actionName };
                                }
                            }

                            // Parse content if present
                            if (doc.RootElement.TryGetProperty("content", out var contentProp) && contentProp.ValueKind == System.Text.Json.JsonValueKind.Object)
                            {
                                var contentType = contentProp.TryGetProperty("type", out var ct) ? ct.GetString() : null;
                                var contentAlign = contentProp.TryGetProperty("align", out var ca) ? ca.GetString() ?? "center" : "center";
                                if (contentType == "constant")
                                {
                                    var contentValue = contentProp.TryGetProperty("value", out var cv) ? cv.GetString() : null;
                                    if (contentValue != null)
                                    {
                                        panel.Content = new ConstantTextContent(contentValue, contentAlign);
                                    }
                                }
                                else if (contentType == "entityStringValue")
                                {
                                    var contentName = contentProp.TryGetProperty("name", out var cn) ? cn.GetString() : null;
                                    if (contentName != null)
                                    {
                                        panel.Content = new EntityStringValueContent(contentName, contentAlign);
                                    }
                                }
                            }
                        }
                        catch (System.Text.Json.JsonException ex)
                        {
                            // Invalid JSON in panel_json, skip onClick/content parsing
                            System.Diagnostics.Debug.WriteLine($"JSON parse error: {ex.Message}");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Unexpected error parsing panel_json: {ex.Message}");
                        }
                    }
                }
                catch (AccessViolationException ex)
                {
                    // panel_json pointer is invalid; log and skip
                    System.Diagnostics.Debug.WriteLine($"AccessViolationException reading panel_json: {ex.Message}");
                }
                catch (Exception ex)
                {
                    // Other errors reading panel_json
                    System.Diagnostics.Debug.WriteLine($"Error reading panel_json: {ex.Message}");
                }
            }
            
            if (nativePanel.children_json != IntPtr.Zero)
            {
                string childrenJson = SafePtrToStringAnsi(nativePanel.children_json);
                if (!string.IsNullOrEmpty(childrenJson))
                {
                    try
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

                            // parse content if present
                            if (elem.TryGetProperty("content", out var contentProp) && contentProp.ValueKind == System.Text.Json.JsonValueKind.Object)
                            {
                                var contentType = contentProp.TryGetProperty("type", out var ct) ? ct.GetString() : null;
                                var contentAlign = contentProp.TryGetProperty("align", out var ca) ? ca.GetString() ?? "center" : "center";
                                if (contentType == "constant")
                                {
                                    var contentValue = contentProp.TryGetProperty("value", out var cv) ? cv.GetString() : null;
                                    if (contentValue != null)
                                    {
                                        child.Content = new ConstantTextContent(contentValue, contentAlign);
                                    }
                                }
                                else if (contentType == "entityStringValue")
                                {
                                    var contentName = contentProp.TryGetProperty("name", out var cn) ? cn.GetString() : null;
                                    if (contentName != null)
                                    {
                                        child.Content = new EntityStringValueContent(contentName, contentAlign);
                                    }
                                }
                            }

                            childList.Add(child);
                        }
                        panel.Children = childList.ToArray();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error parsing children_json: {ex.Message}");
                    }
                }
            }
            return panel;
        }
        finally
        {
            runtime_free_panel(ptr);
        }
    }
    
    private static IntPtr SafeReadIntPtr(IntPtr basePtr, int offset)
    {
        try
        {
            return Marshal.ReadIntPtr(basePtr, offset);
        }
        catch (AccessViolationException ex)
        {
            System.Diagnostics.Debug.WriteLine($"AccessViolationException reading IntPtr at offset {offset}: {ex.Message}");
            return IntPtr.Zero;
        }
    }
    
    private static string SafePtrToStringAnsi(IntPtr ptr)
    {
        if (ptr == IntPtr.Zero) {
            System.Diagnostics.Debug.WriteLine($"SafePtrToStringAnsi: ptr is null");
            return null;
        }
        
        // Try to read a single byte to verify the pointer is valid
        try
        {
            byte b = Marshal.ReadByte(ptr);
            var readMsg = $"SafePtrToStringAnsi: successfully read first byte (0x{b:X2}) from ptr={ptr.ToInt64()}";
            try { System.IO.File.AppendAllText("E:\\workspace\\test_log.txt", readMsg + "\n"); } catch { }
        }
        catch (AccessViolationException ex)
        {
            var errMsg = $"SafePtrToStringAnsi: Cannot even read first byte! AccessViolationException: {ex.Message}";
            try { System.IO.File.AppendAllText("E:\\workspace\\test_log.txt", errMsg + "\n"); } catch { }
            return null;
        }
        
        try
        {
            var result = Marshal.PtrToStringAnsi(ptr);
            System.Diagnostics.Debug.WriteLine($"SafePtrToStringAnsi: successfully converted ptr={ptr} to string");
            return result;
        }
        catch (AccessViolationException ex)
        {
            System.Diagnostics.Debug.WriteLine($"AccessViolationException converting pointer to ANSI string: {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error converting pointer to ANSI string: {ex.Message}");
            return null;
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
        public IntPtr panel_json;
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

    // Logger callback support
    private delegate void LogCallback([MarshalAs(UnmanagedType.LPStr)] string message);
    private static Action<string>? userLogCallback;
    private static LogCallback? nativeLogCallback;

    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
    private static extern void register_logger(IntPtr callback);

    public static void RegisterLogger(Action<string> callback)
    {
        userLogCallback = callback;
        // Create a delegate that will be called from native code
        nativeLogCallback = (message) =>
        {
            userLogCallback?.Invoke(message);
        };
        // Register with native runtime - need to marshal the delegate as a function pointer
        IntPtr funcPtr = Marshal.GetFunctionPointerForDelegate(nativeLogCallback);
        register_logger(funcPtr);
    }
}
