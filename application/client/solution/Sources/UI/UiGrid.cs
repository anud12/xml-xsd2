using Godot;
using System.Linq;

namespace GdUnit4.Examples.Basics.Setup.Sources.UI;

/// A grid layout container with proportional/fixed/auto tracks (CSS-grid-like).
///
/// Tracks are solved independently per axis: fixed tracks take their px size,
/// scaled tracks share the remaining space proportionally, and every track is
/// clamped by its min/max. Children flow row-major (or column-major with
/// RowFirst == false) into the cell matrix; each cell is the intersection of
/// its row and column tracks, with gaps between tracks.
///
/// Layout is driven by the container's SortChildren notification (Godot's
/// C# API does not expose an overridable SortChildren), so the grid also
/// re-lays-out on resize; children are placed with explicit Position/Size.
public partial class UiGrid : Container
{
    public List<TrackSpec> Columns = new();
    public List<TrackSpec> Rows = new();
    public float GapRow;
    public float GapColumn;
    public bool RowFirst = true;

    // Concrete pixel sizes from the last layout (no gaps included).
    List<float> _colSizes = new();
    List<float> _rowSizes = new();

    /// Apply a parsed layout spec to this grid container.
    public void Configure(UiGridLayoutSpec spec)
    {
        Columns = spec.Columns;
        Rows = spec.Rows;
        GapRow = spec.GapRow;
        GapColumn = spec.GapColumn;
        RowFirst = spec.RowFirst;
    }

    public override void _Notification(int what)
    {
        if (what == (int)(long)Container.NotificationSortChildren)
            Layout();
    }

    /// The size the container asks for: the sum of the guaranteed track sizes
    /// (fixed sizes, or scaled tracks' mins) plus gaps, clamped by the
    /// container's own custom minimum.
    public override Vector2 _GetMinimumSize()
    {
        var min = GridMinimumSize();
        return new Vector2(
            Mathf.Max(min.X, (float)CustomMinimumSize.X),
            Mathf.Max(min.Y, (float)CustomMinimumSize.Y));
    }

    void Layout()
    {
        _colSizes = SolveTracks(Columns, Size.X);
        _rowSizes = SolveTracks(Rows, Size.Y);
        PlaceChildren();
    }

    /// Minimum size: the sum of the guaranteed track sizes (fixed sizes, or
    /// scaled tracks' mins) plus gaps.
    public Vector2 GridMinimumSize()
    {
        return new Vector2(
            TrackTotal(Columns, GapColumn),
            TrackTotal(Rows, GapRow));
    }

    static float TrackTotal(List<TrackSpec> tracks, float gap)
    {
        float total = 0f;
        for (int i = 0; i < tracks.Count; i++)
        {
            var t = tracks[i];
            // A scaled track only guarantees its min; a fixed track guarantees
            // its size.
            total += t.IsFixed ? Math.Max(0f, t.Fixed) : Math.Max(0f, t.Min);
            if (i + 1 < tracks.Count) total += Math.Max(0f, gap);
        }
        return total;
    }

    /// Solve track pixel sizes for a single axis. Fixed tracks are clamped and
    /// kept; scaled tracks split the leftover proportionally (clamped); if the
    /// sum falls short of `available`, the surplus is handed out to tracks that
    /// can still grow (scaled ones first, then any track with headroom).
    internal static List<float> SolveTracks(List<TrackSpec> specs, float available)
    {
        var sizes = new float[specs.Count];
        float sum = 0f;
        for (int i = 0; i < specs.Count; i++)
        {
            var s = specs[i];
            if (s.IsFixed)
            {
                sizes[i] = Mathf.Clamp(s.Fixed, s.Min, s.Max);
                sum += sizes[i];
            }
            else
            {
                sizes[i] = s.Min;
                sum += s.Min;
            }
        }
        var remaining = Math.Max(0f, available - sum);
        float scaleTotal = 0f;
        for (int i = 0; i < specs.Count; i++)
            if (!specs[i].IsFixed && specs[i].Scale > 0f) scaleTotal += specs[i].Scale;
        if (scaleTotal > 0f)
        {
            for (int i = 0; i < specs.Count; i++)
            {
                if (specs[i].IsFixed || specs[i].Scale <= 0f) continue;
                var s = specs[i];
                sizes[i] = Mathf.Clamp(s.Min + remaining * (s.Scale / scaleTotal), s.Min, s.Max);
            }
            sum = 0f;
            for (int i = 0; i < specs.Count; i++) sum += sizes[i];
        }
        // Distribute any surplus (e.g. no scaled tracks) to tracks with headroom.
        float surplus = Math.Max(0f, available - sum);
        while (surplus > 1e-3f)
        {
            bool grew = false;
            for (int i = 0; i < specs.Count && surplus > 1e-3f; i++)
            {
                var s = specs[i];
                var room = s.Max - sizes[i];
                if (room <= 1e-3f) continue;
                var give = Math.Min(room, surplus);
                sizes[i] += give;
                surplus -= give;
                sum += give;
                grew = true;
            }
            if (!grew) break;
        }
        return sizes.ToList();
    }

