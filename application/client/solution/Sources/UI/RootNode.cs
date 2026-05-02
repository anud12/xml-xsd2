using System.Collections.Generic;
using Godot;
using NewGameProject.Runtime;

namespace GdUnit4.Examples.Basics.Setup.Sources.UI {
    using UIPanel = Panel;

    public partial class RootNode : Godot.Panel {
        public void RegisterChild(Node child) {
            base.AddChild(child);
        }

        public RootNode() {
            SetAnchorsPreset(Control.LayoutPreset.Center);
            var idList = RuntimeInterop.GetPanelIds();
            foreach (var id in idList) {
                var p = new UIPanel(RuntimeInterop.GetPanelById(id)) {
                    Name = id
                };
                base.AddChild(p);
            }
        }
    }
}