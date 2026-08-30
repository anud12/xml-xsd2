using System.Text.Json;

namespace GdUnit4.Examples.Basics.Setup.Sources.UI;

/// Parsed div layout: either the BoxContainer path ("column"/"row") or a
/// track grid (number N or a layout object { columns, rows, gap, rowFirst }).
public partial class UiGrid
{
    public class UiGridLayoutSpec
    {
        public enum LayoutMode { BoxColumn, BoxRow, Grid }
        public const LayoutMode ModeBoxColumn = LayoutMode.BoxColumn;
        public const LayoutMode ModeBoxRow = LayoutMode.BoxRow;
        public const LayoutMode ModeGrid = LayoutMode.Grid;
        public LayoutMode Mode = LayoutMode.BoxColumn;
        public List<UiGrid.TrackSpec> Columns = new();
        public List<UiGrid.TrackSpec> Rows = new();
        public float GapRow;
        public float GapColumn;
        public bool RowFirst = true;

        static readonly JsonValueKind JUndefined = JsonValueKind.Undefined;
        static readonly JsonValueKind JString = JsonValueKind.String;
        static readonly JsonValueKind JNumber = JsonValueKind.Number;
        static readonly JsonValueKind JObject = JsonValueKind.Object;
        static readonly JsonValueKind JArray = JsonValueKind.Array;
        static readonly JsonValueKind JTrue = JsonValueKind.True;

        public static UiGridLayoutSpec Parse(JsonElement opts)
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
                        && rf0.ValueKind == JsonValueKind.False);
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
                spec.Columns = new List<UiGrid.TrackSpec>();
                for (int i = 0; i < n; i++)
                    spec.Columns.Add(new UiGrid.TrackSpec { IsFixed = false, Scale = 1f });
                spec.Rows = new List<UiGrid.TrackSpec>
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
                    spec.Columns = new List<UiGrid.TrackSpec> { new() { IsFixed = false, Scale = 1f } };
                if (spec.Rows.Count == 0)
                    spec.Rows = new List<UiGrid.TrackSpec> { new() { IsFixed = false, Scale = 1f } };
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

        static List<UiGrid.TrackSpec> ParseTracks(JsonElement arr)
        {
            var outList = new List<UiGrid.TrackSpec>();
            foreach (var el in arr.EnumerateArray())
            {
                if (el.ValueKind == JNumber)
                {
                    outList.Add(new UiGrid.TrackSpec
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
                        ? new UiGrid.TrackSpec { IsFixed = false, Min = min, Max = max, Scale = scale }
                        : new UiGrid.TrackSpec { IsFixed = true, Fixed = min, Min = min, Max = max });
                }
            }
            return outList;
        }
    }
}
