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

        return null;
    }
}
