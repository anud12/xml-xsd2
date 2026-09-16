using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace NewGameProject.UI;

/// Reads a Rust `ui::abi` binary slab (see `application/runtime/src/ui/abi.rs`)
/// into `UiNodeData` models. The struct regions are copied out with
/// `Marshal.PtrToStructure`; every `uint` string field is a byte offset into
/// the slab's NUL-terminated arena. Options and field bindings are
/// re-serialized into the flat JSON shape the `UiWindow` readers consume.
public static class UiSlab
{
    public static List<UiNodeData> ReadNodes(IntPtr snapshot)
    {
        if (snapshot == IntPtr.Zero) return new List<UiNodeData>();
        unsafe
        {
            var snap = Marshal.PtrToStructure<UiAbi.UiSnapshot>(snapshot);
            var list = new List<UiNodeData>((int)snap.NodeCount);
            for (uint i = 0; i < snap.NodeCount; i++)
            {
                var node = Marshal.PtrToStructure<UiAbi.UiNode>(snap.Nodes + (IntPtr)(i * (ulong)Marshal.SizeOf<UiAbi.UiNode>()));
                list.Add(ToNodeData(node, snap.Strings));
            }
            return list;
        }
    }

    public static UiDelta? ReadDelta(IntPtr delta)
    {
        if (delta == IntPtr.Zero) return null;
        unsafe
        {
            var d = Marshal.PtrToStructure<UiAbi.UiDelta>(delta);
            var result = new UiDelta();
            for (uint i = 0; i < d.OpCount; i++)
            {
                var op = Marshal.PtrToStructure<UiAbi.UiDeltaOp>(d.Ops + (IntPtr)(i * (ulong)Marshal.SizeOf<UiAbi.UiDeltaOp>()));
                var node = ToNodeData(op.Node, d.Strings);
                result.Ops.Add(op.Op == UiAbi.OpRemove
                    ? new UiDeltaOp { Op = "remove", Node = node, Id = node.Id }
                    : new UiDeltaOp
                    {
                        Op = op.Op == UiAbi.OpUpdate ? "update" : "add",
                        Node = node
                    });
            }
            return result;
        }
    }

    static unsafe UiNodeData ToNodeData(UiAbi.UiNode node, IntPtr arena)
    {
        var data = new UiNodeData
        {
            Id = Str(arena, node.Id),
            Kind = (UiNodeKind)node.Kind,
            Value = Str(arena, node.Value),
            Src = Str(arena, node.Src),
            OptionsJson = SerializeOptions(node.Opt, arena),
            BindingJson = SerializeBinding(node.Binding, arena)
        };
        if (node.ChildCount > 0 && node.Children != IntPtr.Zero)
        {
            for (uint i = 0; i < node.ChildCount; i++)
            {
                var off = *(uint*)(node.Children + i * sizeof(uint));
                data.Children.Add(Str(arena, off));
            }
        }
        return data;
    }

    static unsafe string Str(IntPtr arena, uint offset)
    {
        if (offset == UiAbi.NoStr) return "";
        return Marshal.PtrToStringAnsi(arena + (int)offset) ?? "";
    }

    // -------------------------------------------------------------------
    // Options re-serialization (flat reader shape)
    // -------------------------------------------------------------------

    static string SerializeOptions(UiAbi.UiNodeOptions o, IntPtr arena)
    {
        var sb = new StringBuilder();
        bool first = true;
        void Add(string key, string raw)
        {
            if (!first) sb.Append(',');
            sb.Append('"').Append(key).Append("\":").Append(raw);
            first = false;
        }
        void AddStr(string key, uint off)
        {
            if (off != UiAbi.NoStr) Add(key, JsonEscaped(Str(arena, off)));
        }

        if (o.HasXY != 0)
        {
            Add("x", Num(o.X));
            Add("y", Num(o.Y));
        }
        if (o.HasSize != 0)
        {
            Add("width", Num(o.Width));
            Add("height", Num(o.Height));
        }
        AddStr("anchor", o.Anchor);
        AddStr("align", o.Align);
        var layout = SerializeLayout(o.Layout, arena);
        if (layout != null) Add("layout", layout);
        var background = SerializeBackground(o.Background, arena);
        if (background != null) Add("background", background);
        if (o.HasBorder != 0)
        {
            var border = new StringBuilder("{");
            if (o.BorderTexture != UiAbi.NoStr)
                border.Append("\"texture\":").Append(JsonEscaped(Str(arena, o.BorderTexture))).Append(',');
            border.Append("\"width\":").Append(Num(o.BorderWidth));
            border.Append('}');
            Add("border", border.ToString());
        }
        var click = SerializeOnClick(o.OnClick, arena);
        if (click != null) Add("onClick", click);
        var hover = SerializeOnHover(o.OnHover, arena);
        if (hover != null) Add("onHover", hover);
        AddStr("container", o.Container);
        if (o.Resizable != 0)
        {
            if (o.ResizableKeepAspect != 0)
                Add("resizable", "{\"keepAspectRatio\":true}");
            else
                Add("resizable", "true");
        }
        if (first) return "{}";
        return "{" + sb + "}";
    }

