using Godot;
using System.Text.Json;

namespace NewGameProject.UI;

/// The spatial world model (rooms + portals) pulled from the runtime over
/// FFI (`runtime_fetch_world_state`). Room `Points` are LOCAL room coords with
/// (0,0) at the room center; a local point maps to world as
/// `Origin + R(Rotation) * point`. Edge i is the segment
/// `Points[i] -> Points[(i+1) % n]`; a portal range (t0, t1) is the 0..1 span
/// along that edge where the portal exists.
public class WorldData
{
    public List<RoomData> Rooms { get; set; } = new();
    public List<PortalData> Portals { get; set; } = new();

    /// Fetches and parses the world state JSON; null on FFI/parse failure.
    public static WorldData? Fetch()
    {
        var json = NewGameProject.Runtime.RuntimeInterop.FetchWorldState();
        if (string.IsNullOrEmpty(json)) return null;
        try
        {
            return Parse(json);
        }
        catch (Exception ex)
        {
            NewGameProject.Runtime.RuntimeInterop.Log($"world: parse failed: {ex.Message}");
            return null;
        }
    }

    public static WorldData Parse(string json)
    {
        var doc = JsonDocument.Parse(json);
        var data = new WorldData();
        if (doc.RootElement.TryGetProperty("rooms", out var rooms))
        {
            foreach (var el in rooms.EnumerateArray())
                data.Rooms.Add(ParseRoom(el));
        }
        if (doc.RootElement.TryGetProperty("portals", out var portals))
        {
            foreach (var el in portals.EnumerateArray())
                data.Portals.Add(ParsePortal(el));
        }
        return data;
    }

    static RoomData ParseRoom(JsonElement el)
    {
        var room = new RoomData
        {
            Id = GetStringProp(el, "id"),
            Terrain = GetStringProp(el, "terrain"),
            Rotation = (float)GetNumProp(el, "rotation", 0),
        };
        room.Origin = ReadPoint(el, "origin");
        if (el.TryGetProperty("points", out var pts))
        {
            foreach (var p in pts.EnumerateArray())
                room.Points.Add(ReadPointValue(p));
        }
        return room;
    }

    static PortalData ParsePortal(JsonElement el)
    {
        var portal = new PortalData { Id = GetStringProp(el, "id") };
        if (el.TryGetProperty("from", out var from)) portal.From = ParseSide(from);
        if (el.TryGetProperty("to", out var to)) portal.To = ParseSide(to);
        return portal;
    }

    static PortalSideData ParseSide(JsonElement el)
    {
        var side = new PortalSideData
        {
            Room = GetStringProp(el, "room"),
            Edge = (int)GetNumProp(el, "edge", 0),
        };
        side.Range = ReadPoint(el, "range");
        return side;
    }

    /// Tolerant 2D point parse: accepts [x, y] arrays or {x:, y:} objects.
    static Vector2 ReadPoint(JsonElement el, string prop)
    {
        if (!el.TryGetProperty(prop, out var p)) return Vector2.Zero;
        return ReadPointValue(p);
    }

    static Vector2 ReadPointValue(JsonElement p)
    {
        if (p.ValueKind == JsonValueKind.Array && p.GetArrayLength() >= 2)
            return new Vector2((float)p[0].GetDouble(), (float)p[1].GetDouble());
        if (p.ValueKind == JsonValueKind.Object)
        {
            var x = p.TryGetProperty("x", out var px) ? (float)px.GetDouble() : 0f;
            var y = p.TryGetProperty("y", out var py) ? (float)py.GetDouble() : 0f;
            return new Vector2(x, y);
        }
        return Vector2.Zero;
    }

    static string GetStringProp(JsonElement el, string prop)
    {
        return el.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString() ?? ""
            : "";
    }

    static double GetNumProp(JsonElement el, string prop, double fallback)
    {
        return el.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.Number
            ? v.GetDouble()
            : fallback;
    }

    /// Maps a room-local point to world space: `Origin + R(Rotation) * local`.
    public static Vector2 RoomToWorld(RoomData room, Vector2 local)
    {
        var c = Mathf.Cos(room.Rotation);
        var s = Mathf.Sin(room.Rotation);
        return room.Origin + new Vector2(
            c * local.X - s * local.Y,
            s * local.X + c * local.Y);
    }

    /// World-space endpoints of edge `edge` (segment Points[edge] ->
    /// Points[(edge+1) % n]) of the given room.
    public static (Vector2 a, Vector2 b) PortalSegment(RoomData room, int edge)
    {
        if (room.Points.Count == 0) return (Vector2.Zero, Vector2.Zero);
        var n = room.Points.Count;
        var a = room.Points[((edge % n) + n) % n];
        var b = room.Points[(((edge + 1) % n) + n) % n];
        return (RoomToWorld(room, a), RoomToWorld(room, b));
    }

    public static Vector2 PointOnSegment(Vector2 a, Vector2 b, float t)
    {
        return a + (b - a) * t;
    }
}

public class RoomData
{
    public string Id { get; set; } = "";
    public string Terrain { get; set; } = "";
    public Vector2 Origin { get; set; }
    public float Rotation { get; set; }
    public List<Vector2> Points { get; set; } = new();
}

public class PortalData
{
    public string Id { get; set; } = "";
    public PortalSideData From { get; set; } = new();
    public PortalSideData To { get; set; } = new();
}

public class PortalSideData
{
    public string Room { get; set; } = "";
    public int Edge { get; set; }
    public Vector2 Range { get; set; }
}
