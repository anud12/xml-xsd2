using System;
using System.Runtime.InteropServices;
using GdUnit4.Examples.Basics.Setup.Test.PanelToPanelNode.Anchor;

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
}

public struct Layout
{
    public TrackDefinition[]? Columns;
    public bool? RowFirst;
    public bool? ReverseOrder;
    public Gap? gap;
}

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