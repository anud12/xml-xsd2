using Godot;
using NewGameProject.Module;
using NewGameProject.UI;
using System.Text.Json;
using RuntimeInterop = NewGameProject.Runtime.RuntimeInterop;

namespace GdUnit4.Examples.Basics.Setup.Sources.UI;

/// Background rendering: static PNG textures, sprite map composition, and
/// frame animations advanced per runtime elapsed time unit.
public partial class UiWindow
{
    /// options.background: a string is a static PNG archive path (rendered as a
    /// full-rect background behind the content), an object with "kind" is a
    /// sprite map (composed from the TIFF mask + layer skins), an object with
    /// "name" is an animation reference whose frames advance per runtime
    /// elapsed time unit. options.portalArrow (engine-owned sector portal
    /// headless arrow) is rendered as a full-rect solid shaft instead.
    void ApplyBackground(JsonElement opts)
    {
        if (opts.ValueKind == JsonValueKind.Object
            && opts.TryGetProperty("portalArrow", out var pa)
            && pa.ValueKind == JsonValueKind.True)
        {
            var aw = opts.TryGetProperty("width", out var pw) && pw.ValueKind == JsonValueKind.Number
                ? (float)pw.GetDouble() : 0f;
            var ah = opts.TryGetProperty("height", out var ph) && ph.ValueKind == JsonValueKind.Number
                ? (float)ph.GetDouble() : 0f;
            var unlinked = opts.TryGetProperty("unlinked", out var ul)
                && ul.ValueKind == JsonValueKind.True;
            var line = opts.TryGetProperty("portalLine", out var pl)
                && pl.ValueKind == JsonValueKind.True;
            ApplyPortalArrow(aw, ah, unlinked, line);
            return;
        }
        ApplyAreaOutline(opts);
        if (opts.ValueKind == JsonValueKind.Undefined
            || !opts.TryGetProperty("background", out var bg))
            return;
        if (bg.ValueKind == JsonValueKind.String)
        {
            var path = bg.GetString();
            if (string.IsNullOrEmpty(path)) return;
            if (RuntimeInterop.GetFileFromArchive().TryGetValue(path, out var data))
            {
                var img = new Image();
                img.LoadPngFromBuffer(data);
                var tex = ImageTexture.CreateFromImage(img);
                _baseBackgroundTexture = tex;
                SetBackgroundTexture(tex);
            }
            else
            {
                RuntimeInterop.Log($"ui: background not found in archive: {path}");
            }
        }
        else if (bg.ValueKind == JsonValueKind.Object)
        {
            if (bg.TryGetProperty("kind", out var kind)
                && kind.GetString() == "spriteMap")
            {
                ApplySpriteMapBackground(bg);
                return;
            }
            if (!bg.TryGetProperty("name", out var n) || n.ValueKind != JsonValueKind.String)
                return;
        var name = n.GetString() ?? "";
        _animName = name;
        _animDurationTicks = 1;
        _animLoop = false;
        if (bg.TryGetProperty("duration", out var d) && d.ValueKind == JsonValueKind.Number)
            _animDurationTicks = Math.Max(1, (int)d.GetDouble());
        if (bg.TryGetProperty("loop", out var l) && l.ValueKind == JsonValueKind.True)
            _animLoop = true;
        _animLastElapsed = -1;
        _currentFramePath = null;
        // Render frame 0 immediately: in test mode the runtime loop is off, so
        // elapsed time never advances and _Process would never advance a frame.
        AdvanceAnimationFrame(true);
        }
    }

    // Sector portal marker: a short headless shaft drawn in _Draw across the
    // gap between two adjacent cells. A vertical wall (narrow node, w < h)
    // gets a horizontal shaft; a horizontal wall (wide node, w >= h) gets a
    // vertical one. The shaft is centered on the node and overflows its thin
    // extent so it reads as a bridge between the two cells.
    readonly struct PortalArrowSpec
    {
        public readonly bool Horizontal;
        public readonly bool Unlinked;
        public readonly bool Line;
        public readonly float W;
        public readonly float H;
        public PortalArrowSpec(bool horizontal, bool unlinked, bool line, float w, float h)
        { Horizontal = horizontal; Unlinked = unlinked; Line = line; W = w; H = h; }
    }
    PortalArrowSpec? _portalArrow;

    /// A single area polygon to draw: its vertices (view-local logical units)
    /// plus its own outline line color, body fill color, and outline thickness.
    class AreaPoly
    {
        public List<Vector2> Points = new();
        public Color OutlineColor = new(1f, 0f, 0f, 1f);
        public Color BodyColor = new(0f, 1f, 0f, 1f);
        public float Thickness = 2f;
    }

