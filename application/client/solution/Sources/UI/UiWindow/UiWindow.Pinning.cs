using Godot;

namespace GdUnit4.Examples.Basics.Setup.Sources.UI;

public partial class UiWindow
{
    // A polygon pin avoids depending on the active font's glyph coverage.
    public void SetPinIndicator(string key, bool pinned, Action? toggle = null)
    {
        var button = GetNodeOrNull<Button>("__pinIndicator");
        if (button == null)
        {
            button = new Button { Name = "__pinIndicator", MouseFilter = MouseFilterEnum.Stop, FocusMode = FocusModeEnum.None };
            button.SetAnchorsPreset(LayoutPreset.TopRight);
            button.OffsetLeft = -48;
            button.OffsetRight = -2;
            button.OffsetTop = 2;
            button.OffsetBottom = 24;
            button.Pressed += () => toggle?.Invoke();
            var icon = new Polygon2D { Name = "icon", Position = new Vector2(7, 4), Polygon = new Vector2[] {
                new(2, 0), new(10, 0), new(8, 5), new(11, 8), new(7, 8), new(6, 14), new(5, 8), new(1, 8), new(4, 5) } };
            button.AddChild(icon);
            var label = new Label { Name = "key", MouseFilter = MouseFilterEnum.Ignore, Position = new Vector2(23, 1) };
            button.AddChild(label);
            AddChild(button);
        }
        var color = pinned ? Colors.Gold : Colors.White;
        button.GetNode<Polygon2D>("icon").Color = color;
        var keyLabel = button.GetNode<Label>("key");
        keyLabel.Text = key;
        keyLabel.AddThemeColorOverride("font_color", color);
        MoveChild(button, GetChildCount() - 1);
    }
}
