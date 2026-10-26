using Godot;

namespace GdUnit4.Examples.Basics.Setup.Sources.UI;

/// Per-axis fill rules plus optional nine-slice geometry describing how a
/// texture is painted into a region. Sliced painting is nine-slice scaling
/// (en.wikipedia.org/wiki/9-slice_scaling).
///
/// Fill rules (unsliced region):
///   stretch/stretch                 independent scale per axis (may distort)
///   stretch/stretch + KeepAspect    uniform scale, centered, overflow clipped
///   fit/fit                         uniform scale (min), centered (contain)
///   fit/stretch                     uniform scale capped by the fit axis,
///                                   centered on the fit axis
///   tile/stretch                    uniform scale taken from the stretch
///                                   axis; the tile axis repeats at that scale
///   tile/tile                       native pixel size, repeated on both axes
/// Sliced: corners are painted fixed at slice size; each edge is painted along
/// its axis per the corresponding rule at native cross size; the center is
/// painted per the fill rules when DrawCenter.
public readonly struct UiPaintSpec
{
    public readonly bool TileWidth;
    public readonly bool TileHeight;
    public readonly bool KeepAspect;
    public readonly bool FitWidth;
    public readonly bool FitHeight;
    public readonly int SliceWidth;
    public readonly int SliceHeight;

    public UiPaintSpec(
        bool tileWidth, bool tileHeight, bool keepAspect,
        bool fitWidth = false, bool fitHeight = false,
        int sliceWidth = 0, int sliceHeight = 0)
    {
        TileWidth = tileWidth;
        TileHeight = tileHeight;
        KeepAspect = keepAspect;
        FitWidth = fitWidth;
        FitHeight = fitHeight;
        SliceWidth = sliceWidth;
        SliceHeight = sliceHeight;
    }

    /// Full-rect cover: uniform scale, centered, overflow clipped.
    public static UiPaintSpec Cover => new(false, false, true);

    /// Full-rect contain: uniform scale (min), centered, letterboxed.
    public static UiPaintSpec Contain => new(false, false, false, true, true);

    /// Nine-slice frame: corners fixed at `margin`, edges and the (transparent)
    /// center stretched per the fill rules.
    public static UiPaintSpec Frame(int margin)
        => new(false, false, false, false, false, margin, margin);

    /// Nine-slice frame with the center stretched per the fill rules.
    public static UiPaintSpec FrameCentered(int margin)
        => new(false, false, false, false, false, margin, margin);
}

/// The single paint primitive all textured ui surfaces go through.
public static class UiTexturePainter
{
    public static void Paint(CanvasItem ci, Rect2 region, UiPaintSpec spec, Texture2D? tex)
    {
        if (tex == null) return;
        var tw = (float)tex.GetWidth();
        var th = (float)tex.GetHeight();
        if (tw <= 0f || th <= 0f
            || region.Size.X <= 0f || region.Size.Y <= 0f)
        {
            return;
        }
        if (spec.SliceWidth > 0 && spec.SliceHeight > 0)
            PaintNineSlice(ci, region, spec, tex, tw, th);
        else
            PaintFill(ci, region, Vector2.Zero, new Vector2(tw, th), spec, tex);
    }

    /// Paints the source extent (`srcSize` of `tex` at `srcOrigin`) into
    /// `dest` per the fill rules.
    static void PaintFill(
        CanvasItem ci, Rect2 dest,
        Vector2 srcOrigin, Vector2 srcSize,
        UiPaintSpec spec, Texture2D tex)
    {
        if (dest.Size.X <= 0f || dest.Size.Y <= 0f
            || srcSize.X <= 0f || srcSize.Y <= 0f)
        {
            return;
        }
        var sx = dest.Size.X / srcSize.X;
        var sy = dest.Size.Y / srcSize.Y;

        if (spec.TileWidth && spec.TileHeight)
        {
            Repeat(ci, dest, tex, srcOrigin, srcSize, 1f, 1f, true, true);
            return;
        }

        if (!spec.TileWidth && !spec.TileHeight)
        {
            var hasFit = spec.FitWidth || spec.FitHeight;
            if (hasFit)
            {
                var fs = Mathf.Min(sx, sy);
                var fw = srcSize.X * fs;
                var fh = srcSize.Y * fs;
                var fx = dest.Position.X + (dest.Size.X - fw) / 2f;
                var fy = dest.Position.Y + (dest.Size.Y - fh) / 2f;
                ci.DrawTextureRect(tex, new Rect2(fx, fy, fw, fh), false);
                return;
            }
            if (!spec.KeepAspect)
            {
                ci.DrawTextureRectRegion(tex, dest, new Rect2(srcOrigin, srcSize));
                return;
            }
            var cov = Mathf.Max(sx, sy);
            var ox = (dest.Size.X - srcSize.X * cov) / 2f;
            var oy = (dest.Size.Y - srcSize.Y * cov) / 2f;
            // Draw exactly `dest` (canvas draws are not clipped to the
            // control), sampling the centered cover crop from the source.
            ci.DrawTextureRectRegion(tex, dest,
                new Rect2(
                    srcOrigin.X - ox / cov,
                    srcOrigin.Y - oy / cov,
                    dest.Size.X / cov,
                    dest.Size.Y / cov));
            return;
        }

        var s = spec.TileWidth ? sy : sx;
        if (spec.FitWidth || spec.FitHeight)
        {
            var freeScale = spec.TileWidth ? sx : sy;
            s = Mathf.Min(s, freeScale);
        }
        Repeat(ci, dest, tex, srcOrigin, srcSize, s, s,
            spec.TileWidth, !spec.TileWidth);
    }