    /// Container-view area outline: the entity's declared area polygons
    /// (view-local logical units) drawn as a solid outline + rgba body fill
    /// over the item sprite. `null` when the node has no area outline.
    class AreaOutlineSpec
    {
        public List<AreaPoly> Polygons = new();
        // The item's own view-local origin (x/y stamp): each polygon is
        // translated by this so it spans the node's local rect rather than
        // view space.
        public Vector2 Origin = Vector2.Zero;
    }
    AreaOutlineSpec _areaOutline;

    public override void _Draw()
    {
        if (_portalArrow is { } a)
        {
            DrawPortalArrow(a);
        }
        if (_areaOutline is { } ao && ao.Polygons.Count > 0)
        {
            DrawAreaOutline(ao);
        }
    }

    void DrawPortalArrow(PortalArrowSpec a)
    {
        // Linked portals are orange; unlinked (a declared opening with no facing
        // sector) are red, so a lone sector's dead-end openings read as "missing".
        var color = a.Unlinked ? new Color(0.85f, 0.22f, 0.22f) : new Color(0.90f, 0.49f, 0.13f);
        if (a.Line)
        {
            DrawRect(new Rect2(0, 0, a.W, a.H), color);
            return;
        }
        var shaft = 4f;
        if (a.Horizontal)
        {
            var len = Mathf.Max(a.W, 14f);
            var cx = a.W / 2f;
            var cy = a.H / 2f;
            var x0 = cx - len / 2f;
            DrawRect(new Rect2(x0, cy - shaft / 2f, len, shaft), color);
        }
        else
        {
            var len = Mathf.Max(a.H, 14f);
            var cx = a.W / 2f;
            var cy = a.H / 2f;
            var y0 = cy - len / 2f;
            DrawRect(new Rect2(cx - shaft / 2f, y0, shaft, len), color);
        }
    }

    /// Draws the area outlines over the sprite: each polygon gets a
    /// translucent rgba body fill (its own body color) plus a solid outline
    /// line of its own color/thickness. The stamped polygons are in view-local
    /// logical units; each is translated by the item's own x/y (`ao.Origin`)
    /// so it spans this node's local rect.
    void DrawAreaOutline(AreaOutlineSpec ao)
    {
        foreach (var poly in ao.Polygons)
        {
            // The stamped polygon is in view-local logical units; this item's
            // own _Draw space is local to the node (a 7x5 box at its cell
            // origin). Translate by the item's own x/y so the polygon spans
            // the node rect (otherwise it lands hundreds of px away and is
            // clipped to nothing).
            var arr = poly.Points.Select(p => p - ao.Origin).ToArray();
            if (arr.Length < 3) continue;
            // Body fill: a translucent version of this polygon's body color.
            var fill = new Color(poly.BodyColor.R, poly.BodyColor.G, poly.BodyColor.B, poly.BodyColor.A * 0.35f);
            DrawColoredPolygon(arr, fill, null, null);
            // Solid outline line, drawn after the fill so it sits over it.
            for (int i = 0; i < arr.Length; i++)
            {
                DrawLine(arr[i], arr[(i + 1) % arr.Length], poly.OutlineColor, poly.Thickness, false);
            }
        }
    }

    void ApplyPortalArrow(float w, float h, bool unlinked, bool line)
    {
        _portalArrow = new PortalArrowSpec(w < h, unlinked, line, w, h);
        // Above sibling cell windows so the arrow (which overflows the thin
        // node into the cell gap) is not occluded by the cell backgrounds.
        ZIndex = 100;
        QueueRedraw();
    }

