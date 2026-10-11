using System.Text.Json;
using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;
using NewGameProject.UI;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_11.AreaVisual.StampedOutline;

[TestSuite]
public class AreaVisualStampedOutlineTests : Steps
{
    [TestCategory("Stage_11")]
    [TestCase]
    public void Given_container_view_the_item_options_should_carry_the_stamped_area_outline()
    {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .EnsureDllAccessible()
            .ProcessArchive();

        var ptr = RuntimeInterop.FetchUiState();
        Assertions.AssertThat(ptr != IntPtr.Zero).IsTrue();
        var nodes = UiSlab.ReadNodes(ptr);
        RuntimeInterop.FreeUiState(ptr);

        // room declares a 5-point local area (a 60x80 box with the top-right
        // corner cut: [0,0],[40,0],[60,20],[60,80],[0,80]); the view is 700x500
        // over a 100x100 container (cell_w = 7, cell_h = 5). room's world pos
        // (10, 10) -> view-local offset (70, 50). Each local point (px, py)
        // maps to view-local (px*7+70, py*5+50):
        //   [0,0]  -> (70, 50)
        //   [40,0] -> (350, 50)
        //   [60,20]-> (490, 150)
        //   [60,80]-> (490, 450)
        //   [0,80] -> (70, 450)
        // Winding is normalized by make_area, so compare the corners as an
        // unordered set.
        var room = nodes.FirstOrDefault(n => n.Id == "room");
        Assertions.AssertThat(room != null).IsTrue();
        var roomCorners = OutlineCorners(room!.OptionsJson);
        Assertions.AssertThat(roomCorners).IsNotNull();
        Assertions.AssertThat(roomCorners!.Count).IsEqual(5);
        var expectedRoom = new HashSet<string> { "70,50", "350,50", "490,150", "490,450", "70,450" };
        foreach (var c in expectedRoom)
            Assertions.AssertThat(roomCorners.Contains(c)).IsTrue();

        // crate has no declared area: a 1x1 fallback rect at its view-local
        // origin. crate's world pos is (40, 40) -> view-local (280, 200).
        var crate = nodes.FirstOrDefault(n => n.Id == "crate");
        Assertions.AssertThat(crate != null).IsTrue();
        var crateCorners = OutlineCorners(crate!.OptionsJson);
        Assertions.AssertThat(crateCorners).IsNotNull();
        Assertions.AssertThat(crateCorners!.Count).IsEqual(4);
        var expectedCrate = new HashSet<string> { "280,200", "281,200", "281,201", "280,201" };
        foreach (var c in expectedCrate)
            Assertions.AssertThat(crateCorners.Contains(c)).IsTrue();
    }

    // Reads the stamped options.areaOutline.polygons from a node's re-serialized
    // options JSON and returns the view-local corner set of the first polygon
    // as "x,y" strings (rounded to whole units; the stamp uses exact integer
    // view positions).
    static HashSet<string>? OutlineCorners(string optionsJson)
    {
        using var doc = JsonDocument.Parse(optionsJson);
        if (!doc.RootElement.TryGetProperty("areaOutline", out var ao)
            || ao.ValueKind != JsonValueKind.Object
            || !ao.TryGetProperty("polygons", out var polys)
            || polys.ValueKind != JsonValueKind.Array)
            return null;
        var set = new HashSet<string>();
        foreach (var poly in polys.EnumerateArray())
        {
            if (poly.ValueKind != JsonValueKind.Object
                || !poly.TryGetProperty("points", out var pts)
                || pts.ValueKind != JsonValueKind.Array)
                continue;
            foreach (var p in pts.EnumerateArray())
            {
                var a = p.EnumerateArray().ToArray();
                if (a.Length < 2) continue;
                var x = Math.Round(a[0].GetDouble(), 3);
                var y = Math.Round(a[1].GetDouble(), 3);
                set.Add($"{x},{y}");
            }
        }
        return set;
    }
}
