using System.Text.Json;
using NewGameProject.Runtime;
using NewGameProject.UI;

namespace NewGameProject.Module;

/// <summary>
/// The C# side of the .ui node store. Mirrors the Rust <c>UI_NODES</c>
/// store (kind + options + children per id) from the panels the Jint
/// shim collects, so the Godot <see cref="UiWindow"/> tree can paint the
/// declared UI even when the Rust runtime UI host is unavailable.
/// Also seeds the C# file/animation/action stores that <see cref="UiState"/>
/// and <see cref="RuntimeInterop"/> consult first.
/// </summary>
public static class PanelNodeStore
{
    static readonly List<UiNodeData> _nodes = new();
    static readonly Dictionary<string, byte[]> _extraFiles = new();
    static readonly Dictionary<string, string> _animations = new();
    static readonly Dictionary<string, List<Action>> _actionHandlers = new();

    public static void Clear()
    {
        _nodes.Clear();
        _extraFiles.Clear();
        _animations.Clear();
        _registeredIds.Clear();
        _actionHandlers.Clear();
    }

    /// <summary>
    /// Registers a C#-side action handler, called by
    /// <see cref="ModuleEngine"/> when a module declares an action.
    /// </summary>
    public static void RegisterActionHandler(string name, Action apply)
    {
        if (!_actionHandlers.TryGetValue(name, out var list))
            _actionHandlers[name] = list = new List<Action>();
        list.Add(apply);
    }

    /// <summary>
    /// True when a C#-side handler exists for the action.
    /// </summary>
    public static bool HasHandlerFor(string name)
        => _actionHandlers.TryGetValue(name, out var h) && h.Count > 0;

    /// <summary>
    /// Runs every C#-side handler registered for the action. Returns true
    /// when at least one handler ran.
    /// </summary>
    public static bool TryEmitAction(string name)
    {
        if (!_actionHandlers.TryGetValue(name, out var handlers)
            || handlers.Count == 0)
            return false;
        foreach (var h in handlers)
            h();
        return true;
    }

    /// <summary>
    /// C#-side files synthesized here (frame_N / hover_N aliases), not part
    /// of the native archive.
    /// </summary>
    public static IReadOnlyDictionary<string, byte[]> GetFiles() => _extraFiles;

    public static string? GetAnimationJson(string name)
    {
        _animations.TryGetValue(name, out var json);
        return json;
    }

    public static void RegisterAll(
        Runtime.Panel[] panels,
        IReadOnlyDictionary<string, byte[]> archiveFiles)
    {
        _nodes.Clear();
        _extraFiles.Clear();
        _registeredIds.Clear();
        foreach (var p in panels)
            RegisterPanel(p, archiveFiles);
    }

    static readonly HashSet<string> _registeredIds = new();

