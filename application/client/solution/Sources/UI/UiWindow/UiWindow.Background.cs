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
    /// elapsed time unit.
    void ApplyBackground(JsonElement opts)
    {
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
        var framePaths = new List<string>();
        foreach (var f in frames.EnumerateArray())
        {
            if (f.TryGetProperty("sprite", out var s) && s.ValueKind == JsonValueKind.String)
                framePaths.Add(s.GetString() ?? "");
        }
        if (framePaths.Count == 0) return;

        // Same frame scheme as the legacy Panel: frame 0 from the first
        // elapsed unit, ticksPerFrame = duration / frames.
        var ticksPerFrame = Math.Max(
            (int)Math.Round(_animDurationTicks / (double)framePaths.Count), 1);
        var rawIndex = (int)((elapsed - 1) / ticksPerFrame);
        if (rawIndex < 0) rawIndex = 0;
        var frameIndex = _animLoop
            ? rawIndex % framePaths.Count
            : Math.Min(rawIndex, framePaths.Count - 1);

        var path = framePaths[frameIndex];
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
