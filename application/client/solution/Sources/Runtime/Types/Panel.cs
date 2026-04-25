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