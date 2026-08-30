using Godot;
using NewGameProject.UI;
using System.Text.Json;

namespace GdUnit4.Examples.Basics.Setup.Sources.UI;

/// Text flattening: a window whose only content is a single text/field child
/// renders that text directly as a "text" Label on the window itself (the
/// child node is folded away), matching the legacy panel content semantics.
public partial class RootNode
{
    void FlattenTextChildren(List<UiNodeData> nodes, Dictionary<string, UiNodeData> byId)
    {
        foreach (var node in nodes)
        {
            if (node.Kind != UiNodeKind.Window && node.Kind != UiNodeKind.Division) continue;
            if (node.Children.Count != 1) continue;
            if (!byId.TryGetValue(node.Children[0], out var childNode)) continue;
            if (childNode.Kind != UiNodeKind.Text && childNode.Kind != UiNodeKind.Field) continue;
            if (!_windows.TryGetValue(node.Id, out var win)) continue;
            if (!byId.TryGetValue(childNode.Id, out _)) continue;
            if (win.GetNodeOrNull<Label>("text") != null) continue;

            if (_windows.TryGetValue(childNode.Id, out var childWin))
            {
                childWin.QueueFree();
                _windows.Remove(childNode.Id);
                _flattenedFields.Remove(childNode.Id);
            }
            var label = new Label
            {
                Name = "text",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            label.SetAnchorsPreset(LayoutPreset.FullRect);
            ApplyLabelAlignment(label, ParseOptions(node), ParseOptions(childNode));
            win.AddChild(label);
            label.Text = childNode.Value;
            _flattened.Add(childNode.Id);
            if (childNode.Kind == UiNodeKind.Field)
                _flattenedFields[childNode.Id] = (win, childNode.Id);
        }
    }

    /// Re-resolves the value of every flattened field node from the current
    /// entity store and pushes it into the host window's "text" Label, so
    /// entity value changes surface live (the id-keyed delta alone only
    /// re-applies node state, not the folded-away label).
    void RefreshFlattenedFieldValues(Dictionary<string, UiNodeData> byId)
    {
        foreach (var kv in _flattenedFields)
        {
            var (host, childId) = kv.Value;
            if (!byId.TryGetValue(childId, out var childNode)) continue;
            var label = host.GetNodeOrNull<Label>("text");
            if (label == null) continue;
            if (label.Text != childNode.Value)
                label.Text = childNode.Value;
        }
    }

    static JsonElement ParseOptions(UiNodeData node)
    {
        if (!string.IsNullOrEmpty(node.OptionsJson))
            return JsonDocument.Parse(node.OptionsJson).RootElement;
        return default;
    }

    /// Applies a legacy 9-point align ("top", "center-left", ...) from the
    /// host or child node options to the flattened content label.
    static void ApplyLabelAlignment(Label label, JsonElement hostOpts, JsonElement childOpts)
    {
        string? align = null;
        foreach (var opts in new[] { hostOpts, childOpts })
        {
            if (opts.ValueKind == JsonValueKind.Object
                && opts.TryGetProperty("align", out var a)
                && a.ValueKind == JsonValueKind.String)
            {
                align = a.GetString();
                break;
            }
        }
        if (string.IsNullOrEmpty(align)) return;
        label.HorizontalAlignment = align switch
        {
            "top-left" or "center-left" or "bottom-left" => HorizontalAlignment.Left,
            "top-right" or "center-right" or "bottom-right" => HorizontalAlignment.Right,
            _ => HorizontalAlignment.Center
        };
        label.VerticalAlignment = align switch
        {
            "top" or "top-left" or "top-right" => VerticalAlignment.Top,
            "bottom" or "bottom-left" or "bottom-right" => VerticalAlignment.Bottom,
            _ => VerticalAlignment.Center
        };
    }
}