    static string SerializeLayout(UiAbi.UiLayout l, IntPtr arena)
    {
        switch (l.Kind)
        {
            case UiAbi.LayoutColumn: return "\"column\"";
            case UiAbi.LayoutRow:
                if (l.EqualTracks > 0) return Num(l.EqualTracks);
                return "\"row\"";
            case UiAbi.LayoutTracks:
                var sb = new StringBuilder("{");
                sb.Append("\"rowFirst\":").Append(l.RowFirst != 0 ? "true" : "false");
                if (l.GapRow != 0f || l.GapCol != 0f)
                    sb.Append(",\"gap\":{\"row\":").Append(Num(l.GapRow))
                      .Append(",\"column\":").Append(Num(l.GapCol)).Append('}');
                if (l.ColCount > 0)
                    sb.Append(",\"columns\":[").Append(Tracks(l.ColTracks, l.ColCount, arena)).Append(']');
                if (l.RowCount > 0)
                    sb.Append(",\"rows\":[").Append(Tracks(l.RowTracks, l.RowCount, arena)).Append(']');
                sb.Append('}');
                return sb.ToString();
            default: return null;
        }
    }

    static unsafe string Tracks(IntPtr tracks, uint count, IntPtr arena)
    {
        var sb = new StringBuilder();
        for (uint i = 0; i < count; i++)
        {
            var t = *(UiAbi.UiTrack*)(tracks + i * (IntPtr)Marshal.SizeOf<UiAbi.UiTrack>());
            if (i > 0) sb.Append(',');
            var hasMin = (t.Flags & UiAbi.TrackHasMin) != 0;
            var hasMax = (t.Flags & UiAbi.TrackHasMax) != 0;
            // Numeric track: min==max, no scale -> a plain fixed number.
            if (hasMin && hasMax && t.Scale == 0f)
            {
                sb.Append(Num(t.Min));
                continue;
            }
            var o = new StringBuilder("{");
            bool tFirst = true;
            void TAdd(string key, string raw)
            {
                if (!tFirst) o.Append(',');
                o.Append('"').Append(key).Append("\":").Append(raw);
                tFirst = false;
            }
            if (hasMin) TAdd("min", Num(t.Min));
            if (hasMax) TAdd("max", Num(t.Max));
            if (t.Scale != 0f) TAdd("scale", Num(t.Scale));
            o.Append('}');
            sb.Append(o);
        }
        return sb.ToString();
    }

    static string SerializeBackground(UiAbi.UiBackground b, IntPtr arena)
    {
        switch (b.Kind)
        {
            case UiAbi.BgStatic:
                return JsonEscaped(Str(arena, b.Path));
            case UiAbi.BgAnimation:
                var anim = new StringBuilder("{\"name\":");
                anim.Append(JsonEscaped(Str(arena, b.Name)));
                if (b.Duration > 0f) anim.Append(",\"duration\":").Append(Num(b.Duration));
                if (b.Loop != 0) anim.Append(",\"loop\":true");
                anim.Append('}');
                return anim.ToString();
            case UiAbi.BgSpriteMap:
                var map = new StringBuilder("{\"kind\":\"spriteMap\",\"map\":");
                map.Append(JsonEscaped(Str(arena, b.Map)));
                if (b.LayerCount > 0)
                {
                    unsafe
                    {
                        map.Append(",\"layers\":[");
                        for (uint i = 0; i < b.LayerCount; i++)
                        {
                            var layer = *(UiAbi.UiMapLayer*)(b.Layers + i * (IntPtr)Marshal.SizeOf<UiAbi.UiMapLayer>());
                            if (i > 0) map.Append(',');
                            map.Append("{\"texture\":").Append(JsonEscaped(Str(arena, layer.Texture)))
                              .Append(",\"layer\":").Append(layer.Layer).Append('}');
                        }
                        map.Append(']');
                    }
                }
                map.Append('}');
                return map.ToString();
            default: return null;
        }
    }

