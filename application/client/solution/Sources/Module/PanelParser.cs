using System.Text.Json;
using NewGameProject.Runtime;

namespace NewGameProject.Module;

static class PanelParser
{
    static MapLayerBinding[]? _lastSpriteMapLayers;

    public static bool TryParse(string json, out Runtime.Panel panel)
    {
        panel = default;
        try
        {
            using var doc = JsonDocument.Parse(json);
            panel = Parse(doc.RootElement);
            return true;
        }
        catch { return false; }
    }

    static Runtime.Panel Parse(JsonElement e)
    {
        var p = new Runtime.Panel
        {
            Id = Extract.String(e, "id") ?? "",
            Background = ExtractTextureFromSprite(e, "background")
        };

        if (e.TryGetProperty("background", out var bgVal) && bgVal.ValueKind == JsonValueKind.Object
            && bgVal.TryGetProperty("frames", out var frames) && frames.ValueKind == JsonValueKind.Array
            && frames.GetArrayLength() > 0)
        {
            var framePaths = new List<string>();
            foreach (var frame in frames.EnumerateArray())
            {
                if (frame.ValueKind == JsonValueKind.Object && frame.TryGetProperty("sprite", out var sprite))
                {
                    if (sprite.ValueKind == JsonValueKind.String)
                        framePaths.Add(sprite.GetString() ?? "");
                    else if (sprite.ValueKind == JsonValueKind.Object && sprite.TryGetProperty("name", out var spriteName))
                        framePaths.Add(spriteName.GetString() ?? "");
                    else if (sprite.ValueKind == JsonValueKind.Object && sprite.TryGetProperty("__spriteMap", out var isMap) && isMap.GetBoolean())
                    {
                        // This is a sprite map — extract map path and layer bindings for CPU composition
                        var mapPath = ResolveSpriteMap(sprite, out var layers);
                        framePaths.Add(mapPath);
                        if (layers != null && layers.Length > 0 && _lastSpriteMapLayers == null)
                            _lastSpriteMapLayers = layers;
                    }
                }
            }
            if (framePaths.Count > 0)
            {
                var duration = Extract.Int(bgVal, "duration") ?? 1;
                var loop = Extract.Bool(bgVal, "loop") ?? false;
                p.BackgroundAnimation = new Runtime.AnimationSequence
                {
                    Frames = framePaths.ToArray(),
                    DurationTicks = duration,
                    Loop = loop,
                    SpriteMapLayers = _lastSpriteMapLayers
                };
                _lastSpriteMapLayers = null;
            }
        }

        if (e.TryGetProperty("anchor", out var a))
            p.Anchor = new Runtime.Vector2
            { X = Extract.Float(a, "x") ?? 0f, Y = Extract.Float(a, "y") ?? 0f };

        if (e.TryGetProperty("offset", out var o))
            p.Offset = new Runtime.Offset
            {
                top = Extract.Float(o, "top") ?? 0f, bottom = Extract.Float(o, "bottom") ?? 0f,
                left = Extract.Float(o, "left") ?? 0f, right = Extract.Float(o, "right") ?? 0f
            };

        if (e.TryGetProperty("size", out var s))
            p.Size = new Runtime.Size
            { Height = Extract.Float(s, "height") ?? 0f, Width = Extract.Float(s, "width") ?? 0f };

        if (e.TryGetProperty("hover", out var h) && h.ValueKind == JsonValueKind.Object)
        {
            var t = ExtractTextureFromSprite(h, "texture");
            if (t != null)
                p.Hover = new Runtime.Hover
                { Texture = t, Thickness = Extract.Int(h, "thickness") ?? 0 };
            if (Extract.String(h, "emitAction") is { } ea)
                p.HoverEmitAction = ea;
            p.HoverStopPropagation = Extract.Bool(h, "stopPropagation") ?? false;
            p.HoverBackground = Extract.String(h, "background");
        }

        if (e.TryGetProperty("onClick", out var c) && c.ValueKind == JsonValueKind.Object)
        {
            if (Extract.String(c, "type") == "emitAction")
            {
                var name = Extract.String(c, "actionName");
                if (name != null)
                    p.OnClick = new Runtime.PanelOnClickHandler { ActionName = name };
            }
        }

        p.Surface = Extract.Bool(e, "surface") ?? false;

        if (e.TryGetProperty("content", out var ct) && ct.ValueKind == JsonValueKind.Object)
            p.Content = ContentParser.Parse(ct);

        if (e.TryGetProperty("layout", out var l) && l.ValueKind == JsonValueKind.Object)
            p.Layout = LayoutParser.Parse(l);

        if (e.TryGetProperty("children", out var ch) && ch.ValueKind == JsonValueKind.Array)
        {
            // Children may be nested panel objects (parsed inline) or ids of
            // panels registered earlier in the module (linked after all
            // panels are parsed in ToPanels).
            var children = new List<Runtime.Panel>();
            var childIds = new List<string>();
            foreach (var child in ch.EnumerateArray())
            {
                if (child.ValueKind == JsonValueKind.Object)
                    children.Add(Parse(child));
                else if (child.ValueKind == JsonValueKind.String)
                    childIds.Add(child.GetString() ?? "");
            }
            p.Children = children.ToArray();
            if (childIds.Count > 0)
                p.ChildIds = childIds.ToArray();
        }

        return p;
    }

    static string ResolveSpriteMap(JsonElement spriteObj, out MapLayerBinding[]? layers)
    {
        var mapPath = Extract.String(spriteObj, "map") ?? "";
        layers = null;

        if (spriteObj.TryGetProperty("layers", out var layersArr) && layersArr.ValueKind == JsonValueKind.Array)
        {
            var layerList = new List<MapLayerBinding>();
            foreach (var layerEl in layersArr.EnumerateArray())
            {
                layerList.Add(new MapLayerBinding
                {
                    TiffLayerName = Extract.String(layerEl, "layer") ?? "",
                    SkinPath = Extract.String(layerEl, "texture") ?? "",
                });
            }
            layers = layerList.ToArray();
        }

        return mapPath;
    }

    static string? ExtractTextureFromSprite(JsonElement e, string prop)
    {
        var direct = Extract.String(e, prop);
        if (direct != null)
            return direct;

        if (e.TryGetProperty(prop, out var val) && val.ValueKind == JsonValueKind.Object)
        {
            if (val.TryGetProperty("frames", out var frames) && frames.ValueKind == JsonValueKind.Array
                && frames.GetArrayLength() > 0)
            {
                var firstFrame = frames[0];
                if (firstFrame.ValueKind == JsonValueKind.Object && firstFrame.TryGetProperty("sprite", out var sprite))
                {
                    if (sprite.ValueKind == JsonValueKind.String)
                        return sprite.GetString();
                    if (sprite.ValueKind == JsonValueKind.Object && sprite.TryGetProperty("name", out var spriteName))
                    {
                        return spriteName.GetString();
                    }
                }
            }

            var name = Extract.String(val, "name");
            if (name != null)
                return name;
        }

        return null;
    }
}
