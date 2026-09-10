using System.Text.Json;

namespace NewGameProject.Runtime;

static class LayoutParser
{
    internal static Layout? Parse(JsonElement elem)
    {
        var layout = new Layout();

        if (elem.TryGetProperty("columns", out var cols) && cols.ValueKind == JsonValueKind.Array)
        {
            var defs = new List<TrackDefinition>();
            foreach (var col in cols.EnumerateArray())
                defs.Add(new TrackDefinition
                {
                    min = Extract.Int(col, "min"),
                    max = Extract.Int(col, "max"),
                    weight = Extract.Int(col, "weight"),
                    align = ParseAlign(Extract.String(col, "align"))
                });
            layout.Columns = defs.ToArray();
        }

        if (elem.TryGetProperty("rowFirst", out var rf))
            layout.RowFirst = rf.GetBoolean();

        if (elem.TryGetProperty("reverse", out var rv))
            layout.ReverseOrder = rv.GetBoolean();

        if (elem.TryGetProperty("gap", out var g) && g.ValueKind == JsonValueKind.Object)
            layout.gap = new Gap { Row = Extract.Int(g, "row") ?? 0, Column = Extract.Int(g, "column") ?? 0 };

        return layout;
    }

    static Align? ParseAlign(string? v)
    {
        if (v == "end") return Align.End;
        return Align.Start;
    }
}