    void PlaceChildren()
    {
        var children = new List<Control>();
        for (int i = 0; i < GetChildCount(); i++)
            if (GetChild(i) is Control c) children.Add(c);

        int cols = Math.Max(1, _colSizes.Count);
        int rows = Math.Max(1, _rowSizes.Count);
        for (int n = 0; n < children.Count; n++)
        {
            int r, cIdx;
            if (RowFirst) { cIdx = n % cols; r = n / cols; }
            else          { r = n % rows; cIdx = n / rows; }
            r = Math.Min(r, rows - 1);
            cIdx = Math.Min(cIdx, cols - 1);

            var child = children[n];
            child.Position = new Vector2(CellX(cIdx), CellY(r));
            child.CustomMinimumSize = Vector2.Zero;
            child.Size = new Vector2(_colSizes[cIdx], _rowSizes[r]);
        }
    }

    float CellX(int col)
    {
        float x = 0f;
        for (int i = 0; i <= col && i < _colSizes.Count; i++)
        {
            x += _colSizes[i];
            if (i < col) x += Math.Max(0f, GapColumn);
        }
        return x;
    }

    float CellY(int row)
    {
        float y = 0f;
        for (int i = 0; i <= row && i < _rowSizes.Count; i++)
        {
            y += _rowSizes[i];
            if (i < row) y += Math.Max(0f, GapRow);
        }
        return y;
    }

    /// One track spec: either fixed px, or scaled with min/max clamps.
    public struct TrackSpec
    {
        public float Fixed;
        public float Min;
        public float Max;
        public float Scale;
        public bool IsFixed;
    }

    /// Parsed div layout: either the BoxContainer path ("column"/"row") or a
    /// track grid (number N or a layout object { columns, rows, gap, rowFirst }).
    public class UiGridLayoutSpec
    {
        public enum LayoutMode { BoxColumn, BoxRow, Grid }
        public const LayoutMode ModeBoxColumn = LayoutMode.BoxColumn;
        public const LayoutMode ModeBoxRow = LayoutMode.BoxRow;
        public const LayoutMode ModeGrid = LayoutMode.Grid;
        public LayoutMode Mode = LayoutMode.BoxColumn;
        public List<TrackSpec> Columns = new();
        public List<TrackSpec> Rows = new();
        public float GapRow;
        public float GapColumn;
        public bool RowFirst = true;

        static readonly System.Text.Json.JsonValueKind JUndefined = System.Text.Json.JsonValueKind.Undefined;
        static readonly System.Text.Json.JsonValueKind JString = System.Text.Json.JsonValueKind.String;
        static readonly System.Text.Json.JsonValueKind JNumber = System.Text.Json.JsonValueKind.Number;
        static readonly System.Text.Json.JsonValueKind JObject = System.Text.Json.JsonValueKind.Object;
        static readonly System.Text.Json.JsonValueKind JArray = System.Text.Json.JsonValueKind.Array;
        static readonly System.Text.Json.JsonValueKind JTrue = System.Text.Json.JsonValueKind.True;