    static void RegisterPanel(
        Runtime.Panel p,
        IReadOnlyDictionary<string, byte[]> archiveFiles)
    {
        if (!string.IsNullOrEmpty(p.Id) && !_registeredIds.Add(p.Id)) return;
        var opts = new Dictionary<string, object?>();
        if (p.Size.Width > 0)
            opts["width"] = p.Size.Width;
        if (p.Size.Height > 0)
            opts["height"] = p.Size.Height;
        if (p.Offset.left != 0 || p.Offset.top != 0)
        {
            opts["x"] = p.Offset.left;
            opts["y"] = p.Offset.top;
        }
        // The Jint shim always resolves an anchor (defaulting to center), so
        // this is meaningful for every window() panel; x/y take precedence in
        // UiWindow when present.
        opts["anchor"] = new Dictionary<string, object?>
        {
            ["x"] = p.Anchor.X,
            ["y"] = p.Anchor.Y
        };
        if (p.BackgroundAnimation is { Frames.Length: > 0 } anim)
        {
            // Prefer the fixture-registered animation name (resolved from the
            // { name, duration } background reference by the hostApi shim) so
            // UiState.GetAnimation finds the definition; fall back to a
            // per-panel synthetic name.
            var animName = !string.IsNullOrEmpty(p.Background) && _animations.ContainsKey(p.Background)
                ? p.Background
                : $"anim_{p.Id}";
            try { System.IO.File.AppendAllText(@"C:\Users\acriha\AppData\Local\Temp\opencode\dbg-anim.log", $"[PNODE] {p.Id} bg={p.Background} animName={animName} frames={anim.Frames.Length}\n"); } catch { }
            var frameNames = new List<string>();
            var frameJsons = new List<string>();
            for (int i = 0; i < anim.Frames.Length; i++)
            {
                var origPath = ResolveFramePath(anim.Frames[i], archiveFiles);
                var frameName = !string.IsNullOrEmpty(origPath) ? origPath : $"{animName}_frame_{i}";
                frameNames.Add(frameName);
                frameJsons.Add($"{{\"sprite\": \"{frameName}\"}}");
            }
            _animations[animName] = JsonDocument.Parse(
                $"{{\"name\": \"{animName}\", \"duration\": {anim.DurationTicks}, "
                + $"\"loop\": {(anim.Loop ? "true" : "false")}, "
                + $"\"frames\": [{string.Join(",", frameJsons)}]}}")
                .RootElement.GetRawText();
            if (frameNames.Count == 1)
            {
                opts["background"] = frameNames[0];
            }
            else
            {
                opts["background"] = new Dictionary<string, object?>
                {
                    ["name"] = animName,
                    ["duration"] = anim.DurationTicks,
                    ["loop"] = anim.Loop
                };
            }
        }
        else if (!string.IsNullOrEmpty(p.Background))
        {
            opts["background"] = p.Background;
        }
        // onHover: an outline (texture+thickness), a background swap, an
        // emit action, and/or stopPropagation — any combination.
        var hover = new Dictionary<string, object?>();
        if (p.Hover is { } hv && !string.IsNullOrEmpty(hv.Texture))
        {
            var hoverPath = $"hover_{p.Id}";
            if (archiveFiles.TryGetValue(hv.Texture, out var hoverData))
            {
                // Stored under an alias because the hover texture may be a JS
                // animation object name (not an archive path); UiWindow looks
                // the texture up in the archive by this path.
                _extraFiles[hoverPath] = hoverData;
                hover["texture"] = hoverPath;
            }
            else
            {
                hover["texture"] = hv.Texture;
            }
            hover["thickness"] = hv.Thickness;
        }
        if (!string.IsNullOrEmpty(p.HoverBackground))
            hover["background"] = p.HoverBackground;
        if (p.HoverEmitAction is { } ea)
            hover["emitAction"] = ea;
        if (p.HoverStopPropagation)
            hover["stopPropagation"] = true;
        if (hover.Count > 0)
            opts["onHover"] = hover;
        if (p.OnClick is { } oc)
            opts["onClick"] = oc.ActionName;

        var children = (p.Children ?? Array.Empty<Runtime.Panel>())
            .Where(c => !string.IsNullOrEmpty(c.Id))
            .Select(c => c.Id)
            .ToList();

        _nodes.Add(ToUiNodeData(p, opts, children));

        foreach (var c in p.Children ?? Array.Empty<Runtime.Panel>())
            if (!string.IsNullOrEmpty(c.Id))
                RegisterPanel(c, archiveFiles);
    }

    static UiNodeData ToUiNodeData(
        Runtime.Panel p,
        Dictionary<string, object?> opts,
        List<string> children)
    {
        try { System.IO.File.AppendAllText(@"C:\Users\acriha\AppData\Local\Temp\opencode\dbg-anim.log", $"[NODE] {p.Id} bgOpt={(opts.ContainsKey("background") ? System.Text.Json.JsonSerializer.Serialize(opts["background"]) : "none")}\n"); } catch { }
        if (p.Content is { } content)
        {
            switch (content)
            {
                case Runtime.ConstantTextContent ctc:
                    return new UiNodeData
                    {
                        Id = p.Id,
                        Kind = UiNodeKind.Text,
                        Value = ctc.Value,
                        OptionsJson = JsonSerializer.Serialize(new Dictionary<string, object?>
                        {
                            ["align"] = ctc.Align
                        })
                    };
                case Runtime.EntityTextValueContent etvc:
                    return new UiNodeData
                    {
                        Id = p.Id,
                        Kind = UiNodeKind.Field,
                        Value = ResolveEntityValue(etvc.EntityId, etvc.Name, isNumber: false),
                        OptionsJson = JsonSerializer.Serialize(new Dictionary<string, object?>
                        {
                            ["entity"] = etvc.EntityId ?? "",
                            ["name"] = etvc.Name,
                            ["map"] = "text",
                            ["align"] = etvc.Align
                        })
                    };
                case Runtime.EntityNumberValueContent envc:
                    return new UiNodeData
                    {
                        Id = p.Id,
                        Kind = UiNodeKind.Field,
                        Value = ResolveEntityValue(envc.EntityId, envc.Name, isNumber: true),
                        OptionsJson = JsonSerializer.Serialize(new Dictionary<string, object?>
                        {
                            ["entity"] = envc.EntityId ?? "",
                            ["name"] = envc.Name,
                            ["map"] = "number",
                            ["align"] = envc.Align
                        })
                    };
                case Runtime.ContainerListViewContent clvc:
                    return new UiNodeData
                    {
                        Id = p.Id,
                        Kind = UiNodeKind.Division,
                        Value = "",
                        OptionsJson = JsonSerializer.Serialize(opts),
                        Children = children
                    };
                case Runtime.ConstantNumberContent cnc:
                    return new UiNodeData
                    {
                        Id = p.Id,
                        Kind = UiNodeKind.Text,
                        Value = cnc.Value.ToString(),
                        OptionsJson = JsonSerializer.Serialize(new Dictionary<string, object?>
                        {
                            ["align"] = cnc.Align
                        })
                    };
                default:
                    break;
            }
        }
        if (p.Layout != null)
        {
            var rowFirst = p.Layout.Value.RowFirst ?? false;
            var layoutOpts = new Dictionary<string, object?>(opts)
            {
                ["layout"] = rowFirst ? "row" : "column"
            };
            return new UiNodeData
            {
                Id = p.Id,
                Kind = UiNodeKind.Division,
                Value = "",
                OptionsJson = JsonSerializer.Serialize(layoutOpts),
                Children = children
            };
        }

        return new UiNodeData
        {
            Id = p.Id,
            Kind = UiNodeKind.Window,
            OptionsJson = JsonSerializer.Serialize(opts),
            Children = children
        };
    }