    /// Nine-slice: the four corners are painted 1:1 at the slice size; each
    /// edge and the center are painted per the {@link UiPaintSpec} fill rules
    /// (stretch/tile per axis + keepAspectRatio) — the free axis follows the
    /// fill rule and the fixed axis is stretched to fit its (native) cross
    /// size. A frame is a nine-slice whose source center is transparent, so the
    /// "center" paints nothing. `spec` is the fill rule shared by edges and
    /// center; `sw`/`sh` are the slice margins.
    static void PaintNineSlice(
        CanvasItem ci, Rect2 r, UiPaintSpec spec, Texture2D tex,
        float tw, float th)
    {
        // Slice geometry is clamped so the four corners always fit AND the
        // source center band keeps at least one texel: the center band is
        // [sw, tw - sw], which collapses to zero width when sw reaches tw/2,
        // dropping the center fill. Capping sw at (tw - 1) / 2 guarantees
        // tw - 2*sw >= 1. Source and destination corners share the clamped
        // size, so a solid texture still fills the whole region.
        var sw = Mathf.Max(0, Mathf.Min(Mathf.Min(spec.SliceWidth, (int)(r.Size.X / 2f)), (int)(tw - 1) / 2));
        var sh = Mathf.Max(0, Mathf.Min(Mathf.Min(spec.SliceHeight, (int)(r.Size.Y / 2f)), (int)(th - 1) / 2));
        if (sw == 0 && sh == 0)
        {
            PaintFill(ci, r, Vector2.Zero, new Vector2(tw, th), spec, tex);
            return;
        }

        // Source band boundaries (x, then y).
        var sxl = new[] { 0f, (float)sw, (float)(tw - sw), tw };
        var syl = new[] { 0f, (float)sh, (float)(th - sh), th };
        // Destination band boundaries (the corners keep the clamped slice size).
        var dxl = new[] { 0f, (float)sw, (float)(r.Size.X - sw), (float)r.Size.X };
        var dyl = new[] { 0f, (float)sh, (float)(r.Size.Y - sh), (float)r.Size.Y };

        for (var j = 0; j < 3; j++)
        {
            for (var i = 0; i < 3; i++)
            {
                var dest = new Rect2(
                    r.Position.X + dxl[i], r.Position.Y + dyl[j],
                    dxl[i + 1] - dxl[i], dyl[j + 1] - dyl[j]);
                var src = new Rect2(
                    sxl[i], syl[j],
                    sxl[i + 1] - sxl[i], syl[j + 1] - syl[j]);
                // Corners: fixed 1:1 (dest size == source size).
                if (i != 1 && j != 1)
                {
                    ci.DrawTextureRectRegion(tex, dest, src);
                }
                // Edges: the free axis follows the fill rule, the cross axis
                // stretches to its native size. Center: the full fill rule.
                else if (i == 1 && j == 1)
                {
                    PaintFill(ci, dest, src.Position, src.Size, spec, tex);
                }
                else if (j == 1) // left/right edge: free axis = height
                {
                    var es = new UiPaintSpec(false, spec.TileHeight, false);
                    PaintFill(ci, dest, src.Position, src.Size, es, tex);
                }
                else // top/bottom edge: free axis = width
                {
                    var es = new UiPaintSpec(spec.TileWidth, false, false);
                    PaintFill(ci, dest, src.Position, src.Size, es, tex);
                }
            }
        }
    }

    /// Repeats the source tile (size `srcSize` of `tex` at `srcOrigin`) across
    /// `dest` at scale (sw, sh), anchored at `dest.Position`; the final
    /// partial tile is cropped from the source, never stretched. An axis that
    /// is not tiled is drawn once, stretched to fill the remaining extent.
    static void Repeat(
        CanvasItem ci, Rect2 dest, Texture2D tex,
        Vector2 srcOrigin, Vector2 srcSize,
        float sw, float sh, bool tileWidth, bool tileHeight)
    {
        var dW = srcSize.X * sw;
        var dH = srcSize.Y * sh;
        if (dW <= 0f || dH <= 0f) return;
        for (var y = dest.Position.Y; ; y += dH)
        {
            if (y >= dest.End.Y) break;
            var rowH = tileHeight ? Mathf.Min(dH, dest.End.Y - y) : dest.End.Y - y;
            for (var x = dest.Position.X; ; x += dW)
            {
                if (x >= dest.End.X) break;
                var colW = tileWidth ? Mathf.Min(dW, dest.End.X - x) : dest.End.X - x;
                ci.DrawTextureRectRegion(tex,
                    new Rect2(x, y, colW, rowH),
                    new Rect2(srcOrigin.X, srcOrigin.Y, colW / sw, rowH / sh));
            }
            if (!tileHeight) break;
        }
    }
}

/// Full-rect paint surface: draws its texture via the painter with a fixed
/// spec. Used for backgrounds, borders, and hover outlines.
public partial class UiTextureSurface : Control
{
    UiPaintSpec _spec;
    Texture2D? _texture;

    public Texture2D? Texture => _texture;
    public UiPaintSpec Spec => _spec;

    public void Update(UiPaintSpec spec, Texture2D? texture)
    {
        _spec = spec;
        _texture = texture;
        QueueRedraw();
    }

    public override void _Draw()
    {
        // Local-space rect: _Draw coordinates are relative to this control's
        // own top-left, not its position within the parent.
        var region = new Rect2(Vector2.Zero, Size);
        if (_texture == null) return;
        UiTexturePainter.Paint(this, region, _spec, _texture);
    }
}
