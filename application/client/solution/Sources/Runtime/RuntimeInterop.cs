using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Linq;

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

    // Diagnostic method to test struct marshaling with fixed values
    public static void TestFixedStructMarshaling()
    {
        var logPath = "E:\\workspace\\test_fixed_struct_log.txt";
        try
        {
            System.IO.File.AppendAllText(logPath, "[START] TestFixedStructMarshaling called\n");
            
            IntPtr ptr = get_panel_by_id_struct("TEST_FIXED");
            System.IO.File.AppendAllText(logPath, $"[GOT_PTR] ptr={ptr.ToInt64():X}\n");
            
            if (ptr == IntPtr.Zero)
            {
                System.IO.File.AppendAllText(logPath, "[ERROR] Rust returned NULL\n");
                return;
            }
            
            // Read the full 72 bytes
            byte[] allBytes = new byte[72];
            Marshal.Copy(ptr, allBytes, 0, 72);
            var hexStr = string.Join(" ", allBytes.Select(b => $"{b:X2}"));
            System.IO.File.AppendAllText(logPath, $"[BYTES] 72 bytes: {hexStr}\n");
            
            // Try manual unmarshaling
            IntPtr id = Marshal.ReadIntPtr(ptr, 0);
            IntPtr bg = Marshal.ReadIntPtr(ptr, 8);
            IntPtr children = Marshal.ReadIntPtr(ptr, 56);
            IntPtr panelJson = Marshal.ReadIntPtr(ptr, 64);
            
            System.IO.File.AppendAllText(logPath, $"[POINTERS] id={id.ToInt64():X}, bg={bg.ToInt64():X}, children={children.ToInt64():X}, panelJson={panelJson.ToInt64():X}\n");
            
            // Try to read the strings
            string idStr = SafePtrToStringAnsi(id) ?? "NULL";
            string bgStr = SafePtrToStringAnsi(bg) ?? "NULL";
            string childrenStr = SafePtrToStringAnsi(children) ?? "NULL";
            string panelStr = SafePtrToStringAnsi(panelJson) ?? "NULL";
            
            System.IO.File.AppendAllText(logPath, $"[STRINGS] id='{idStr}', bg='{bgStr}', children='{childrenStr}', panel='{panelStr}'\n");
            System.IO.File.AppendAllText(logPath, "[END] TestFixedStructMarshaling completed successfully\n");
            
            runtime_free_panel(ptr);
        }
        catch (Exception ex)
        {
            System.IO.File.AppendAllText(logPath, $"[EXCEPTION] {ex.GetType().Name}: {ex.Message}\n");
            System.IO.File.AppendAllText(logPath, $"[STACK] {ex.StackTrace}\n");
        }
    }

    public static Panel GetPanelById(string id)
    {
        try { System.IO.File.WriteAllText("E:\\workspace\\test_log.txt", $"GetPanelById called with id={id}\nStruct size: {Marshal.SizeOf<NativePanel>()}\n"); } catch { }
        IntPtr ptr = get_panel_by_id_struct(id);
        var ffiReturnMsg = $"FFI returned ptr={ptr.ToInt64():X}";
        try { System.IO.File.AppendAllText("E:\\workspace\\test_log.txt", ffiReturnMsg + "\n"); } catch { }
        if (ptr == IntPtr.Zero) return default(Panel);
            
        try
        {
            // Manually read struct fields from the raw pointer to avoid Marshal.PtrToStructure alignment issues
            System.Diagnostics.Debug.WriteLine($"[GetPanelById] Reading struct fields manually from ptr={ptr.ToInt64():X}");
            
            // Read all IntPtr and float fields manually at their known offsets
            // NativePanel struct layout:
            // offset 0: IntPtr id (8 bytes)
            // offset 8: IntPtr background (8 bytes)
            // offset 16: float anchor.x (4 bytes)
            // offset 20: float anchor.y (4 bytes)
            // offset 24: float pivot.x (4 bytes)
            // offset 28: float pivot.y (4 bytes)
            // offset 32: float offset.top (4 bytes)
            // offset 36: float offset.bottom (4 bytes)
            // offset 40: float offset.left (4 bytes)
            // offset 44: float offset.right (4 bytes)
            // offset 48: float size.height (4 bytes)
            // offset 52: float size.width (4 bytes)
            // offset 56: IntPtr children_json (8 bytes)
            // offset 64: IntPtr panel_json (8 bytes)
            
            IntPtr id_ptr = Marshal.ReadIntPtr(ptr, 0);
            IntPtr bg_ptr = Marshal.ReadIntPtr(ptr, 8);
            
            // Read floats using byte buffer and BitConverter
            byte[] floatBuffer = new byte[4];
            
            Marshal.Copy(new IntPtr(ptr.ToInt64() + 16), floatBuffer, 0, 4);
            float anchor_x = BitConverter.ToSingle(floatBuffer, 0);
            
            Marshal.Copy(new IntPtr(ptr.ToInt64() + 20), floatBuffer, 0, 4);
            float anchor_y = BitConverter.ToSingle(floatBuffer, 0);
            
            Marshal.Copy(new IntPtr(ptr.ToInt64() + 24), floatBuffer, 0, 4);
            float pivot_x = BitConverter.ToSingle(floatBuffer, 0);
            
            Marshal.Copy(new IntPtr(ptr.ToInt64() + 28), floatBuffer, 0, 4);
            float pivot_y = BitConverter.ToSingle(floatBuffer, 0);
            
            Marshal.Copy(new IntPtr(ptr.ToInt64() + 32), floatBuffer, 0, 4);
            float offset_top = BitConverter.ToSingle(floatBuffer, 0);
            
            Marshal.Copy(new IntPtr(ptr.ToInt64() + 36), floatBuffer, 0, 4);
            float offset_bottom = BitConverter.ToSingle(floatBuffer, 0);
            
            Marshal.Copy(new IntPtr(ptr.ToInt64() + 40), floatBuffer, 0, 4);
            float offset_left = BitConverter.ToSingle(floatBuffer, 0);
            
            Marshal.Copy(new IntPtr(ptr.ToInt64() + 44), floatBuffer, 0, 4);
            float offset_right = BitConverter.ToSingle(floatBuffer, 0);
            
            Marshal.Copy(new IntPtr(ptr.ToInt64() + 48), floatBuffer, 0, 4);
            float size_height = BitConverter.ToSingle(floatBuffer, 0);
            
            Marshal.Copy(new IntPtr(ptr.ToInt64() + 52), floatBuffer, 0, 4);
            float size_width = BitConverter.ToSingle(floatBuffer, 0);

            // Debug: dump ALL raw bytes from offset 0 to see full struct
            byte[] allBytes = new byte[72];
            Marshal.Copy(ptr, allBytes, 0, 72);
            try { System.IO.File.WriteAllBytes("E:\\workspace\\struct_dump.bin", allBytes); } catch { }
            var allHex = string.Join(" ", allBytes.Select(b => $"{b:X2}"));
            try { System.IO.File.AppendAllText("E:\\workspace\\test_log.txt", $"ALL 72 bytes (hex): {allHex}\n"); } catch { }

            IntPtr children_json = Marshal.ReadIntPtr(ptr, 56);
            IntPtr panel_json = Marshal.ReadIntPtr(ptr, 64);
            
            // Construct the NativePanel from manually read fields
            var nativePanel = new NativePanel
            {
                id = id_ptr,
                background = bg_ptr,
                anchor = new AnchorFfi { x = anchor_x, y = anchor_y },
                pivot = new AnchorFfi { x = pivot_x, y = pivot_y },
                offset = new OffsetFfi { top = offset_top, bottom = offset_bottom, left = offset_left, right = offset_right },
                size = new SizeFfi { height = size_height, width = size_width },
                children_json = children_json,
                panel_json = panel_json
            };
            
            var log_str = $"Manually read struct: id={id_ptr.ToInt64():X}, bg={bg_ptr.ToInt64():X}, children={children_json.ToInt64():X}, panelJson={panel_json.ToInt64():X}";
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
            
            var panel_info = $"Panel ID='{pid}', Background='{bgFinal}'";
            try { System.IO.File.AppendAllText("E:\\workspace\\test_log.txt", panel_info + "\n"); } catch { }

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
                                else if (contentType == "entityStringValue" || contentType == "entityTextValue")
                                {
                                    var contentName = contentProp.TryGetProperty("name", out var cn) ? cn.GetString() : null;
                                    var contentEntityId = contentProp.TryGetProperty("entityId", out var ei) ? ei.GetString() : null;
                                    if (contentName != null)
                                    {
                                        panel.Content = new EntityTextValueContent(contentName, contentAlign, contentEntityId);
                                    }
                                }
                                else if (contentType == "constantNumber")
                                {
                                    var contentValue = contentProp.TryGetProperty("value", out var cv) ? cv.GetDouble() : 0.0;
                                    panel.Content = new ConstantNumberContent(contentValue, contentAlign);
                                }
                                else if (contentType == "entityNumberValue")
                                {
                                    var contentName = contentProp.TryGetProperty("name", out var cn) ? cn.GetString() : null;
                                    var contentEntityId = contentProp.TryGetProperty("entityId", out var ei) ? ei.GetString() : null;
                                    if (contentName != null)
                                    {
                                        panel.Content = new EntityNumberValueContent(contentName, contentAlign, contentEntityId);
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
                                else if (contentType == "entityStringValue" || contentType == "entityTextValue")
                                {
                                    var contentName = contentProp.TryGetProperty("name", out var cn) ? cn.GetString() : null;
                                    var contentEntityId = contentProp.TryGetProperty("entityId", out var ei) ? ei.GetString() : null;
                                    if (contentName != null)
                                    {
                                        child.Content = new EntityTextValueContent(contentName, contentAlign, contentEntityId);
                                    }
                                }
                                else if (contentType == "constantNumber")
                                {
                                    var contentValue = contentProp.TryGetProperty("value", out var cv) ? cv.GetDouble() : 0.0;
                                    child.Content = new ConstantNumberContent(contentValue, contentAlign);
                                }
                                else if (contentType == "entityNumberValue")
                                {
                                    var contentName = contentProp.TryGetProperty("name", out var cn) ? cn.GetString() : null;
                                    var contentEntityId = contentProp.TryGetProperty("entityId", out var ei) ? ei.GetString() : null;
                                    if (contentName != null)
                                    {
                                        child.Content = new EntityNumberValueContent(contentName, contentAlign, contentEntityId);
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
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[GetPanelById] Exception processing panel: {ex.Message}");
            return default(Panel);
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

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    private struct AnchorFfi { public float x; public float y; }

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    private struct OffsetFfi
    {
        public float top;
        public float bottom;
        public float left;
        public float right;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    private struct SizeFfi { public float height; public float width; }

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
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
            // Strip control characters (e.g. ANSI escape 0x1B) but keep common whitespace like CR/LF/TAB.
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

    public static void emitAction(string action) => runtime_emit_action(action);

    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
    private static extern double runtime_run_iteration(double tickRateInSec);


    public static double RunIteration(double tickRateInSec = 0) {
        userLogCallback?.Invoke("DEBUG: RunIteration called with tickRateInSec=" + tickRateInSec);
        var elapsedTime = runtime_run_iteration(tickRateInSec);
        return elapsedTime;
    }

    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
    private static extern void runtime_set_game_time(ulong ms);

    public static void setGameTime(ulong ms) {
        runtime_set_game_time(ms);
    }

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
