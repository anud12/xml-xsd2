/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const { number, string } = hostApi.runtime;

  // 1. Feedback highlight texture: a tiny solid-color PNG loaded from the
  //    archive; stretched to fill the inventory window while the pointer hovers
  //    over it during a drag (the drop-target feedback overlay). Declared up
  //    front because the "land" drag type's `feedback` references it.
  const highlight = hostApi.runtime.setAnimation(string.of("drop-highlight"), {
    frames: [{ sprite: hostApi.ui.getSpritePNG("drop_highlight.png") }],
    duration: number.of(1),
  });

  // 2. Shared panel background: a solid OPAQUE texture so each window is a real
  //    surface. Without it the panels are transparent and the coreUI buttons
  //    (which sit at the top of the viewport, y 8-44) bleed through the window
  //    crops in headless test runs.
  const panelBg = hostApi.runtime.setAnimation(string.of("panel-bg"), {
    frames: [{ sprite: hostApi.ui.getSpritePNG("panel_bg.png") }],
    duration: number.of(1),
  });

  // 2b. Drag ghost: a DISTINCT vivid-magenta square (the C# default is a faint
  //     translucent white 24x24). The ghost is the only drag visual and it
  //     follows the pointer, so a bold color makes its movement off the source
  //     (and onto the destination) evident in the captured frames.
  const ghostSprite = hostApi.runtime.setAnimation(string.of("drag-ghost"), {
    frames: [{ sprite: hostApi.ui.getSpritePNG("drag_ghost.png") }],
    duration: number.of(1),
  });

  // Font for the drop-feedback text label (the same setFont mechanism used by
  // ui.text / ui.field). The label is drawn centered over the feedback overlay
  // and is visible only while the onDrag/onHover feedback is active.
  const feedbackFont = hostApi.ui.setFont({ file: "fonts/Ubuntu-Regular.ttf", size: 22, color: "#FFFFFF" });

  // 3. The game holds value that is spent to buy land.
  hostApi.runtime.setEntity({
    id: string.of("game"),
    numberMap: { value: number.of(100), landCount: number.of(0) },
  });

  // 4. A single land for sale, id "1", costing 10.
  const land = hostApi.runtime.setEntity({
    id: string.of("1"),
    numberMap: { cost: number.of(10) },
    stringMap: { name: string.of("Land") },
  });

  // 5. Containers.
  hostApi.runtime.setEntity({
    id: string.of("lands"),
    entityListMap: { members: [land] },
  });
  hostApi.runtime.setEntity({
    id: string.of("inventory"),
    entityListMap: { members: [] },
  });

  // 6. Purchase effect (same as DragDropBuy): deduct the cost from the game's
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

      context.getMutableEntityBy({
        filters: [(c) => c.id.isContainingExactly(string.of("lands"))],
      }).forEach((landsContainer) => {
        landsContainer.removeMember("members", string.of(idStr));
      });
      context.getMutableEntityBy({
        filters: [(c) => c.id.isContainingExactly(string.of("inventory"))],
      }).forEach((invContainer) => {
        invContainer.addMember("members", land);
      });
    },
  });

  // 7. Buy action (same as DragDropBuy). Defined before the drag type so the
  //    drag type's onDrop can capture it in a closure.
  const buyAction = hostApi.runtime.setAction({
    name: string.of("inventory-buy"),
    apply: (ctx) => {
      ctx.emitEffect(purchaseEffect, { entityId: ctx.args.entityId });
    },
  });

  // 8. Register the drag type — the single home for the "land" drop semantics:
  //    the onDrop callback (fires the buy action) and the hover feedback
  //    texture. Any droppable that accepts LAND inherits both, so the drop
  //    target below only needs to declare `accepts`.
  const LAND = hostApi.ui.dragAndDropChannel({
    name: "land",
    description: "A purchasable land parcel",
    onDrop: (ctx) => {
      hostApi.runtime.log("___From module dragdrop visual onDrop fired line___");
      ctx.emitAction(buyAction, { entityId: ctx.item.entityId });
    },
    feedback: {
      onDrag: { animation: highlight, fill: { width: "stretch", height: "stretch" }, text: { value: string.of("DROP"), font: feedbackFont } },
      onHover: { animation: highlight, fill: { width: "stretch", height: "stretch" }, text: { value: string.of("HERE"), font: feedbackFont } },
    },
  });
  hostApi.runtime.log("___dragdrop visual dragAndDropChannel name=" + LAND.name + "___");

  // 9. Drag source: a window whose panel is `draggable`. `LAND.draggable({ payload, ...options })`
  //    produces the draggable config — it emits this type and tags the per-source
  //    identity (which land + source container). Positioned at y=150 — below the
  //    coreUI button row (y 8-44) — so the buttons are out of the captured frame.
  hostApi.ui.window("shop", {
    panel: {
      x: 20, y: 150, width: 300, height: 100,
      background: { animation: panelBg, fill: { width: "stretch", height: "stretch" } },
      draggable: LAND.draggable({
        payload: { entityId: 1, sourceContainerId: "lands" },
        ghost: { animation: ghostSprite, fill: { width: "fit", height: "fit" } },
      }),
    },
  }, []);

  // 10. Drop target: a window whose panel is `droppable`, accepting LAND.
  //     `LAND.droppable()` produces the droppable config — the onDrop + feedback
  //     live on the drag type (step 8), so no per-target options are needed.
  hostApi.ui.window("inventory", {
    panel: {
      x: 340, y: 150, width: 300, height: 100,
      background: { animation: panelBg, fill: { width: "stretch", height: "stretch" } },
      droppable: LAND.droppable(),
    },
  }, []);
}
