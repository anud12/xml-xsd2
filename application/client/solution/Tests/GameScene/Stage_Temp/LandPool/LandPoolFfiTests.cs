using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_temp.LandPool;

[TestSuite]
public class LandPoolFfiTests : Steps {
    [TestCategory("Stage_temp")]
    [TestCase]
    public void Given_empty_land_pool_when_land_tick_runs_it_should_top_up_to_max_count() {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .ProcessArchive();

        // The land tick reoccurs every 10ms and tops the pool up to 4. Advance
        // in small steps (each iteration fires the reoccurring tick once) until
        // the pool is full.
        for (int i = 0; i < 10; i++) {
            RuntimeInterop.RunIteration(10);
        }

        var lands = ContainerInterop.GetContainerById("lands");
        var landGen = RuntimeInterop.ReadEntityNumberValue("game", "landGen");
        Console.WriteLine($"DEBUG LandPool lands count={lands.Entities.Length} ids=[{string.Join(",", lands.Entities)}] landGen='{landGen}'");

        Assertions.AssertThat(lands.Entities.Length)
            .OverrideFailureMessage($"land pool did not top up to 4 (got {lands.Entities.Length}: [{string.Join(",", lands.Entities)}], landGen='{landGen}')")
            .IsEqual(4);
    }
}
