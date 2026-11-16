/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const { number, string } = hostApi.runtime;

  // 1. Register the drag type.
  const LAND = hostApi.ui.defineDragType({ name: "land", description: "A purchasable land parcel" });
  hostApi.runtime.log("___dragdrop visual defineDragType name=" + LAND.name + "___");

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

  // 5. Purchase effect (same as DragDropBuy): deduct the cost from the game's
  //    value and move the land from "lands" into "inventory".
  const purchaseEffect = hostApi.runtime.setEffect({
    name: string.of("inventory-purchase"),
    apply: (context) => {
      const landId = globalThis.__actionArgs && globalThis.__actionArgs.entityId;
      if (landId === undefined || landId === null) return;
      const idStr = String(landId);

      let cost = number.of(0);
      context.getMutableEntityBy({
        filters: [(e) => e.id.isContainingExactly(string.of(idStr))],
      }).forEach((entity) => {
        cost = entity.number_map.get("cost").orElse(number.of(0));
      });

      context.getMutableEntityBy({
        filters: [(e) => e.id.isContainingExactly(string.of("game"))],
      }).forEach((game) => {
        game.number_map.put("value", (current) =>
          current.map((v) => v.subtract(cost)));
      });

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

  // 6. Buy action (same as DragDropBuy). Defined at module setup time so the
  //    onDrop callback can capture it in a closure.
  const buyAction = hostApi.runtime.setAction({
    name: string.of("inventory-buy"),
    apply: (ctx) => {
      ctx.emitEffect(purchaseEffect, { entityId: ctx.args.entityId });
    },
  });

  // 7. Feedback highlight texture. A tiny solid-color PNG loaded from the
  //    archive; stretched to fill the inventory window while the pointer hovers
  //    over it during a drag (the drop-target feedback overlay).
  const highlight = hostApi.runtime.setAnimation(string.of("drop-highlight"), {
    frames: [{ sprite: hostApi.ui.getSpritePNG("drop_highlight.png") }],
    duration: number.of(1),
  });

  // 7b. Shared panel background: a solid OPAQUE texture so each window is a real
  //     surface. Without it the panels are transparent and the coreUI buttons
  //     (which sit at the top of the viewport, y 8-44) bleed through the window
  //     crops in headless test runs.
  const panelBg = hostApi.runtime.setAnimation(string.of("panel-bg"), {
    frames: [{ sprite: hostApi.ui.getSpritePNG("panel_bg.png") }],
    duration: number.of(1),
  });

  // 8. Drag source: a window whose panel is `draggable` (same as DragDropBuy).
  //    Positioned at y=150 — below the coreUI button row (y 8-44) — so the
  //    buttons are out of the captured window frame entirely.
  hostApi.ui.window("shop", {
    panel: {
      x: 20, y: 150, width: 300, height: 100,
      background: { animation: panelBg, fill: { width: "stretch", height: "stretch" } },
      draggable: {
        type: LAND,
        payload: { entityId: 1, sourceContainerId: "lands" },
      },
    },
  }, []);

  // 9. Drop target: a window whose panel is `droppable` WITH a `feedback`
  //    texture, so the drop-feedback overlay can be asserted visually. Uses a
  //    distinct onDrop marker so it doesn't cross-contaminate the DragDropBuy
  //    test.
  hostApi.ui.window("inventory", {
    panel: {
      x: 340, y: 150, width: 300, height: 100,
      background: { animation: panelBg, fill: { width: "stretch", height: "stretch" } },
      droppable: {
        accepts: [LAND],
        onDrop: (ctx) => {
          hostApi.runtime.log("___From module dragdrop visual onDrop fired line___");
          ctx.emitAction(buyAction, { entityId: ctx.item.entityId });
        },
        feedback: {
          animation: highlight,
          fill: { width: "stretch", height: "stretch" },
        },
      },
    },
  }, []);
}