    /// options.areaOutline: { polygons: [ { points: [[x, y], ...],
    /// color?: [r, g, b, a], bodyColor?: [r, g, b, a], thickness? }, ... ] } —
    /// the entity's declared area polygons in view-local logical units, drawn
    /// over the item sprite as a translucent body + solid outline each. Each
    /// polygon carries its own color/bodyColor/thickness (defaults red/green/2
    /// when omitted).
    void ApplyAreaOutline(JsonElement opts)
    {
        if (opts.ValueKind != JsonValueKind.Object
            || !opts.TryGetProperty("areaOutline", out var ao))
        {
            _areaOutline = null;
            return;
        }
        if (ao.ValueKind != JsonValueKind.Object
            || !ao.TryGetProperty("polygons", out var polys)
            || polys.ValueKind != JsonValueKind.Array)
        {
            _areaOutline = null;
            return;
        }
        var spec = new AreaOutlineSpec();
        foreach (var poly in polys.EnumerateArray())
        {
            if (poly.ValueKind != JsonValueKind.Object
                || !poly.TryGetProperty("points", out var pts)
                || pts.ValueKind != JsonValueKind.Array)
            {
                continue;
            }
            var ap = new AreaPoly();
            foreach (var p in pts.EnumerateArray())
            {
                if (p.ValueKind != JsonValueKind.Array) continue;
                var x = 0f; var y = 0f;
                var idx = 0;
                foreach (var comp in p.EnumerateArray())
                {
                    if (idx == 0 && comp.ValueKind == JsonValueKind.Number) x = (float)comp.GetDouble();
                    else if (idx == 1 && comp.ValueKind == JsonValueKind.Number) y = (float)comp.GetDouble();
                    idx++;
                }
                ap.Points.Add(new Vector2(x, y));
            }
            if (ap.Points.Count < 3)
            {
                continue;
            }
            // Per-polygon styling (defaults red/green/2 when a field is omitted).
            if (poly.TryGetProperty("color", out var col) && col.ValueKind == JsonValueKind.Array)
            {
                ap.OutlineColor = ParseRgba(col, new Color(1f, 0f, 0f, 1f));
            }
            if (poly.TryGetProperty("bodyColor", out var bc) && bc.ValueKind == JsonValueKind.Array)
            {
                ap.BodyColor = ParseRgba(bc, new Color(0f, 1f, 0f, 1f));
            }
            if (poly.TryGetProperty("thickness", out var t) && t.ValueKind == JsonValueKind.Number)
            {
                ap.Thickness = (float)t.GetDouble();
            }
            spec.Polygons.Add(ap);
        }
        if (spec.Polygons.Count == 0)
        {
            _areaOutline = null;
            return;
        }
        // The item's view-local origin (the x/y stamp): used to translate the
        // view-local polygons into this node's local _Draw space.
        var ox = TryNum(opts, "x", out var px) ? px : 0f;
        var oy = TryNum(opts, "y", out var py) ? py : 0f;
        spec.Origin = new Vector2(ox, oy);
        _areaOutline = spec;
        QueueRedraw();
    }

    /// Parses a `[r, g, b, a]` (0..1) JSON array into a `Color`, falling back
    /// to `fallback` for any component that is missing or not a number.
    static Color ParseRgba(JsonElement arr, Color fallback)
    {
        var r = fallback.R; var g = fallback.G; var b = fallback.B; var a = fallback.A;
        var i = 0;
        foreach (var ch in arr.EnumerateArray())
        {
            if (ch.ValueKind != JsonValueKind.Number) continue;
            var v = (float)ch.GetDouble();
            switch (i)
            {
                case 0: r = v; break;
                case 1: g = v; break;
                case 2: b = v; break;
                case 3: a = v; break;
            }
            i++;
        }
        return new Color(r, g, b, a);
    }

    /// A sprite map background: { kind: "spriteMap", map, layers: [{layer,
    /// texture}] } — composed once from the 16-bit TIFF mask + 8-bit PNG skins.
    void ApplySpriteMapBackground(JsonElement bg)
    {
        var mapPath = bg.TryGetProperty("map", out var m) ? m.GetString() ?? "" : "";
        var files = RuntimeInterop.GetFileFromArchive();
        if (!files.TryGetValue(mapPath, out var mapData))
        {
            RuntimeInterop.Log(
                $"Sprite map: missing TIFF file \"{mapPath}\" for panel \"{Name}\".");
            return;
        }
        var skins = new List<Image>();
        if (bg.TryGetProperty("layers", out var layers) && layers.ValueKind == JsonValueKind.Array)
        {
            foreach (var layer in layers.EnumerateArray())
            {
                var skinPath = layer.TryGetProperty("texture", out var t) ? t.GetString() ?? "" : "";
                if (files.TryGetValue(skinPath, out var skinData))
                {
                    var img = new Image();
                    img.LoadPngFromBuffer(skinData);
                    skins.Add(img);
                }
                else
                {
                    RuntimeInterop.Log(
                        $"Sprite map: missing skin file \"{skinPath}\" for panel \"{Name}\".");
                    skins.Add(null!);
                }
            }
        }
        var composed = SpriteMapCpu.ComposeSpriteMap(mapData, skins.ToArray());
        foreach (var s in skins) s?.Dispose();
        if (composed == null)
        {
            RuntimeInterop.Log($"Sprite map: composition failed for \"{mapPath}\".");
            return;
        }
        var tex = ImageTexture.CreateFromImage(composed);
        _baseBackgroundTexture = tex;
        TextureFilter = TextureFilterEnum.Nearest;
        SetBackgroundTexture(tex);
    }

