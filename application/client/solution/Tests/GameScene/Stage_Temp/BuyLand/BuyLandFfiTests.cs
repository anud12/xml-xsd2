using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_temp.BuyLand;

[TestSuite]
public class BuyLandFfiTests : Steps {
    [TestCategory("Stage_temp")]
    [TestCase]
    public void Given_land_for_sale_when_buy_action_emits_it_should_move_to_inventory_and_deduct_cost() {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .ProcessArchive();

        // Before purchase: the land is in "lands", the inventory is empty, and
        // the game holds 100 interest.
        var lands = ContainerInterop.GetContainerById("lands");
        Assertions.AssertThat(lands.Entities.Contains("1")).IsTrue();
        var inv = ContainerInterop.GetContainerById("inventory");
        Assertions.AssertThat(inv.Entities.Length).IsEqual(0);
        Assertions.AssertThat(double.Parse(
            RuntimeInterop.ReadEntityNumberValue("game", "value"))).IsEqual(100.0);

        // The action-args bridge only carries (key, f64) pairs, so the land id
        // must travel as a number. Publish the args into the persistent sim
        // context and queue the purchase effect, mirroring the real `.ui` click
        // flow (emitAction -> emitEffect -> RunIteration).
        RuntimeInterop.PublishActionArgsAndQueueEffect("inventory-buy", "inventory-purchase", "{\"entityId\":1}");
        RuntimeInterop.RunIteration();

        // The land moved from "lands" into "inventory".
        lands = ContainerInterop.GetContainerById("lands");
        Assertions.AssertThat(lands.Entities.Contains("1")).IsFalse();
        inv = ContainerInterop.GetContainerById("inventory");
        Assertions.AssertThat(inv.Entities.Length).IsEqual(1);
        Assertions.AssertThat(inv.Entities.Contains("1")).IsTrue();

        // The land's cost (10) was deducted from the game's value.
        Assertions.AssertThat(double.Parse(
            RuntimeInterop.ReadEntityNumberValue("game", "value"))).IsEqual(90.0);
    }
}