        public static UiGridLayoutSpec Parse(System.Text.Json.JsonElement opts)
        {
            var spec = new UiGridLayoutSpec();
            if (opts.ValueKind == JUndefined
                || !opts.TryGetProperty("layout", out var layout))
                return spec; // default: column box
            if (layout.ValueKind == JString)
            {
                spec.Mode = layout.GetString() == "row" ? LayoutMode.BoxRow : LayoutMode.BoxColumn;
                return spec;
            }
            if (layout.ValueKind == JObject)
            {
                // Box-style layout: only rowFirst (no columns/rows/gap) — a
                // single uniform track flowing row-first (row) or column-first
                // (column). Full grid objects declare columns/rows.
                bool hasTracks = false;
                if (layout.TryGetProperty("columns", out var cc) && cc.ValueKind == JArray) hasTracks = true;
                if (layout.TryGetProperty("rows", out var rr) && rr.ValueKind == JArray) hasTracks = true;
                if (!hasTracks)
                {
                    bool rf = !(layout.TryGetProperty("rowFirst", out var rf0)
                        && rf0.ValueKind == System.Text.Json.JsonValueKind.False);
                    spec.RowFirst = rf;
                    spec.Mode = rf ? LayoutMode.BoxRow : LayoutMode.BoxColumn;
                    return spec;
                }
            }
            spec.Mode = LayoutMode.Grid;
            if (layout.ValueKind == JNumber)
            {
                // N equal auto columns, single implicit auto row.
                int n = Math.Max(1, (int)layout.GetDouble());
                spec.Columns = new List<TrackSpec>();
                for (int i = 0; i < n; i++)
                    spec.Columns.Add(new TrackSpec { IsFixed = false, Scale = 1f });
                spec.Rows = new List<TrackSpec>
                {
                    new() { IsFixed = false, Scale = 1f }
                };
                return spec;
            }
            if (layout.ValueKind == JObject)
            {
                if (layout.TryGetProperty("columns", out var c)
                    && c.ValueKind == JArray)
                    spec.Columns = ParseTracks(c);
                if (layout.TryGetProperty("rows", out var r)
                    && r.ValueKind == JArray)
                    spec.Rows = ParseTracks(r);
                if (spec.Columns.Count == 0)
                    spec.Columns = new List<TrackSpec> { new() { IsFixed = false, Scale = 1f } };
                if (spec.Rows.Count == 0)
                    spec.Rows = new List<TrackSpec> { new() { IsFixed = false, Scale = 1f } };
                if (layout.TryGetProperty("gap", out var g)
                    && g.ValueKind == JObject)
                {
                    spec.GapRow = g.TryGetProperty("row", out var gr)
                        && gr.ValueKind == JNumber
                        ? (float)gr.GetDouble() : 0f;
                    spec.GapColumn = g.TryGetProperty("column", out var gc)
                        && gc.ValueKind == JNumber
                        ? (float)gc.GetDouble() : 0f;
                }
                if (layout.TryGetProperty("rowFirst", out var rf)
                    && rf.ValueKind == JTrue)
                    spec.RowFirst = true;
            }
            return spec;
        }

        static List<TrackSpec> ParseTracks(System.Text.Json.JsonElement arr)
        {
            var outList = new List<TrackSpec>();
            foreach (var el in arr.EnumerateArray())
            {
                if (el.ValueKind == JNumber)
                {
                    outList.Add(new TrackSpec
                    {
                        IsFixed = true,
                        Fixed = (float)el.GetDouble(),
                        Min = 0f,
                        Max = float.PositiveInfinity
                    });
                }
                else if (el.ValueKind == JObject)
                {
                    float min = 0f, max = float.PositiveInfinity, scale = 0f;
                    if (el.TryGetProperty("min", out var mn)
                        && mn.ValueKind == JNumber)
                        min = (float)mn.GetDouble();
                    if (el.TryGetProperty("max", out var mx)
                        && mx.ValueKind == JNumber)
                        max = (float)mx.GetDouble();
                    if (el.TryGetProperty("scale", out var sc)
                        && sc.ValueKind == JNumber)
                        scale = (float)sc.GetDouble();
                    outList.Add(scale > 0f
                        ? new TrackSpec { IsFixed = false, Min = min, Max = max, Scale = scale }
                        : new TrackSpec { IsFixed = true, Fixed = min, Min = min, Max = max });
                }
            }
            return outList;
        }
    }
}
