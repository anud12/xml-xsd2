using System.Runtime.InteropServices;
using System.Text.Json;

namespace NewGameProject.Runtime;

public static class SectorInterop
{
    private const string LIB_NAME = "libxml_xsd2";

    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr sector_grid_ids();

    /// All sector grid ids known to the runtime (empty when none declared).
    public static string[] GetSectorGridIds()
    {
        var ptr = sector_grid_ids();
        if (ptr == IntPtr.Zero) return Array.Empty<string>();
        var result = new List<string>();
        int offset = 0;
        while (true)
        {
            IntPtr strPtr = Marshal.ReadIntPtr(ptr, offset);
            if (strPtr == IntPtr.Zero) break;
            result.Add(Marshal.PtrToStringAnsi(strPtr) ?? string.Empty);
            offset += IntPtr.Size;
        }
        return result.ToArray();
    }

    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr sector_grid_by_id([MarshalAs(UnmanagedType.LPStr)] string id);

    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
    private static extern void runtime_free_sector(IntPtr p);

    /// The computed state for one grid, or null when no such grid exists.
    public static SectorGrid? GetSectorGridById(string id)
    {
        IntPtr ptr = sector_grid_by_id(id);
        if (ptr == IntPtr.Zero) return null;
        try
        {
            string json = Marshal.PtrToStringAnsi(ptr) ?? string.Empty;
            return ParseSectorGrid(json);
        }
        finally
        {
            runtime_free_sector(ptr);
        }
    }

    private static SectorGrid ParseSectorGrid(string json)
    {
        var grid = new SectorGrid();
        if (string.IsNullOrEmpty(json)) return grid;
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.TryGetProperty("id", out var idProp))
            grid.Id = idProp.GetString() ?? string.Empty;

        if (root.TryGetProperty("cells", out var cellsProp) && cellsProp.ValueKind == JsonValueKind.Array)
        {
            foreach (var cellElem in cellsProp.EnumerateArray())
                grid.Cells.Add(ParseCell(cellElem));
        }

        if (root.TryGetProperty("portals", out var portalsProp) && portalsProp.ValueKind == JsonValueKind.Array)
        {
            foreach (var portalElem in portalsProp.EnumerateArray())
                grid.Portals.Add(ParsePortal(portalElem));
        }

        return grid;
    }

    private static SectorCell ParseCell(JsonElement cellElem)
    {
        var cell = new SectorCell
        {
            X = cellElem.TryGetProperty("x", out var x) ? x.GetInt32() : 0,
            Y = cellElem.TryGetProperty("y", out var y) ? y.GetInt32() : 0,
            Container = cellElem.TryGetProperty("container", out var c) ? c.GetString() ?? string.Empty : string.Empty,
        };
        if (cellElem.TryGetProperty("boundary_edges", out var be) && be.ValueKind == JsonValueKind.Array)
        {
            foreach (var edgeElem in be.EnumerateArray())
            {
                var edge = new BoundaryEdge
                {
                    Side = edgeElem.TryGetProperty("side", out var side) ? side.GetString() ?? string.Empty : string.Empty,
                };
                if (edgeElem.TryGetProperty("square", out var sq) && sq.ValueKind == JsonValueKind.Array)
                    edge.Square = ReadIntPair(sq);
                cell.BoundaryEdges.Add(edge);
            }
        }
        if (cellElem.TryGetProperty("openings", out var op) && op.ValueKind == JsonValueKind.Array)
        {
            foreach (var openElem in op.EnumerateArray())
            {
                var opening = new SectorOpening
                {
                    Side = openElem.TryGetProperty("side", out var side) ? side.GetString() ?? string.Empty : string.Empty,
                    Start = openElem.TryGetProperty("start", out var start) ? start.GetInt32() : 0,
                    Length = openElem.TryGetProperty("length", out var len) ? len.GetInt32() : 0,
                };
                if (openElem.TryGetProperty("cell", out var cl) && cl.ValueKind == JsonValueKind.Array)
                    opening.Cell = ReadIntPair(cl);
                cell.Openings.Add(opening);
            }
        }
        return cell;
    }

    private static Portal ParsePortal(JsonElement portalElem)
    {
        var portal = new Portal
        {
            Id = portalElem.TryGetProperty("id", out var id) ? id.GetString() ?? string.Empty : string.Empty,
        };
        if (portalElem.TryGetProperty("a", out var a) && a.ValueKind == JsonValueKind.Object)
            portal.A = ParsePortalSide(a);
        if (portalElem.TryGetProperty("b", out var b) && b.ValueKind == JsonValueKind.Object)
            portal.B = ParsePortalSide(b);
        return portal;
    }

    private static PortalSide ParsePortalSide(JsonElement sideElem)
    {
        var side = new PortalSide
        {
            Side = sideElem.TryGetProperty("side", out var s) ? s.GetString() ?? string.Empty : string.Empty,
            Span = sideElem.TryGetProperty("span", out var span) ? span.GetInt32() : 0,
            Length = sideElem.TryGetProperty("length", out var len) ? len.GetInt32() : 0,
        };
        if (sideElem.TryGetProperty("cell", out var cl) && cl.ValueKind == JsonValueKind.Array)
            side.Cell = ReadIntPair(cl);
        return side;
    }

    private static int[] ReadIntPair(JsonElement arrayElem)
    {
        var values = new List<int>();
        foreach (var elem in arrayElem.EnumerateArray())
        {
            if (elem.ValueKind == JsonValueKind.Number)
                values.Add(elem.GetInt32());
        }
        var result = new int[2];
        if (values.Count > 0) result[0] = values[0];
        if (values.Count > 1) result[1] = values[1];
        return result;
    }
}
