using System.Text.Json;

namespace NewGameProject.Runtime;

static class ContentParser
{
    internal static PanelContent? Parse(JsonElement elem)
    {
        var type = Extract.String(elem, "type");
        var align = Extract.String(elem, "align") ?? "center";

        if (type == "constant")
        {
            var value = Extract.String(elem, "value");
            return value != null ? new ConstantTextContent(value, align) : null;
        }

        if (type == "entityTextValue" || type == "entityStringValue")
        {
            var name = Extract.String(elem, "name");
            var entityId = Extract.String(elem, "entityId");
            return name != null ? new EntityTextValueContent(name, align, entityId) : null;
        }

        if (type == "constantNumber")
        {
            var value = Extract.Double(elem, "value") ?? 0.0;
            return new ConstantNumberContent(value, align);
        }

        if (type == "entityNumberValue")
        {
            var name = Extract.String(elem, "name");
            var entityId = Extract.String(elem, "entityId");
            return name != null ? new EntityNumberValueContent(name, align, entityId) : null;
        }

        if (type == "containerListView")
        {
            var containerId = Extract.String(elem, "containerId");
            if (containerId != null)
            {
                var vertical = Extract.Bool(elem, "vertical") ?? true;
                var content = new ContainerListViewContent(containerId, vertical);

                if (elem.TryGetProperty("__templateResults", out var results) && results.ValueKind == JsonValueKind.Array)
                {
                    var parsedResults = new List<Panel>();
                    foreach (var item in results.EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.String)
                        {
                            var jsonStr = item.GetString();
                            if (jsonStr != null)
                            {
                                var parsed = ParsePanel(jsonStr);
                                if (parsed.HasValue)
                                    parsedResults.Add(parsed.Value);
                            }
                        }
                    }
                    if (parsedResults.Count > 0)
                        content.TemplateResults = parsedResults.ToArray();
                }

                return content;
            }
        }

        return null;
    }

    internal static PanelContent? ParseJson(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return Parse(doc.RootElement);
    }

    internal static Panel? ParsePanel(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var id = Extract.String(root, "id");
        if (id == null)
            return null;

        var panel = new Panel
        {
            Id = id,
            Background = ExtractTexture(root),
        };

        if (root.TryGetProperty("anchor", out var anchorElem) && anchorElem.ValueKind == JsonValueKind.Object)
        {
            panel.Anchor = new Vector2
            {
                X = (float)(Extract.Double(anchorElem, "x") ?? 0.5),
                Y = (float)(Extract.Double(anchorElem, "y") ?? 0.5),
            };
        }

        if (root.TryGetProperty("offset", out var offsetElem) && offsetElem.ValueKind == JsonValueKind.Object)
        {
            panel.Offset = new Offset
            {
                top = (float)(Extract.Double(offsetElem, "top") ?? 0),
                bottom = (float)(Extract.Double(offsetElem, "bottom") ?? 0),
                left = (float)(Extract.Double(offsetElem, "left") ?? 0),
                right = (float)(Extract.Double(offsetElem, "right") ?? 0),
            };
        }

        if (root.TryGetProperty("size", out var sizeElem) && sizeElem.ValueKind == JsonValueKind.Object)
        {
            panel.Size = new Size
            {
                Width = (float)(Extract.Double(sizeElem, "width") ?? 80),
                Height = (float)(Extract.Double(sizeElem, "height") ?? 40),
            };
        }

        if (root.TryGetProperty("content", out var contentElem) && contentElem.ValueKind == JsonValueKind.Object)
        {
            panel.Content = Parse(contentElem);
        }

        return panel;
    }

    static string? ExtractTexture(JsonElement elem)
    {
        var direct = Extract.String(elem, "background");
        if (direct != null)
            return direct;

        if (elem.TryGetProperty("background", out var bg) && bg.ValueKind == JsonValueKind.Object)
        {
            if (bg.TryGetProperty("frames", out var frames) && frames.ValueKind == JsonValueKind.Array
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

            var nameVal = Extract.String(bg, "name");
            if (nameVal != null)
                return nameVal;
        }

        return null;
    }
}
