using Godot;
using NewGameProject.Runtime;

public partial class ContainerListContentNode : Godot.Container
{
    private ContainerListContent content;
    private VBoxContainer _listContainer;

    public ContainerListContentNode(ContainerListContent content)
    {
        this.content = content;
        Name = "content";
        SetAnchorsPreset(LayoutPreset.FullRect);

        _listContainer = new VBoxContainer
        {
            Name = "listContainer",
        };
        _listContainer.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(_listContainer);

        PopulateList();
    }

    private void PopulateList()
    {
        // Clear existing children
        foreach (Node child in _listContainer.GetChildren())
        {
            child.QueueFree();
        }

        var container = ContainerInterop.GetContainerById(content.ContainerId);
        if (container.Entities == null || container.Entities.Length == 0)
            return;

        for (int i = 0; i < container.Entities.Length; i++)
        {
            string entityId = container.Entities[i];
            string panelJson = RuntimeInterop.InvokeTemplate(
                content.TemplateSource, entityId, i);

            if (string.IsNullOrEmpty(panelJson) || panelJson == "{}")
                continue;

            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(panelJson);
                var root = doc.RootElement;

                var panelData = new NewGameProject.Runtime.Panel
                {
                    Id = root.TryGetProperty("id", out var idProp) ? idProp.GetString() ?? $"item_{i}" : $"item_{i}",
                    Background = root.TryGetProperty("background", out var bgProp) && bgProp.ValueKind != System.Text.Json.JsonValueKind.Null ? bgProp.GetString() : null,
                    Anchor = new NewGameProject.Runtime.Vector2
                    {
                        X = root.TryGetProperty("anchor", out var anc) && anc.TryGetProperty("x", out var ax) ? ax.GetSingle() : 0f,
                        Y = root.TryGetProperty("anchor", out var anc2) && anc2.TryGetProperty("y", out var ay) ? ay.GetSingle() : 0f,
                    },
                    Pivot = new NewGameProject.Runtime.Vector2
                    {
                        X = root.TryGetProperty("pivot", out var piv) && piv.TryGetProperty("x", out var px) ? px.GetSingle() : 0f,
                        Y = root.TryGetProperty("pivot", out var piv2) && piv2.TryGetProperty("y", out var py) ? py.GetSingle() : 0f,
                    },
                    Offset = new NewGameProject.Runtime.Offset
                    {
                        top = root.TryGetProperty("offset", out var off) && off.TryGetProperty("top", out var ot) ? ot.GetSingle() : 0f,
                        bottom = root.TryGetProperty("offset", out var off2) && off2.TryGetProperty("bottom", out var ob) ? ob.GetSingle() : 0f,
                        left = root.TryGetProperty("offset", out var off3) && off3.TryGetProperty("left", out var ol) ? ol.GetSingle() : 0f,
                        right = root.TryGetProperty("offset", out var off4) && off4.TryGetProperty("right", out var or_) ? or_.GetSingle() : 0f,
                    },
                    Size = new NewGameProject.Runtime.Size
                    {
                        Height = root.TryGetProperty("size", out var sz) && sz.TryGetProperty("height", out var sh) ? sh.GetSingle() : 0f,
                        Width = root.TryGetProperty("size", out var sz2) && sz2.TryGetProperty("width", out var sw) ? sw.GetSingle() : 0f,
                    },
                };

                // Parse content
                if (root.TryGetProperty("content", out var contentProp) && contentProp.ValueKind == System.Text.Json.JsonValueKind.Object)
                {
                    var contentType = contentProp.TryGetProperty("type", out var ct) ? ct.GetString() : null;
                    var contentAlign = contentProp.TryGetProperty("align", out var ca) ? ca.GetString() ?? "center" : "center";

                    if (contentType == "constant")
                    {
                        var value = contentProp.TryGetProperty("value", out var cv) ? cv.GetString() : null;
                        if (value != null)
                            panelData.Content = new ConstantTextContent(value, contentAlign);
                    }
                    else if (contentType == "entityStringValue" || contentType == "entityTextValue")
                    {
                        var name = contentProp.TryGetProperty("name", out var cn) ? cn.GetString() : null;
                        if (name != null)
                            panelData.Content = new EntityTextValueContent(name, contentAlign, entityId);
                    }
                    else if (contentType == "constantNumber")
                    {
                        var value = contentProp.TryGetProperty("value", out var cv) ? cv.GetDouble() : 0.0;
                        panelData.Content = new ConstantNumberContent(value, contentAlign);
                    }
                    else if (contentType == "entityNumberValue")
                    {
                        var name = contentProp.TryGetProperty("name", out var cn) ? cn.GetString() : null;
                        if (name != null)
                            panelData.Content = new EntityNumberValueContent(name, contentAlign, entityId);
                    }
                }

                var panel = new Panel(panelData);
                _listContainer.AddChild(panel);
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating item panel at index {i}: {ex.Message}");
            }
        }
    }

    public override void _Process(double delta)
    {
        PopulateList();
    }
}
