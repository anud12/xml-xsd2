using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_16.DragDropBuy;

[TestSuite]
public class DragDropBuyTests : Steps {
    [TestCategory("Stage_16")]
    [TestCase]
    public async Task Given_land_for_sale_when_dropped_on_inventory_it_should_buy() {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .ProcessArchive();

        // Before the drop: the land is in "lands", the inventory is empty,
        // and the game holds 100.
        var lands = EntityInterop.GetEntityById("lands")!;
        var inv = EntityInterop.GetEntityById("inventory")!;

        Assertions.AssertThat(lands.Members("members").Contains("1")).IsTrue();
        Assertions.AssertThat(inv.Members("members").Length).IsEqual(0);
        Assertions.AssertThat(double.Parse(
            RuntimeInterop.ReadEntityNumberValue("game", "value"))).IsEqual(100.0);

        // Simulate a drop: call the FFI drop handler directly (the same path
        // the C# renderer uses when the user drops the drag on the inventory
        // window). This fires the stashed onDrop callback (registered by
        // __prepDragDrop during ProcessArchive), which logs its marker and
        // emits the inventory-buy action with the dropped entity id. The
        // emission is dispatched synchronously through
        // runtime_debug_simulate_action_args — the identical path a click's
        // emitAction takes — so the action fires and the effect applies.
        RuntimeInterop.UiJsDrop(
            nodeId: "inventory",
            dropType: "land",
            entityId: "1",
            sourceContainerId: "lands",
            hasCursor: false,
            cursorX: 0, cursorY: 0, cellX: -1, cellY: -1,
            dataJson: "{}");

        RuntimeInterop.RunIteration();

        // The land moved from "lands" into "inventory".
        lands = EntityInterop.GetEntityById("lands")!;
        Assertions.AssertThat(lands.Members("members").Contains("1")).IsFalse();
        inv = EntityInterop.GetEntityById("inventory")!;
        Assertions.AssertThat(inv.Members("members").Length).IsEqual(1);
        Assertions.AssertThat(inv.Members("members").Contains("1")).IsTrue();

        // The land's cost (10) was deducted from the game's value.
        Assertions.AssertThat(double.Parse(
            RuntimeInterop.ReadEntityNumberValue("game", "value"))).IsEqual(90.0);

        // The onDrop callback logged its marker (proves the FFI drop path
        // invoked the stashed JS handler, not a fallback).
        AssertRuntimeOutputContains("___From module dragdrop onDrop fired line___");

        // The body is fully synchronous; the trailing await keeps the async
        // signature (required by GdUnit4 for [TestCase] methods) warning-free.
        await Task.CompletedTask;
    }
}
