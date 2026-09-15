using System.Text.Json;
using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;
using NewGameProject.UI;

namespace GdUnit4.Examples.Basics.Setup.Test.Ffi.Ui;

/// <summary>
/// FFI smoke test for the binary-slab UI state path: the module archive is
/// processed by the Rust runtime, and the C# side reads the node store and
/// delta back through the <see cref="UiSlab"/> slab reader (no JSON at the
/// FFI boundary).
/// </summary>
[TestSuite]
public class UiSlabSmokeTests : Steps
{
    [TestCase]
    public void Given_module_archive_the_slab_snapshot_should_round_trip_nodes()
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

        Assertions.AssertThat(nodes.Count).IsEqual(3);

        var win = nodes.FirstOrDefault(n => n.Id == "slab-win");
        Assertions.AssertThat(win != null).IsTrue();
        Assertions.AssertThat(win!.Kind).IsEqual(UiNodeKind.Window);
        using (var winDoc = JsonDocument.Parse(win.OptionsJson))
        {
            Assertions.AssertThat(winDoc.RootElement.GetProperty("width").GetDouble()).IsEqual(200.0);
            Assertions.AssertThat(winDoc.RootElement.GetProperty("height").GetDouble()).IsEqual(100.0);
        }
        Assertions.AssertThat(win.Children.Contains("slab-text")).IsTrue();
        Assertions.AssertThat(win.Children.Contains("slab-field")).IsTrue();

        var text = nodes.FirstOrDefault(n => n.Id == "slab-text");
        Assertions.AssertThat(text != null).IsTrue();
        Assertions.AssertThat(text!.Kind).IsEqual(UiNodeKind.Text);
        Assertions.AssertThat(text.Value).IsEqual("slab");

        var field = nodes.FirstOrDefault(n => n.Id == "slab-field");
        Assertions.AssertThat(field != null).IsTrue();
        Assertions.AssertThat(field!.Kind).IsEqual(UiNodeKind.Field);
        Assertions.AssertThat(field.BindingJson.Length > 0).IsTrue();
        using var binding = JsonDocument.Parse(field.BindingJson);
        Assertions.AssertThat(binding.RootElement.GetProperty("entity").GetString()).IsEqual("hero");
        Assertions.AssertThat(binding.RootElement.GetProperty("map").GetString()).IsEqual("number");
        Assertions.AssertThat(binding.RootElement.GetProperty("name").GetString()).IsEqual("hp");
        Assertions.AssertThat(binding.RootElement.GetProperty("fallback").GetString()).IsEqual("n/a");
    }

    [TestCase]
    public void Given_module_archive_the_slab_delta_should_carry_add_ops()
    {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .EnsureDllAccessible()
            .ProcessArchive();

        var ptr = RuntimeInterop.FetchUiDelta();
        Assertions.AssertThat(ptr != IntPtr.Zero).IsTrue();
        var delta = UiSlab.ReadDelta(ptr);
        RuntimeInterop.FreeUiDelta(ptr);
        Assertions.AssertThat(delta != null).IsTrue();
        var adds = delta!.Ops.Where(o => o.Op == "add").Select(o => o.Node.Id).ToList();
        Assertions.AssertThat(adds.Contains("slab-win")).IsTrue();
        Assertions.AssertThat(adds.Contains("slab-text")).IsTrue();
        Assertions.AssertThat(adds.Contains("slab-field")).IsTrue();
    }
}
