using System.Collections.Generic;
using Godot;
using NewGameProject.Runtime;

namespace GdUnit4.Examples.Basics.Setup.Sources.UI;

public partial class RootNode : Control {
    private List<Node> children = new();

    public void AddChild(Node child) {
        children.Add(child);
    }

    public RootNode() {
        SetAnchorsPreset(Control.LayoutPreset.Center);
        var idList = RuntimeInterop.GetPanelIds();
        foreach (var id in idList) {
            this.AddChild(new Panel(RuntimeInterop.GetPanelById(id)) {
                Name = id
            });
        }
    }
}