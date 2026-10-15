using Godot;
using NewGameProject.UI;
using System.Text.Json;
using RuntimeInterop = NewGameProject.Runtime.RuntimeInterop;

namespace GdUnit4.Examples.Basics.Setup.Sources.UI;

/// Progress bar node: a window-like control that paints the `fill` texture
/// tiled along the progress axis (stretched across the perpendicular axis)
/// and cropped at the fill boundary, over an optional `track` texture tiled
/// the same way across the full rect. The value arrives already resolved per
/// tick by the Rust runtime as an integer count in 0..steps (the entity-bound
/// value quantized by `steps`); `options.progress` carries
/// { fill, track?, orientation, steps, value } and the fraction drawn is
/// value / steps. Each of fill/track is a static PNG archive path or an
/// animation reference (shaped like a panel background) whose frames advance
/// per runtime elapsed time unit.
public partial class UiWindow
{
    bool _isProgress;
    float _progressValue;
    bool _progressVertical;
    ProgressTexState? _progressFill;
    ProgressTexState? _progressTrack;
    readonly Dictionary<string, Texture2D> _progressTextures = new();

    /// Per-texture state for a progress fill/track: either a static texture,
    /// or an animation reference advanced per runtime elapsed time unit (the
    /// resolved `Tex` is the current frame's tile texture).
    class ProgressTexState
    {
        public bool IsAnimation;
        public string? AnimName;
        public int DurationTicks = 1;
        public bool Loop;
        public long LastElapsed = -1;
        public string? CurrentFramePath;
        public string? SourcePath;
        public Texture2D? Tex;
    }

    // Test hooks: the resolved source path (static path, or the animation's
    // current frame path) and the loaded texture for the progress fill/track.
    // Headless test viewports don't render textures, so these let a test verify
    // resolution (which frame/asset) without pixel comparison.
    internal string? ProgressFillSourcePath => _progressFill?.SourcePath;
    internal Texture2D? ProgressFillTexture => _progressFill?.Tex;
    internal string? ProgressTrackSourcePath => _progressTrack?.SourcePath;
    internal Texture2D? ProgressTrackTexture => _progressTrack?.Tex;

    void ApplyProgress(UiNodeData node)
    {
        _isWindow = true;
        var opts = ParseOptions(node);
        ParseWindowGeometry(opts);
        ApplyBackground(opts);

        float value = 0f;
        int steps = 100;
        JsonElement? fill = null;
        JsonElement? track = null;
        bool vertical = false;
        if (opts.ValueKind == JsonValueKind.Object
            && opts.TryGetProperty("progress", out var p)
            && p.ValueKind == JsonValueKind.Object)
        {
            if (TryNum(p, "value", out var v)) value = v;
            if (TryNum(p, "steps", out var s)) steps = Math.Max(1, (int)s);
            if (p.TryGetProperty("fill", out var f)) fill = f;
            if (p.TryGetProperty("track", out var t)) track = t;
            vertical = TryStr(p, "orientation") == "vertical";
        }
        _isProgress = true;
        _progressValue = Mathf.Clamp(steps > 0 ? value / steps : 0f, 0f, 1f);
        _progressVertical = vertical;
        _progressFill = ResolveProgressTex(fill);
        _progressTrack = ResolveProgressTex(track);
        // Render frame 0 immediately for any animated texture: in test mode the
        // runtime loop is off, so elapsed time never advances and _Process would
        // never advance a frame.
        AdvanceProgressTex(_progressFill, true);
        AdvanceProgressTex(_progressTrack, true);
        // Crisp pixel-art tiling: no bilinear bleed at tile edges or in the
        // cross-axis stretch.
        TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
        QueueRedraw();
    }

    /// Resolves a fill/track reference: a string is a static PNG archive path;
    /// an object with "name" is an animation reference (frames advance per
    /// runtime elapsed time unit).
    ProgressTexState? ResolveProgressTex(JsonElement? el)
    {
        if (el == null) return null;
        var e = el.Value;
        if (e.ValueKind == JsonValueKind.String)
        {
            var path = e.GetString();
            if (string.IsNullOrEmpty(path)) return null;
            return new ProgressTexState { IsAnimation = false, SourcePath = path, Tex = LoadProgressTexture(path) };
        }
        if (e.ValueKind == JsonValueKind.Object
            && e.TryGetProperty("name", out var n)
            && n.ValueKind == JsonValueKind.String)
        {
            var st = new ProgressTexState { IsAnimation = true, AnimName = n.GetString() };
            if (e.TryGetProperty("duration", out var d) && d.ValueKind == JsonValueKind.Number)
                st.DurationTicks = Math.Max(1, (int)d.GetDouble());
            if (e.TryGetProperty("loop", out var l) && l.ValueKind == JsonValueKind.True)
                st.Loop = true;
            return st;
        }
        return null;
    }

    /// Loads a PNG texture from the current archive, caching by path.
    Texture2D? LoadProgressTexture(string path)
    {
        if (_progressTextures.TryGetValue(path, out var cached)) return cached;
        if (!RuntimeInterop.GetFileFromArchive().TryGetValue(path, out var data))
        {
            RuntimeInterop.Log($"ui: progress texture not found in archive: {path}");
            return null;
        }
        var img = new Image();
        img.LoadPngFromBuffer(data);
        var tex = ImageTexture.CreateFromImage(img);
        _progressTextures[path] = tex;
        return tex;
    }

