using Godot;

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

    /// The (col, row) cell containing the given local point, using the solved
    /// track sizes and gaps from the last layout. Out-of-bounds points clamp
    /// to the last cell.
    public (int Col, int Row) CellAt(Vector2 local)
    {
        var gapCol = Math.Max(0f, GapColumn);
        var gapRow = Math.Max(0f, GapRow);

        int col = Math.Max(0, _colSizes.Count - 1);
        float acc = 0f;
        for (int i = 0; i < _colSizes.Count; i++)
        {
            if (local.X < acc + _colSizes[i] || i + 1 >= _colSizes.Count)
            { col = i; break; }
            acc += _colSizes[i] + gapCol;
        }

        int row = Math.Max(0, _rowSizes.Count - 1);
        acc = 0f;
        for (int i = 0; i < _rowSizes.Count; i++)
        {
            if (local.Y < acc + _rowSizes[i] || i + 1 >= _rowSizes.Count)
            { row = i; break; }
            acc += _rowSizes[i] + gapRow;
        }

        return (col, row);
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
}
