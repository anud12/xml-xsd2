using System;
using System.Runtime.InteropServices;

namespace NewGameProject.Runtime;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
public struct Panel
{
    public string Id;

    // Only fields required by tests are kept in layout parity with native PanelFfi
    public string Background;

    public Vector2 Anchor;
    public Vector2 Pivot;
    public Offset Offset;
    public Size Size;
    public Layout? Layout;
    public Panel[]? Children;
    public PanelOnClickHandler? OnClick;
    public PanelContent? Content;
    public Hover? Hover;
    public AnimationSequence? BackgroundAnimation;
}

public struct Layout
{
    public TrackDefinition[]? Columns;
    public bool? RowFirst;
    public bool? ReverseOrder;
    public Gap? gap;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]

public struct TrackDefinition
{
    public Align? align;
    public int? min;
    public int? max;
    public int? weight;
}

public enum Align : int
{
    Start = 0,
    End = 1
}
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
public struct Gap
{
    public int Row;
    public int Column;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
public struct Vector2
{
    public float X;
    public float Y;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
public struct Offset
{
    public float top;
    public float bottom;
    public float left;
    public float right;
}

[StructLayout(LayoutKind.Sequential)]
public struct Size
{
    public float Height;
    public float Width;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
public struct PanelOnClickHandler {
    public string ActionName;
}

public abstract class PanelContent
{
}

public class ConstantTextContent : PanelContent
{
    public string Value { get; set; }
    public string Align { get; set; }

    public ConstantTextContent(string value, string align = "center")
    {
        Value = value;
        Align = align;
    }
}

public class EntityTextValueContent : PanelContent
{
    public string Name { get; set; }
    public string Align { get; set; }
    public string? EntityId { get; set; }

    public EntityTextValueContent(string name, string align = "center", string? entityId = null)
    {
        Name = name;
        Align = align;
        EntityId = entityId;
    }
}

public class ConstantNumberContent : PanelContent
{
    public double Value { get; set; }
    public string Align { get; set; }

    public ConstantNumberContent(double value, string align = "center")
    {
        Value = value;
        Align = align;
    }
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
public struct Hover
{
    public string Texture;
    public int Thickness;
}

public class EntityNumberValueContent : PanelContent
{
    public string Name { get; set; }
    public string Align { get; set; }
    public string? EntityId { get; set; }

    public EntityNumberValueContent(string name, string align = "center", string? entityId = null)
    {
        Name = name;
        Align = align;
        EntityId = entityId;
    }
}

public class ContainerListViewContent : PanelContent
{
    public string ContainerId { get; set; }
    public bool Vertical { get; set; }
    public Panel[]? TemplateResults { get; set; }

    public ContainerListViewContent(string containerId, bool vertical = true)
    {
        ContainerId = containerId;
        Vertical = vertical;
    }
}

public class AnimationSequence
{
    public string[] Frames { get; set; }
    public int DurationTicks { get; set; }
    public bool Loop { get; set; }
}