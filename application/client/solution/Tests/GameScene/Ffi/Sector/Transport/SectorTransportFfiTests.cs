using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;

namespace GdUnit4.Examples.Basics.Setup.Test.Ffi.Sector.Transport;

[TestSuite]
public class SectorTransportFfiTests : Steps
{
    [TestCase]
    public void Given_a_sector_module_it_should_expose_the_grid_over_ffi()
    {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .ProcessArchive();

        var ids = SectorInterop.GetSectorGridIds();
        Assertions.AssertThat(ids.Contains("cave")).IsTrue();

        var grid = SectorInterop.GetSectorGridById("cave");
        Assertions.AssertThat(grid).IsNotNull();
        Assertions.AssertThat(grid!.Id).IsEqual("cave");
        Assertions.AssertThat(grid.Cells.Count).IsEqual(1);

        var missing = SectorInterop.GetSectorGridById("does-not-exist");
        Assertions.AssertThat(missing).IsNull();
    }
}
