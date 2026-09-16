namespace NewGameProject.Runtime;

/// The full computed state of one sector grid: the occupied cells (with their
/// boundary edges + openings) and the portals linking neighbouring sectors.
public class SectorGrid
{
    public string Id = string.Empty;
    public List<SectorCell> Cells = new();
    public List<Portal> Portals = new();
}

/// A grid square owned by a sector, carrying the boundary edges it exposes and
/// the openings declared on them.
public class SectorCell
{
    public int X;
    public int Y;
    public string Container = string.Empty;
    public List<BoundaryEdge> BoundaryEdges = new();
    public List<SectorOpening> Openings = new();

    public int SquareX => X;
    public int SquareY => Y;
}

/// A {square, side} whose adjacent square is not part of the same footprint.
public class BoundaryEdge
{
    public int[] Square = Array.Empty<int>();
    public string Side = string.Empty;

    public int SquareX => Square.Length > 0 ? Square[0] : 0;
    public int SquareY => Square.Length > 1 ? Square[1] : 0;
}

/// An opening declared on a boundary edge; `start`/`length` index interior
/// cells along the edge.
public class SectorOpening
{
    public int[] Cell = Array.Empty<int>();
    public string Side = string.Empty;
    public int Start;
    public int Length;

    public int CellX => Cell.Length > 0 ? Cell[0] : 0;
    public int CellY => Cell.Length > 1 ? Cell[1] : 0;
}

/// A portal joining two sectors along facing boundary edges.
public class Portal
{
    public string Id = string.Empty;
    public PortalSide A = new();
    public PortalSide B = new();
}

/// One sector's side of a portal: the footprint square + side it sits on and
/// the matched span (start/length in interior cells along that side).
public class PortalSide
{
    public int[] Cell = Array.Empty<int>();
    public string Side = string.Empty;
    public int Span;
    public int Length;

    public int CellX => Cell.Length > 0 ? Cell[0] : 0;
    public int CellY => Cell.Length > 1 ? Cell[1] : 0;
}
