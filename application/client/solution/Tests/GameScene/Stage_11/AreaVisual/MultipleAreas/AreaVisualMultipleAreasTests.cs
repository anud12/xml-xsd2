using System.Text.Json;
using Godot;
using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;
using NewGameProject.UI;
using V2 = Godot.Vector2;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_11.AreaVisual.MultipleAreas;

[TestSuite]
public class AreaVisualMultipleAreasTests : Steps
{
    [TestCategory("Stage_11")]
    [TestCase]
    [RequireGodotRuntime]
    public async Task Given_container_view_selecting_two_areas_the_item_should_stamp_and_render_both_polygons()
    {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .EnsureDllAccessible()
            .ProcessArchive();

        var scene = await AttachUiScene();

        // The view is a 700x500 window over a 100x100 container: cell_w = 7,
        // cell_h = 5. The room item is stamped view-local x/y (70, 50).
        scene.AssertPanelThat("world")
            .ViewportIsSize(700, 500)
            .HasChildPanelNamed("room", c => c.IsPositionEqual(70, 50));

        var ptr = RuntimeInterop.FetchUiState();
        Assertions.AssertThat(ptr != IntPtr.Zero).IsTrue();
        var nodes = UiSlab.ReadNodes(ptr);
        RuntimeInterop.FreeUiState(ptr);

        var room = nodes.FirstOrDefault(n => n.Id == "room");
        Assertions.AssertThat(room != null).IsTrue();
        var polys = ReadPolygons(room!.OptionsJson);
        Assertions.AssertThat(polys).IsNotNull();
        // Two areas were selected, so exactly two polygons are stamped.
        Assertions.AssertThat(polys!.Count).IsEqual(2);

        // room's world pos is (10, 10) -> view-local offset (70, 50); the view
        // is 700x500 over a 100x100 container (cell_w = 7, cell_h = 5). Each
        // local point (px, py) maps to view-local (px*7+70, py*5+50).
        //   floor: [0,0],[30,0],[30,40],[0,40] ->
        //          (70,50),(280,50),(280,250),(70,250)
        //   roof:  [0,40],[30,40],[30,80],[0,80] ->
        //          (70,250),(280,250),(280,450),(70,450)
        // Winding is normalized by make_area, so compare corners as unordered
        // sets. Each polygon is identified by its stamped color.
        var floor = polys.First(p => p.OutlineColor == Rgba(1, 0, 0, 1));
        var floorCorners = CornerSet(floor.Points);
        var expectedFloor = new HashSet<string> { "70,50", "280,50", "280,250", "70,250" };
        foreach (var c in expectedFloor)
            Assertions.AssertThat(floorCorners.Contains(c)).IsTrue();
        Assertions.AssertThat(floorCorners.Count).IsEqual(4);

        var roof = polys.First(p => p.OutlineColor == Rgba(0, 0, 1, 1));
        var roofCorners = CornerSet(roof.Points);
        var expectedRoof = new HashSet<string> { "70,250", "280,250", "280,450", "70,450" };
        foreach (var c in expectedRoof)
            Assertions.AssertThat(roofCorners.Contains(c)).IsTrue();
        Assertions.AssertThat(roofCorners.Count).IsEqual(4);

        // Each polygon carries the view's per-area style (distinct per area).
        Assertions.AssertThat(floor.BodyColor).IsEqual(Rgba(0, 1, 0, 1));
        Assertions.AssertThat(floor.Thickness).IsEqual(3f);
        Assertions.AssertThat(roof.BodyColor).IsEqual(Rgba(0, 1, 1, 1));
        Assertions.AssertThat(roof.Thickness).IsEqual(4f);

        // Both polygons render over the view: floor is a red-outlined
        // green-filled box at view-local (70,50)..(280,250), roof is a
        // blue-outlined cyan-filled box at (70,250)..(280,450).
        scene.AssertPanelThat("world").ViewportMatches("expected.png");

        // Sample specific pixels to prove both outlines actually rendered with
        // their distinct colors. Map view-local (vx, vy) to image coords using
        // the window's global rect (the same crop ViewportMatches uses).
        var world = scene.Window("world");
        var full = world.GetViewport().GetTexture().GetImage();
        var gr = world.GetGlobalRect();
        var ox = (int)gr.Position.X;
        var oy = (int)gr.Position.Y;
        Godot.Color At(int vx, int vy) => full.GetPixel(ox + vx, oy + vy);

        // Floor body interior (view 150,120): translucent green fill.
        var floorBody = At(150, 120);
        Assertions.AssertThat(floorBody.G > 0.4f && floorBody.R < 0.4f && floorBody.B < 0.4f).IsTrue();
        // Floor top outline edge (view y=50, between x=70..280): red.
        var floorEdge = At(150, 50);
        Assertions.AssertThat(floorEdge.R > 0.6f && floorEdge.G < 0.4f && floorEdge.B < 0.4f).IsTrue();
        // Roof body interior (view 150,300): translucent cyan fill (green+blue).
        var roofBody = At(150, 300);
        Assertions.AssertThat(roofBody.G > 0.4f && roofBody.B > 0.4f && roofBody.R < 0.4f).IsTrue();
        // Roof top outline edge (view y=250): blue.
        var roofEdge = At(150, 250);
        Assertions.AssertThat(roofEdge.B > 0.6f && roofEdge.R < 0.4f && roofEdge.G < 0.4f).IsTrue();
        // Between the two boxes is impossible (they share the y=250 edge), so
        // sample just above floor's top edge (view y=40): background, not green.
        var aboveFloor = At(150, 40);
        Assertions.AssertThat(aboveFloor.G < 0.4f).IsTrue();
        full.Dispose();
    }

