/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const { number, string } = hostApi.runtime;

  // The game holds value that is spent to buy land.
  hostApi.runtime.setEntity({
    id: string.of("game"),
    numberMap: {
      value: number.of(100),
      landCount: number.of(0),
    },
  });

  // A single land for sale, id "1", costing 10.
  const land = hostApi.runtime.setEntity({
    id: string.of("1"),
    textMap: { name: string.of("Land") },
    numberMap: { cost: number.of(10) },
  });

  hostApi.runtime.setContainer({ id: string.of("lands"), entities: [land] });

  hostApi.runtime.setContainer({ id: string.of("inventory"), entities: [] });

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
      }).map((entity) => {
        cost = entity.number_map.get("cost").orElse(number.of(0));
      });

      // Deduct the cost from the game's value.
      context.getMutableEntityBy({
        filters: [(e) => e.id.isContainingExactly(string.of("game"))],
      }).map((game) => {
        game.number_map.put("value", (current) =>
          current.map((v) => v.subtract(cost)));
      });

      // Move the land from "lands" into "inventory".
      context.getMutableContainersBy({
        filters: [(c) => c.id.isContainingExactly(string.of("lands"))],
      }).map((landsContainer) => {
        landsContainer.removeEntity(string.of(idStr));
      });
      context.getMutableContainersBy({
        filters: [(c) => c.id.isContainingExactly(string.of("inventory"))],
      }).map((invContainer) => {
        invContainer.addEntity(land);
      });
    },
  });

  hostApi.runtime.setAction({
    name: string.of("inventory-buy"),
    apply: (ctx) => {
      ctx.emitEffect(purchaseEffect, { entityId: ctx.args.entityId });
    },
  });
}