    /// Advances a progress fill/track animation to the current runtime frame and
    /// swaps in the resolved tile texture. `force` renders frame 0 immediately on
    /// first apply.
    void AdvanceProgressTex(ProgressTexState? st, bool force)
    {
        if (st == null || !st.IsAnimation || st.AnimName == null) return;
        var elapsed = RuntimeInterop.GetElapsedTimeUnits();
        if (!force && elapsed <= st.LastElapsed) return;
        st.LastElapsed = elapsed;

        var def = UiState.GetAnimation(st.AnimName);
        if (!def.HasValue)
        {
            RuntimeInterop.Log($"ui: progress animation '{st.AnimName}' not registered");
            return;
        }
        if (!def.Value.TryGetProperty("frames", out var frames) || frames.ValueKind != JsonValueKind.Array)
            return;
        if (def.Value.TryGetProperty("duration", out var dd) && dd.ValueKind == JsonValueKind.Number)
            st.DurationTicks = Math.Max(1, (int)dd.GetDouble());
        if (def.Value.TryGetProperty("loop", out var ll) && ll.ValueKind == JsonValueKind.True)
            st.Loop = true;
        // Each frame is { sprite: <ref> }; the sprite is a PNG archive path
        // (or a { name } ref). Sprite-map frames are not supported for progress
        // tiling and are skipped.
        var framePaths = new List<string>();
        foreach (var f in frames.EnumerateArray())
        {
            var sprite = f.ValueKind == JsonValueKind.Object
                && f.TryGetProperty("sprite", out var s)
                    ? s
                    : default;
            if (sprite.ValueKind == JsonValueKind.String)
                framePaths.Add(sprite.GetString() ?? "");
            else if (sprite.ValueKind == JsonValueKind.Object
                && sprite.TryGetProperty("name", out var nm)
                && nm.ValueKind == JsonValueKind.String)
                framePaths.Add(nm.GetString() ?? "");
        }
        if (framePaths.Count == 0) return;

        // Same frame scheme as the background animation: frame 0 from the first
        // elapsed unit, ticksPerFrame = duration / frames.
        var ticksPerFrame = Math.Max(
            (int)Math.Round(st.DurationTicks / (double)framePaths.Count), 1);
        var rawIndex = (int)((elapsed - 1) / ticksPerFrame);
        if (rawIndex < 0) rawIndex = 0;
        var frameIndex = st.Loop
            ? rawIndex % framePaths.Count
            : Math.Min(rawIndex, framePaths.Count - 1);

        var path = framePaths[frameIndex];
        if (!force && path == st.CurrentFramePath) return;
        st.CurrentFramePath = path;
        st.SourcePath = path;
        st.Tex = LoadProgressTexture(path);
        QueueRedraw();
    }

    /// Draws the progress bar: the track tiled across the full rect, then the
    /// fill tiled across the filled fraction. Only the progress axis tiles;
    /// the tile is uniformly scaled (aspect ratio preserved) so it covers the
    /// full extent on the perpendicular axis.
    internal void DrawProgress()
    {
        if (!_isProgress) return;
        var size = Size;
        var (trackAxis, trackCross) = _progressVertical
            ? (size.Y, size.X)
            : (size.X, size.Y);
        var trackTex = _progressTrack?.Tex;
        if (trackTex != null)
            DrawProgressTiles(trackTex, trackAxis, trackCross);
        var value = Mathf.Clamp(_progressValue, 0f, 1f);
        var (fillAxis, fillCross) = _progressVertical
            ? (size.Y * value, size.X)
            : (size.X * value, size.Y);
        var fillTex = _progressFill?.Tex;
        if (fillTex != null && fillAxis > 0f && fillCross > 0f)
            DrawProgressTiles(fillTex, fillAxis, fillCross);
    }

    /// Tiles `tex` across an `axisLen` x `crossLen` rect starting at the local
    /// origin, preserving aspect ratio: the tile is scaled uniformly so its
    /// cross axis covers `crossLen`, then tiles step along the progress axis
    /// at the scaled size (the last partial tile is cropped from the source,
    /// never squashed).
    void DrawProgressTiles(Texture2D tex, float axisLen, float crossLen)
    {
        var tw = (float)tex.GetWidth();
        var th = (float)tex.GetHeight();
        if (tw <= 0f || th <= 0f) return;
        var (tileAxis, tileCross) = _progressVertical ? (th, tw) : (tw, th);
        var scale = crossLen / tileCross;
        var step = tileAxis * scale;
        for (var i = 0; ; i++)
        {
            var start = i * step;
            if (start >= axisLen) break;
            var destLen = Mathf.Min(step, axisLen - start);
            var srcLen = destLen / scale;
            if (_progressVertical)
                DrawTextureRectRegion(tex,
                    new Rect2(0, start, crossLen, destLen),
                    new Rect2(0, 0, tileCross, srcLen));
            else
                DrawTextureRectRegion(tex,
                    new Rect2(start, 0, destLen, crossLen),
                    new Rect2(0, 0, srcLen, tileCross));
        }
    }
}
