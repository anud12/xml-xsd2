using Godot;
using NewGameProject.UI;

namespace GdUnit4.Examples.Basics.Setup.Sources.UI;

/// Reparents a painted window under its declared parent (flow container,
/// x/y-positioned direct child, or top-level under the root).
public partial class RootNode
{
    /// Moves <paramref name="win"/> under its declared parent: flow children
    /// go into the parent's flow container (box/grid), x/y-positioned child
    /// windows go directly under the parent window. Top-level nodes stay under
    /// this root.
    void Reparent(UiWindow win, UiWindow? parent, UiNodeData node)
    {
        if (parent == null || parent == win)
        {
            if (win.GetParent() != this)
                (win.GetParent() as Node)?.RemoveChild(win);
            if (win.GetParent() != this) AddChild(win);
            return;
        }
        bool hasXY = win.HasWindowXY;
        if (hasXY)
        {
            if (win.GetParent() != parent)
            {
                (win.GetParent() as Node)?.RemoveChild(win);
                parent.AddChild(win);
            }
        }
        else
        {
            var flow = parent.FlowContainer();
            if (flow == null)
            {
                // Parent has no flow container yet (e.g. text node): keep the
                // child directly under the parent window.
                if (win.GetParent() != parent)
                {
                    (win.GetParent() as Node)?.RemoveChild(win);
                    parent.AddChild(win);
                }
            }
            else
            {
                win.ApplyFixedFlowSize(win.FixedFlowSize);
                if (win.GetParent() != flow)
                {
                    (win.GetParent() as Node)?.RemoveChild(win);
                    flow.AddChild(win);
                }
            }
        }
    }
}