    static unsafe string SerializeOnClick(UiAbi.UiOnClick c, IntPtr arena)
    {
        if (c.Kind == UiAbi.ClickAction) return JsonEscaped(Str(arena, c.Action));
        if (c.Kind != UiAbi.ClickSteps) return null;
        var sb = new StringBuilder("{\"steps\":[");
        for (uint i = 0; i < c.StepCount; i++)
        {
            var step = *(UiAbi.UiClickStep*)(c.Steps + i * (IntPtr)Marshal.SizeOf<UiAbi.UiClickStep>());
            if (i > 0) sb.Append(',');
            sb.Append("{\"action\":").Append(JsonEscaped(Str(arena, step.Action)));
            if (step.ArgCount > 0)
            {
                sb.Append(",\"args\":{");
                for (uint j = 0; j < step.ArgCount; j++)
                {
                    var arg = *(UiAbi.UiArgPair*)(step.Args + j * (IntPtr)Marshal.SizeOf<UiAbi.UiArgPair>());
                    if (j > 0) sb.Append(',');
                    sb.Append(JsonEscaped(Str(arena, arg.Key))).Append(":");
                    switch (arg.Vt)
                    {
                        case UiAbi.ArgNumber:
                            sb.Append(Num(ParseFloat(Str(arena, arg.Value))));
                            break;
                        case UiAbi.ArgCursor:
                            sb.Append("{\"__cursor\":").Append(JsonEscaped(Str(arena, arg.Value))).Append('}');
                            break;
                        default:
                            sb.Append(JsonEscaped(Str(arena, arg.Value)));
                            break;
                    }
                }
                sb.Append('}');
            }
            sb.Append('}');
        }
        sb.Append("]}");
        return sb.ToString();
    }

    static string SerializeOnHover(UiAbi.UiOnHover h, IntPtr arena)
    {
        var hasAny = h.StopPropagation != 0
            || h.EmitAction != UiAbi.NoStr
            || h.Background != UiAbi.NoStr
            || h.Texture != UiAbi.NoStr;
        if (!hasAny) return null;
        var sb = new StringBuilder();
        bool first = true;
        void Add(string key, string raw)
        {
            if (!first) sb.Append(',');
            sb.Append('"').Append(key).Append("\":").Append(raw);
            first = false;
        }
        if (h.EmitAction != UiAbi.NoStr) Add("emitAction", JsonEscaped(Str(arena, h.EmitAction)));
        if (h.StopPropagation != 0) Add("stopPropagation", "true");
        if (h.Background != UiAbi.NoStr) Add("background", JsonEscaped(Str(arena, h.Background)));
        if (h.Texture != UiAbi.NoStr)
        {
            Add("texture", JsonEscaped(Str(arena, h.Texture)));
            if (h.Thickness != 0f) Add("thickness", Num(h.Thickness));
        }
        return "{" + sb + "}";
    }

    static string SerializeBinding(UiAbi.UiBinding b, IntPtr arena)
    {
        var hasEntity = b.Entity != UiAbi.NoStr;
        var hasName = b.Name != UiAbi.NoStr;
        var hasFallback = b.Fallback != UiAbi.NoStr;
        if (!hasEntity && !hasName && !hasFallback) return "";
        var sb = new StringBuilder("{");
        bool first = true;
        void Add(string key, string raw)
        {
            if (!first) sb.Append(',');
            sb.Append('"').Append(key).Append("\":").Append(raw);
            first = false;
        }
        if (hasEntity) Add("entity", JsonEscaped(Str(arena, b.Entity)));
        var mapName = b.Map == UiAbi.MapNumber ? "number" : b.Map == UiAbi.MapText ? "text" : null;
        if (mapName != null) Add("map", "\"" + mapName + "\"");
        if (hasName) Add("name", JsonEscaped(Str(arena, b.Name)));
        if (hasFallback) Add("fallback", JsonEscaped(Str(arena, b.Fallback)));
        return sb.Append('}').ToString();
    }

    // -------------------------------------------------------------------
    // JSON escaping / number formatting
    // -------------------------------------------------------------------

    static string JsonEscaped(string s)
    {
        if (s == null || s.Length == 0) return "\"\"";
        return "\"" + s
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\b", "\\b")
            .Replace("\f", "\\f")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t") + "\"";
    }

    static string Num(float v)
    {
        var s = v.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
        return s.Length == 0 ? "0" : s;
    }

    static float ParseFloat(string s)
    {
        return float.TryParse(s, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : 0f;
    }
}
