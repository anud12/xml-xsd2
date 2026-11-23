using System.Collections.Generic;
using Godot;
using NewGameProject.Runtime;

namespace NewGameProject.UI;

public partial class ErrorMessageViewer : Control
{
    private const double DisplaySeconds = 5;
    private const double FadeSeconds = 0.75;
    private readonly List<Entry> _entries = new();
    private VBoxContainer _stack = null!;

    private sealed class Entry(PanelContainer panel)
    {
        public PanelContainer Panel { get; } = panel;
        public double Remaining { get; set; } = DisplaySeconds + FadeSeconds;
    }

    public override void _Ready()
    {
        _stack = GetNode<VBoxContainer>("Stack");
        _stack.MinimumSizeChanged += ResizeToStack;
        Hide();
    }

    private void ResizeToStack()
    {
        Size = new Godot.Vector2(Size.X, System.Math.Max(56, _stack.GetCombinedMinimumSize().Y));
    }

    public override void _Process(double delta)
    {
        // Advance existing entries before adding new ones, which start fully visible.
        for (var i = _entries.Count - 1; i >= 0; i--)
        {
            var entry = _entries[i];
            entry.Remaining = System.Math.Max(0, entry.Remaining - delta);
            entry.Panel.Modulate = new Color(1, 1, 1,
                (float)System.Math.Min(1, entry.Remaining / FadeSeconds));
            if (entry.Remaining > 0) continue;
            _stack.RemoveChild(entry.Panel);
            entry.Panel.QueueFree();
            _entries.RemoveAt(i);
        }

        foreach (var message in RuntimeInterop.DrainActionDenials())
        {
            var panel = new PanelContainer { MouseFilter = MouseFilterEnum.Ignore };
            var label = new Label { Text = message, HorizontalAlignment = HorizontalAlignment.Center,
                MouseFilter = MouseFilterEnum.Ignore };
            panel.AddChild(label);
            _stack.AddChild(panel); // Oldest at the top; new messages append at the bottom.
            _entries.Add(new Entry(panel));
        }
        Visible = _entries.Count > 0;
    }
}