    /// Computes the current frame index from the runtime elapsed time units
    /// (same scheme as the legacy Panel) and renders it when it changed.
    /// `force` renders frame 0 immediately on first apply.
    void AdvanceAnimationFrame(bool force)
    {
        if (_animName == null) return;
        var elapsed = RuntimeInterop.GetElapsedTimeUnits();
        if (!force && elapsed <= _animLastElapsed) return;
        _animLastElapsed = elapsed;

        var def = UiState.GetAnimation(_animName);
        if (!def.HasValue)
        {
            RuntimeInterop.Log($"ui: background animation '{_animName}' not registered");
            return;
        }
        if (!def.Value.TryGetProperty("frames", out var frames) || frames.ValueKind != JsonValueKind.Array)
            return;
        if (def.Value.TryGetProperty("duration", out var dd) && dd.ValueKind == JsonValueKind.Number)
            _animDurationTicks = Math.Max(1, (int)dd.GetDouble());
        if (def.Value.TryGetProperty("loop", out var ll) && ll.ValueKind == JsonValueKind.True)
            _animLoop = true;
        var frameSprites = new List<JsonElement>();
        foreach (var f in frames.EnumerateArray())
        {
            if (f.TryGetProperty("sprite", out var s))
                frameSprites.Add(s);
        }
        if (frameSprites.Count == 0) return;

        // Same frame scheme as the legacy Panel: frame 0 from the first
        // elapsed unit, ticksPerFrame = duration / frames.
        var ticksPerFrame = Math.Max(
            (int)Math.Round(_animDurationTicks / (double)frameSprites.Count), 1);
        var rawIndex = (int)((elapsed - 1) / ticksPerFrame);
        if (rawIndex < 0) rawIndex = 0;
        var frameIndex = _animLoop
            ? rawIndex % frameSprites.Count
            : Math.Min(rawIndex, frameSprites.Count - 1);

        var sprite = frameSprites[frameIndex];
        if (sprite.ValueKind == JsonValueKind.Object
            && sprite.TryGetProperty("kind", out var sk)
            && sk.ValueKind == JsonValueKind.String
            && sk.GetString() == "spriteMap")
        {
            // Sprite-map frames are validated, not rendered (baseline parity):
            // a missing map file reports a human-readable error.
            var mapPath = sprite.TryGetProperty("map", out var m) ? m.GetString() ?? "" : "";
            if (!RuntimeInterop.GetFileFromArchive().TryGetValue(mapPath, out _))
            {
                RuntimeInterop.Log(
                    $"Sprite map: missing TIFF file \"{mapPath}\" for panel \"{Name}\".");
            }
            return;
        }

        string path;
        if (sprite.ValueKind == JsonValueKind.String)
            path = sprite.GetString() ?? "";
        else if (sprite.ValueKind == JsonValueKind.Object
            && sprite.TryGetProperty("name", out var nm)
            && nm.ValueKind == JsonValueKind.String)
            path = nm.GetString() ?? "";
        else
            return;
        if (!force && path == _currentFramePath) return;
        _currentFramePath = path;
        var files = RuntimeInterop.GetFileFromArchive();
        if (!files.TryGetValue(path, out var data))
        {
            RuntimeInterop.Log($"ui: animation frame not found in archive: {path}");
            return;
        }
        var img = new Image();
        img.LoadPngFromBuffer(data);
        _animTexture = ImageTexture.CreateFromImage(img);
        SetBackgroundTexture(_animTexture);
    }

    string? _currentFramePath;

    public override void _Process(double delta)
    {
        if (_animName != null)
            AdvanceAnimationFrame(false);
        base._Process(delta);
    }

    /// Renders the background as a full-rect TextureRect behind the content
    /// (UiWindow is a plain Control, so it has no theme background of its own).
    void SetBackgroundTexture(Texture2D texture)
    {
        var tr = GetNodeOrNull<TextureRect>("background");
        if (tr == null)
        {
            tr = new TextureRect { Name = "background" };
            tr.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
            tr.StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered;
            tr.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
            tr.ZIndex = -10;
            tr.MouseFilter = Control.MouseFilterEnum.Ignore;
            AddChild(tr);
            MoveChild(tr, 0);
            tr.SetAnchorsPreset(LayoutPreset.FullRect, true);
        }
        tr.Texture = texture;
    }
}