    static string ResolveEntityValue(string? entityId, string name, bool isNumber)
    {
        if (string.IsNullOrEmpty(entityId)) return "";
        try
        {
            return isNumber
                ? RuntimeInterop.ReadEntityNumberValue(entityId, name)
                : RuntimeInterop.ReadEntityTextValue(entityId, name);
        }
        catch
        {
            return "";
        }
    }

    static string? ResolveFramePath(
        string frame,
        IReadOnlyDictionary<string, byte[]> archiveFiles)
    {
        if (string.IsNullOrEmpty(frame)) return null;
        if (archiveFiles.ContainsKey(frame)) return frame;
        try
        {
            using var doc = JsonDocument.Parse(frame);
            var root = doc.RootElement;
            if (root.TryGetProperty("name", out var n) && n.GetString() is { } name)
                return name;
            if (root.TryGetProperty("frames", out var frames)
                && frames.ValueKind == JsonValueKind.Array
                && frames.GetArrayLength() > 0)
            {
                var f0 = frames[0];
                if (f0.ValueKind == JsonValueKind.String) return f0.GetString();
                if (f0.ValueKind == JsonValueKind.Object
                    && f0.TryGetProperty("sprite", out var sp))
                {
                    if (sp.ValueKind == JsonValueKind.String) return sp.GetString();
                    if (sp.ValueKind == JsonValueKind.Object
                        && sp.TryGetProperty("name", out var sn)) return sn.GetString();
                }
            }
        }
        catch { }
        return null;
    }

    static byte[]? ResolveFrame(
        string frame,
        IReadOnlyDictionary<string, byte[]> archiveFiles)
    {
        if (archiveFiles.TryGetValue(frame, out var data))
            return data;
        try { System.IO.File.AppendAllText(@"C:\Users\acriha\AppData\Local\Temp\opencode\dbg-anim.log", $"[RESOLVE] frame={frame} not in archive, trying JSON\n"); } catch { }
        if (string.IsNullOrEmpty(frame)) return null;
        try
        {
            using var doc = JsonDocument.Parse(frame);
            var root = doc.RootElement;
            if (root.TryGetProperty("name", out var n)
                && n.GetString() is { } name
                && archiveFiles.TryGetValue(name, out var named))
                return named;
            if (root.TryGetProperty("frames", out var frames)
                && frames.ValueKind == JsonValueKind.Array
                && frames.GetArrayLength() > 0)
            {
                var f0 = frames[0];
                string? path = null;
                if (f0.ValueKind == JsonValueKind.String)
                    path = f0.GetString();
                else if (f0.ValueKind == JsonValueKind.Object
                    && f0.TryGetProperty("sprite", out var sp))
                {
                    path = sp.ValueKind == JsonValueKind.String
                        ? sp.GetString()
                        : sp.TryGetProperty("name", out var sn) ? sn.GetString() : null;
                }
                if (path is not null && archiveFiles.TryGetValue(path, out var d))
                    return d;
            }
        }
        catch
        {
            return null;
        }
        return null;
    }

    public static List<UiNodeData> Fetch()
    {
        // Recompute field values from the live entity store so entity value
        // changes surface without a structural delta.
        foreach (var n in _nodes)
        {
            if (n.Kind != UiNodeKind.Field) continue;
            string? entity = null;
            string? name = null;
            var map = "text";
            try
            {
                using var doc = JsonDocument.Parse(n.OptionsJson);
                var root = doc.RootElement;
                if (root.TryGetProperty("entity", out var e)) entity = e.GetString();
                if (root.TryGetProperty("name", out var nm)) name = nm.GetString();
                if (root.TryGetProperty("map", out var m)) map = m.GetString() ?? "text";
            }
            catch { continue; }
            if (string.IsNullOrEmpty(entity) || string.IsNullOrEmpty(name)) continue;
            var value = map == "number"
                ? SafeReadNumber(entity, name)
                : SafeReadText(entity, name);
            if (value != n.Value) n.Value = value;
        }
        return _nodes.ToList();
    }

    static string SafeReadText(string entity, string name)
    {
        try { return RuntimeInterop.ReadEntityTextValue(entity, name); }
        catch { return ""; }
    }

    static string SafeReadNumber(string entity, string name)
    {
        try { return RuntimeInterop.ReadEntityNumberValue(entity, name); }
        catch { return ""; }
    }
}
