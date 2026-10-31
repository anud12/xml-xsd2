using Godot;
using System.Text.Json;

namespace GdUnit4.Examples.Basics.Setup.Sources.UI;

/// Title bar for a titled window (options.title): the strip drawn across the
/// window top and the drag-to-move interaction on it.
public partial class UiWindow
{
    const float TitleBarHeight = 20f;

    /// options.title: draws a title strip across the window top with the
    /// title text centered, and makes the window draggable by the strip
    /// (native-window style). The flow container and any folded text label
    /// are inset below the strip. When the window is also resizable, the
    /// edge/corner resize hit zones keep precedence over the strip.
    void ApplyTitle(JsonElement opts)
    {
        var title = TryStr(opts, "title");
        if (string.IsNullOrEmpty(title))
        {
            if (_titleBar != null)
            {
                _titleBar.QueueFree();
                _titleBar = null;
                _titleLabel = null;
            }
            ApplyTitleInset(0f);
            return;
        }
        if (_titleBar == null)
        {
            // Distinctive names so the strip can never collide with a child
            // node id (a module may name a child "title" itself).
            _titleBar = new ColorRect { Name = "__titleBar" };
            _titleBar.Color = new Color(0.13f, 0.14f, 0.18f);
            _titleBar.MouseFilter = MouseFilterEnum.Stop;
            _titleBar.SetAnchorsPreset(LayoutPreset.TopWide);
            _titleBar.OffsetBottom = TitleBarHeight;
            AddChild(_titleBar);
            _titleLabel = new Label
            {
                Name = "__titleLabel",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                MouseFilter = MouseFilterEnum.Ignore
            };
            _titleLabel.SetAnchorsPreset(LayoutPreset.FullRect);
            _titleBar.AddChild(_titleLabel);
            _titleBar.GuiInput += OnTitleBarGuiInput;
        }
        _titleLabel.Text = title;
        ApplyTitleInset(TitleBarHeight);
    }

    /// Insets the flow container (and any folded text label) below the title
    /// strip. Re-applied on every apply: EnsureFlowContainer recreates the
    /// container when the layout mode changes, so the inset must re-assert.
    void ApplyTitleInset(float top)
    {
        var scroll = GetNodeOrNull<ScrollContainer>("scroll");
        if (scroll != null)
        {
            // The scroll wrapper is the window's direct child and carries the
            // title inset; the flow container inside it sits at its origin.
            scroll.OffsetTop = top;
            if (FlowContainer() is Control inner) inner.OffsetTop = 0;
            return;
        }
        var box = GetNodeOrNull<BoxContainer>("box");
        if (box != null) box.OffsetTop = top;
        var grid = GetNodeOrNull<UiGrid>("grid");
        if (grid != null) grid.OffsetTop = top;
        var text = GetNodeOrNull<Label>("text");
        if (text != null) text.OffsetTop = top;
    }

    void OnTitleBarGuiInput(InputEvent evt)
    {
        if (evt is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left)
        {
            var local = GetGlobalTransform().AffineInverse() * mb.GlobalPosition;
            if (mb.Pressed)
            {
                // A resizable window keeps its edge/corner resize hit zones
                // over the strip: such a press starts a resize, not a drag.
                if (_resizable)
                {
                    BeginResizeAtLocal(local, mb.GlobalPosition);
                    if (_resizing) return;
                }
                _titleDragging = true;
                _titleDragStartMouse = mb.GlobalPosition;
                _titleDragStartPos = Position;
            }
            else
            {
                if (_resizing)
                    EndResizeAtLocal(local);
                _titleDragging = false;
            }
            GetViewport().SetInputAsHandled();
            return;
        }
        if (evt is not InputEventMouseMotion mm) return;
        if (_resizing)
        {
            // The strip captured the press, so the window's own resize path
            // sees none of the motion; drive it from here.
            UpdateResizeMotion(mm.GlobalPosition);
            return;
        }
        if (_titleDragging)
        {
            // Move the window with the cursor; persist it so the per-frame
            // PositionWithin repaint keeps the dragged position.
            Position = _titleDragStartPos + (mm.GlobalPosition - _titleDragStartMouse);
            _hasUserPosition = true;
            _userPosition = Position;
            GetViewport().SetInputAsHandled();
        }
        else if (_resizable)
        {
            var local = GetGlobalTransform().AffineInverse() * mm.GlobalPosition;
            ApplyResizeCursor(ResizeEdgeAt(local));
        }
    }
}
