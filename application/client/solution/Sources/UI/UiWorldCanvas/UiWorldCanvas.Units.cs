using Godot;
using NewGameProject.UI;
using RuntimeInterop = NewGameProject.Runtime.RuntimeInterop;

namespace GdUnit4.Examples.Basics.Setup.Sources.UI;

/// Unit rendering: every entity carrying a numberMap x/y is drawn as an 8px
/// dot in its room (the textMap "room" field, or the active room).
public partial class UiWorldCanvas
{
    void DrawUnits(Node2D world)
    {
        if (_data == null) return;
        var active = FindRoom(_activeRoomId)
            ?? (_data.Rooms.Count > 0 ? _data.Rooms[0] : null);
        if (active == null) return;

        foreach (var entityId in EnumerateEntityIds())
        {
            if (!TryParseNumber(RuntimeInterop.ReadEntityNumberValue(entityId, "x"), out var x)
                || !TryParseNumber(RuntimeInterop.ReadEntityNumberValue(entityId, "y"), out var y))
                continue;
            var roomId = RuntimeInterop.ReadEntityTextValue(entityId, "room");
            var room = string.IsNullOrEmpty(roomId)
                ? active
                : FindRoom(roomId) ?? active;
            _roomColors.TryGetValue(room.Id, out var rc);
            var color = new Color(rc.R * 0.5f, rc.G * 0.5f, rc.B * 0.5f, 1f);
            var pos = WorldData.RoomToWorld(room, new Godot.Vector2(x, y));
            world.AddChild(BuildDot(pos, UnitDotRadius, color, new Color(0.1f, 0.1f, 0.1f, 1f)));
        }
    }

    /// v1: entities are enumerated through the runtime's entity store (panel
    /// ids stand in for entity ids in this build; the numberMap/textMap FFI
    /// lookups are identical to a real entity store).
    static IEnumerable<string> EnumerateEntityIds()
    {
        foreach (var id in RuntimeInterop.GetPanelIds())
            yield return id;
    }

    static bool TryParseNumber(string? s, out float value)
    {
        return float.TryParse(s, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out value);
    }
}
