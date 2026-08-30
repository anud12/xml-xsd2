using Godot;
using NewGameProject.UI;

namespace GdUnit4.Examples.Basics.Setup.Sources.UI;

/// The RTS world view for a `canvas` UI node: renders rooms (Polygon2D),
/// portal edge ranges (thick yellow Line2D + midpoint dot), and entities that
/// carry a numberMap x/y (8px dot, colored by their room).
///
/// v1 unit scope: any entity whose numberMap has BOTH x and y is drawn, using
/// room-local coordinates, in the room named by its textMap "room" field when
/// present, otherwise in this canvas's active room.
public partial class UiWorldCanvas : Node2D
{
    static readonly Color[] RoomPalette =
    {
        new(0.36f, 0.62f, 0.36f, 0.30f),
        new(0.58f, 0.48f, 0.36f, 0.30f),
        new(0.36f, 0.52f, 0.62f, 0.30f),
        new(0.55f, 0.40f, 0.55f, 0.30f),
        new(0.60f, 0.56f, 0.35f, 0.30f),
        new(0.40f, 0.55f, 0.50f, 0.30f),
    };

    static readonly Color PortalColor = new(1f, 0.92f, 0.25f, 1f);
    static readonly Color PortalOutline = new(0.75f, 0.65f, 0.10f, 1f);
    const int DotSegments = 12;
    const float UnitDotRadius = 8f;

    readonly Dictionary<string, Color> _roomColors = new();
    WorldData? _data;
    Node2D? _world;
    Camera2D? _camera;
    string _activeRoomId = "";

    /// Clears all rendered nodes and redraws every room, portal, and unit.
    public void Rebuild(WorldData data)
    {
        _data = data;
        foreach (var child in GetChildren().ToArray())
            child.QueueFree();
        _roomColors.Clear();
        var world = EnsureWorld();

        foreach (var room in data.Rooms)
        {
            _roomColors[room.Id] = TerrainColor(room.Terrain);
            world.AddChild(BuildRoom(room));
        }

        foreach (var portal in data.Portals)
        {
            foreach (var side in new[] { portal.From, portal.To })
            {
                var room = FindRoom(side.Room);
                if (room == null) continue;
                var (a, b) = WorldData.PortalSegment(room, side.Edge);
                world.AddChild(BuildPortalMarker(
                    WorldData.PointOnSegment(a, b, side.Range.X),
                    WorldData.PointOnSegment(a, b, side.Range.Y)));
            }
        }

        DrawUnits(world);
        CenterCamera();
    }

    /// Re-centers the camera on the given room's world origin (ignored when
    /// unknown).
    public void SetRoom(string roomId)
    {
        _activeRoomId = roomId;
        var room = FindRoom(roomId);
        if (room != null && IsInstanceValid(_camera))
            _camera!.GlobalPosition = room.Origin;
    }

    RoomData? FindRoom(string id)
    {
        if (_data == null) return null;
        return _data.Rooms.Find(r => r.Id == id);
    }

    Node2D EnsureWorld()
    {
        if (_world == null || !IsInstanceValid(_world))
        {
            _world = new Node2D { Name = "world" };
            AddChild(_world);
        }
        return _world;
    }

    Camera2D EnsureCamera()
    {
        if (_camera == null || !IsInstanceValid(_camera))
        {
            _camera = new Camera2D { Name = "camera", Enabled = true };
            AddChild(_camera);
        }
        return _camera;
    }

    void CenterCamera()
    {
        var camera = EnsureCamera();
        var room = FindRoom(_activeRoomId)
            ?? (_data?.Rooms.Count > 0 ? _data!.Rooms[0] : null);
        camera.GlobalPosition = room?.Origin ?? Godot.Vector2.Zero;
    }

    /// Stable color per terrain name.
    static Color TerrainColor(string terrain)
    {
        var hash = 0;
        foreach (var c in terrain) hash = hash * 31 + c;
        return RoomPalette[Math.Abs(hash) % RoomPalette.Length];
    }

    static Color TerrainOutline(string terrain)
    {
        var c = TerrainColor(terrain);
        return new Color(c.R * 0.6f, c.G * 0.6f, c.B * 0.6f, 1f);
    }

    Polygon2D BuildRoom(RoomData room)
    {
        var poly = new Polygon2D { Name = "room:" + room.Id };
        var fill = new List<Godot.Vector2>();
        var outline = new List<Godot.Vector2>();
        for (var i = 0; i < room.Points.Count; i++)
        {
            var w = WorldData.RoomToWorld(room, room.Points[i]);
            fill.Add(w);
            outline.Add(w);
        }
        // Close the outline by wrapping back to the first point.
        if (room.Points.Count > 2)
            outline.Add(WorldData.RoomToWorld(room, room.Points[0]));
        poly.Polygon = fill.ToArray();
        poly.Color = _roomColors.TryGetValue(room.Id, out var c)
            ? c
            : TerrainColor(room.Terrain);
        var outlineNode = new Line2D
        {
            Width = 2f,
            DefaultColor = TerrainOutline(room.Terrain),
            Closed = true,
        };
        for (var i = 0; i < outline.Count; i++)
            outlineNode.AddPoint(outline[i]);
        poly.AddChild(outlineNode);
        return poly;
    }

    Node BuildPortalMarker(Godot.Vector2 from, Godot.Vector2 to)
    {
        var holder = new Node2D { Name = "portal" };
        var line = new Line2D
        {
            Width = 4f,
            DefaultColor = PortalColor,
        };
        line.AddPoint(from);
        line.AddPoint(to);
        holder.AddChild(line);
        holder.AddChild(BuildDot((from + to) / 2, 5f, PortalColor, PortalOutline));
        return holder;
    }

    Polygon2D BuildDot(Godot.Vector2 pos, float radius, Color color, Color outline)
    {
        var poly = new Polygon2D();
        var pts = new Godot.Vector2[DotSegments];
        for (var i = 0; i < DotSegments; i++)
        {
            var a = Mathf.Tau * i / DotSegments;
            pts[i] = pos + new Godot.Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius;
        }
        poly.Polygon = pts;
        poly.Color = color;
        return poly;
    }
}
