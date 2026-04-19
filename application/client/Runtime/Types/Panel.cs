using System;
using System.Runtime.InteropServices;

namespace NewGameProject.Runtime.Types;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
public struct Panel
{
    public string Id;
    public Vector2 Anchor;
    public Vector2 Pivot;
    public Vector2 Offset;
    public Size Size;

    public string Background;

    // The 'children' function becomes a delegate (function pointer)
    public IntPtr ChildrenCallback;

}

    
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
public struct Vector2
{
    public float X;
    public float Y;
}

[StructLayout(LayoutKind.Sequential)]
public struct Size
{
    public float Height;
    public float Width;
    
}