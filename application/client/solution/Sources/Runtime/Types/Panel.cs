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
    public string[]? ChildIds;
    public PanelOnClickHandler? OnClick;
    public PanelContent? Content;
    public Hover? Hover;
    public string? HoverEmitAction;
    public bool HoverStopPropagation;
    public AnimationSequence? BackgroundAnimation;
    public string? HoverBackground;

    // Nine-patch border decoration (thickness + texture); center never drawn.
    public Border? Border;

    // Set by the panel/window builders to mark an explicit surface node even
    // when no surface option (size/offset/background/hover/click) is present.
    public bool Surface;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
public struct Border
{
    public int Width;
    public string Texture;
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

/// <summary>
/// Defines an animated background sequence of frames.
/// When <see cref="SpriteMapLayers"/> is set, each frame is a path to a TIFF sprite map
/// rather than a plain PNG image.
/// </summary>
public class AnimationSequence
{
    public string[] Frames { get; set; }
    public int DurationTicks { get; set; }
    public bool Loop { get; set; }
    public MapLayerBinding[]? SpriteMapLayers { get; set; }
}

/// <summary>
/// Binds one Photoshop layer from the TIFF to a skin texture.
///
/// The TIFF must be a 16-bit unsigned integer RGBA image with Photoshop-compatible
/// metadata (layer names stored in the PSD IFD). Each layer in the PSD corresponds to
/// one <see cref="TiffLayerName"/>. The R and G channels of that layer hold 16-bit
/// integer UV coordinates; the A channel holds per-pixel mask alpha.
///
/// The skin texture (<see cref="SkinPath"/>) must be a standard 8-bit RGBA PNG.
/// </summary>
public struct MapLayerBinding
{
    /// <summary>
    /// Name of the Photoshop layer inside the TIFF/PSD file. Must match exactly.
    /// </summary>
    public string TiffLayerName;

    /// <summary>
    /// Archive path to the 8-bit RGBA PNG skin texture for this layer.
    /// </summary>
    public string SkinPath;
}