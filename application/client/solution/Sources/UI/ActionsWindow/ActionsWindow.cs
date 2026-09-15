using Godot;
using NewGameProject.Runtime;

using Vector2 = Godot.Vector2;

namespace GdUnit4.Examples.Basics.Setup.Sources.UI;

/// <summary>
/// A movable window listing all registered actions. Drag it by the header;
/// clicking an action emits it through the runtime.
/// </summary>
public partial class ActionsWindow : PanelContainer
{
    public ActionsWindow()
    {
        Name = "ActionsWindow";
        Position = new Vector2(110, 50);
        CustomMinimumSize = new Vector2(220, 0);
        ZIndex = 10;

        var vBox = new VBoxContainer();
        AddChild(vBox);

        var header = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Stop };
        vBox.AddChild(header);

        var title = new Label
        {
            Text = "Actions",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        header.AddChild(title);

        var closeButton = new Button { Text = "X", Flat = true, FocusMode = Control.FocusModeEnum.None };
        closeButton.Pressed += Close;
        header.AddChild(closeButton);

        var scroll = new ScrollContainer { CustomMinimumSize = new Vector2(200, 300) };
        vBox.AddChild(scroll);

        var list = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        scroll.AddChild(list);

        var ids = RuntimeInterop.GetActionIds();
        if (ids.Length == 0)
        {
            list.AddChild(new Label { Text = "(no actions registered)" });
        }
        foreach (var action in ids)
        {
            var btn = new Button { Text = action };
            btn.Pressed += () => RuntimeInterop.emitAction(action);
            list.AddChild(btn);
        }

        var dragOffset = new Vector2?();
        header.GuiInput += evt =>
        {
            if (evt is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left)
            {
                if (mb.Pressed)
                {
                    dragOffset = mb.GlobalPosition - GlobalPosition;
                    GetViewport().SetInputAsHandled();
                }
                else
                {
                    dragOffset = null;
                }
            }
            else if (evt is InputEventMouseMotion mm && dragOffset.HasValue)
            {
                GlobalPosition = mm.GlobalPosition - dragOffset.Value;
                GetViewport().SetInputAsHandled();
            }
        };
    }

    public void Close() => QueueFree();
}