    class Poly
    {
        public List<V2> Points = new();
        public Color OutlineColor;
        public Color BodyColor;
        public float Thickness;
    }

    static Color Rgba(float r, float g, float b, float a) => new(r, g, b, a);

    // Reads the stamped options.areaOutline.polygons from a node's
    // re-serialized options JSON, returning each polygon's points + style.
    static List<Poly>? ReadPolygons(string optionsJson)
    {
        using var doc = JsonDocument.Parse(optionsJson);
        if (!doc.RootElement.TryGetProperty("areaOutline", out var ao)
            || ao.ValueKind != JsonValueKind.Object
            || !ao.TryGetProperty("polygons", out var polys)
            || polys.ValueKind != JsonValueKind.Array)
            return null;
        var result = new List<Poly>();
        foreach (var poly in polys.EnumerateArray())
        {
            if (poly.ValueKind != JsonValueKind.Object
                || !poly.TryGetProperty("points", out var pts)
                || pts.ValueKind != JsonValueKind.Array)
                continue;
            var p = new Poly();
            foreach (var pt in pts.EnumerateArray())
            {
                var a = pt.EnumerateArray().ToArray();
                if (a.Length < 2) continue;
                p.Points.Add(new V2((float)a[0].GetDouble(), (float)a[1].GetDouble()));
            }
            if (p.Points.Count < 3) continue;
            p.OutlineColor = ParseRgba(poly, "color", new Color(1f, 0f, 0f, 1f));
            p.BodyColor = ParseRgba(poly, "bodyColor", new Color(0f, 1f, 0f, 1f));
            if (poly.TryGetProperty("thickness", out var t) && t.ValueKind == JsonValueKind.Number)
                p.Thickness = (float)t.GetDouble();
            result.Add(p);
        }
        return result;
    }

    static Color ParseRgba(JsonElement poly, string key, Color fallback)
    {
        if (!poly.TryGetProperty(key, out var arr) || arr.ValueKind != JsonValueKind.Array)
            return fallback;
        var c = fallback;
        var comps = arr.EnumerateArray().ToArray();
        if (comps.Length > 0) c.R = (float)comps[0].GetDouble();
        if (comps.Length > 1) c.G = (float)comps[1].GetDouble();
        if (comps.Length > 2) c.B = (float)comps[2].GetDouble();
        if (comps.Length > 3) c.A = (float)comps[3].GetDouble();
        return c;
    }

    static HashSet<string> CornerSet(List<V2> points)
    {
        var set = new HashSet<string>();
        foreach (var p in points)
            set.Add($"{Math.Round(p.X, 3)},{Math.Round(p.Y, 3)}");
        return set;
    }
}
