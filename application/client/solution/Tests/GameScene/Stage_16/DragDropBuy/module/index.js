/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const { number, string } = hostApi.runtime;

  // 1. Register the drag type. The handle carries `.name === "land"` and
  //    serializes (toJSON) to the plain string, so it flows through the
  //    panel options resolution chain unchanged and arrives at the C#
  //    snapshot as `"land"`.
  const LAND = hostApi.ui.defineDragType({ name: "land", description: "A purchasable land parcel" });
  hostApi.runtime.log("___dragdrop defineDragType name=" + LAND.name + "___");

  // 2. The game holds value that is spent to buy land.
  hostApi.runtime.setEntity({
    id: string.of("game"),
    numberMap: {
      value: number.of(100),
      landCount: number.of(0),
    },
  });

  // 3. A single land for sale, id "1", costing 10.
  const land = hostApi.runtime.setEntity({
    id: string.of("1"),
    textMap: { name: string.of("Land") },
    numberMap: { cost: number.of(10) },
  });

  // 4. Containers.
  hostApi.runtime.setContainer({ id: string.of("lands"), entities: [land] });
  hostApi.runtime.setContainer({ id: string.of("inventory"), entities: [] });

  // 5. Purchase effect (same as BuyLand): deduct the cost from the game's
  //    value and move the land from "lands" into "inventory".
  const purchaseEffect = hostApi.runtime.setEffect({
    name: string.of("inventory-purchase"),
    apply: (context) => {
      // The land id travels as a number through the action-args bridge
      // (ctx.args is persisted on the sim context as __actionArgs, so it is
      // visible from the effect's apply phase even though the effect runner
      // fires effects by name only).
      const landId = globalThis.__actionArgs && globalThis.__actionArgs.entityId;
      if (landId === undefined || landId === null) return;
      const idStr = String(landId);

      // Read the land's cost (the land entity's numberMap, not a re-declaration).
      let cost = number.of(0);
      context.getMutableEntityBy({
        filters: [(e) => e.id.isContainingExactly(string.of(idStr))],
      }).forEach((entity) => {
        cost = entity.number_map.get("cost").orElse(number.of(0));
      });

      // Deduct the cost from the game's value.
      context.getMutableEntityBy({
        filters: [(e) => e.id.isContainingExactly(string.of("game"))],
      }).forEach((game) => {
        game.number_map.put("value", (current) =>
          current.map((v) => v.subtract(cost)));
      });

      // Move the land from "lands" into "inventory".
      context.getMutableContainersBy({
        filters: [(c) => c.id.isContainingExactly(string.of("lands"))],
      }).forEach((landsContainer) => {
        landsContainer.removeEntity(string.of(idStr));
      });
      context.getMutableContainersBy({
        filters: [(c) => c.id.isContainingExactly(string.of("inventory"))],
      }).forEach((invContainer) => {
        invContainer.addEntity(land);
      });
    },
  });

  // 6. Buy action (same as BuyLand). Defined once at module setup time so the
  //    onDrop callback can capture it in a closure — setAction must NOT be
  //    called inside the callback (the callback is stashed by __prepDragDrop
  //    and invoked later via the runtime_ui_js_drop FFI).
  const buyAction = hostApi.runtime.setAction({
    name: string.of("inventory-buy"),
    apply: (ctx) => {
      ctx.emitEffect(purchaseEffect, { entityId: ctx.args.entityId });
    },
  });

  // 7. Drag source: a window whose panel is `draggable`. The `type`
  //    (DragTypeReference) and `payload` pass through the options resolution
  //    chain unchanged — __prepDragDrop only rewrites onDragStart/onDragEnd
  //    (absent here), so the panel is forwarded to the original factory as-is.
  hostApi.ui.window("shop", {
    panel: {
      x: 20, y: 20, width: 300, height: 100,
      draggable: {
        type: LAND,
        payload: { entityId: 1, sourceContainerId: "lands" },
      },
    },
  }, []);

  // 8. Drop target: a window whose panel is `droppable`. The onDrop callback
  //    is stashed by __prepDragDrop into globalThis.__uiDropHandlers["inventory"]
  //    (replaced by the "__jsHandler" marker in the serialized options). When
  //    the C# renderer calls RuntimeInterop.UiJsDrop, the FFI builds a
  //    UiDropContext and invokes the stashed callback; ctx.emitAction pushes
  //    to __uiClickEmissions, which the FFI dispatches via
  //    runtime_debug_simulate_action_args — the same path as clicks.
  hostApi.ui.window("inventory", {
    panel: {
      x: 340, y: 20, width: 300, height: 100,
      droppable: {
        accepts: [LAND],
        onDrop: (ctx) => {
          hostApi.runtime.log("___From module dragdrop onDrop fired line___");
          ctx.emitAction(buyAction, { entityId: ctx.item.entityId });
        },
      },
    },
  }, []);
}
