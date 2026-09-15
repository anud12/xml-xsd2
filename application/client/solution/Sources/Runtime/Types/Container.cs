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
    /// How each entity's marker is placed over its span: <see cref="ContainerAlignment.Center"/>
    /// centers the marker on the span midpoint, <see cref="ContainerAlignment.TopLeft"/> (the
    /// default) pins the marker's top-left corner to (x, y).
    public string? Alignment;
}

/// String constants for <see cref="Container.Alignment"/>.
public static class ContainerAlignment
{
    /// Center the entity's marker on the midpoint of its span.
    public const string Center = "center";
    /// Pin the entity's marker's top-left corner to (x, y). Default.
    public const string TopLeft = "top-left";
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
