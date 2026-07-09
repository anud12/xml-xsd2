using System.Runtime.InteropServices;

namespace NewGameProject.Runtime;

public struct Container
{
    public string Id;
    public Dictionary<string, string>? TextMap;
    public Dictionary<string, double>? NumberMap;
    public string[] Entities;
    public Dictionary<string, double>? GetX;
    public Dictionary<string, double>? GetY;
    public Dictionary<string, double>? GetSpanX;
    public Dictionary<string, double>? GetSpanY;
    public AxisSize? SizeX;
    public AxisSize? SizeY;
}

public struct AxisSize
{
    public double Value;
    public OutOfBoundsRule OutOfBounds;
}

public enum OutOfBoundsRule
{
    Unbound,
    Clamp,
    Wrap
}
