using System.Runtime.InteropServices;

namespace NewGameProject.Runtime;

public struct Container
{
    public string Id;
    public Dictionary<string, string>? TextMap;
    public Dictionary<string, double>? NumberMap;
    public string[] Entities;
    public Dictionary<string, double>? GetXForEntityId;
    public Dictionary<string, double>? GetYForEntityId;
    public Dictionary<string, double>? GetSpanXForEntityId;
    public Dictionary<string, double>? GetSpanYForEntityId;
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
