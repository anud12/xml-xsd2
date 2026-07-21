using System.Text.Json;

namespace NewGameProject.Module;

static class ContentParser
{
    internal static Runtime.PanelContent? Parse(JsonElement elem)
    {
        var type = Extract.String(elem, "type");
        var align = Extract.String(elem, "align") ?? "center";

        if (type == "constant")
        {
            var value = Extract.String(elem, "value");
            return value != null ? new Runtime.ConstantTextContent(value, align) : null;
        }

        if (type == "entityTextValue" || type == "entityStringValue")
        {
            var name = Extract.String(elem, "name");
            var entityId = Extract.String(elem, "entityId");
            return name != null ? new Runtime.EntityTextValueContent(name, align, entityId) : null;
        }

        if (type == "constantNumber")
        {
            var value = Extract.Double(elem, "value") ?? 0.0;
            return new Runtime.ConstantNumberContent(value, align);
        }

        if (type == "entityNumberValue")
        {
            var name = Extract.String(elem, "name");
            var entityId = Extract.String(elem, "entityId");
            return name != null ? new Runtime.EntityNumberValueContent(name, align, entityId) : null;
        }

        if (type == "containerListView")
        {
            var containerId = Extract.String(elem, "containerId");
            if (containerId != null)
            {
                var vertical = Extract.Bool(elem, "vertical") ?? true;
                var content = new Runtime.ContainerListViewContent(containerId, vertical);

                if (elem.TryGetProperty("__templateResults", out var results) && results.ValueKind == JsonValueKind.Array)
                {
                    var parsedResults = new List<Runtime.PanelContent>();
                    foreach (var item in results.EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.String)
                        {
                            var jsonStr = item.GetString();
                            if (jsonStr != null)
                            {
                                using var doc = JsonDocument.Parse(jsonStr);
                                var parsed = Parse(doc.RootElement);
                                if (parsed != null)
                                    parsedResults.Add(parsed);
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

    internal static Runtime.PanelContent? ParseJson(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return Parse(doc.RootElement);
    }
}
