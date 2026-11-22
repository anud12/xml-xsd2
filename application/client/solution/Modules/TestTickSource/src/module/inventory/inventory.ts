import {HostApi} from "module_types/HostApi";
import {memoize} from "../../util/memoize";
import {landTypeKey} from "../land/landGenerator";

export const getInventoryModule = memoize((hostApi: HostApi) => {

  const container = hostApi.runtime.setContainer({
    id: hostApi.runtime.string.of("inventory"),
    entities: [],
  });

  const purchaseEffect = hostApi.runtime.setEffect({
    name: hostApi.runtime.string.of("inventory-purchase"),
    prepare: (_context, input: { entityId: string } | undefined) => input?.entityId,
    apply: (context, landId) => {
      if (typeof landId !== "string" || !landId) return;
      const id = hostApi.runtime.string.of(landId);
      const zero = hostApi.runtime.number.of(0);
      const one = hostApi.runtime.number.of(1);

      // The id selects the specific item; the number map identifies its type.
      context.getMutableContainersBy({
        filters: [(c) => c.id.isContainingExactly(hostApi.runtime.string.of("lands"))],
      }).forEach((landsContainer) => {
        landsContainer.entities.forEach((member) => {
          member.id.isContainingExactly(id).ifTrue(() => {
            context.getMutableEntityBy({
              filters: [
                (entity) => entity.id.isContainingExactly(id),
                (entity) => entity.number_map.get(landTypeKey).orElse(zero).isEqualTo(one),
              ],
            }).forEach((land) => {
              land.number_map.get("cost").ifPresent((cost) => {
                context.getMutableEntityBy({
                  filters: [(entity) => entity.id.isContainingExactly(hostApi.runtime.string.of("game"))],
                }).forEach((game) => {
                  game.number_map.get("value").ifPresent((value) => {
                    value.isGreaterOrEqualTo(cost).ifTrue(() => {
                      context.getMutableContainersBy({
                        filters: [(container) => container.id.isContainingExactly(hostApi.runtime.string.of("inventory"))],
                      }).forEach((inventory) => {
                        game.number_map.put("value", (current) => current.map((v) => v.subtract(cost)));
                        landsContainer.removeEntity(id);
                        inventory.addEntity(land);
                      });
                      return hostApi.runtime.condition.of(true);
                    });
                  });
                });
              });
            });
            return hostApi.runtime.condition.of(true);
          });
        });
      });
    },
  });

  const buy = hostApi.runtime.setAction({
    name: hostApi.runtime.string.of("inventory-buy"),
    apply: (ctx) => {
      ctx.emitEffect(purchaseEffect, { entityId: ctx.args.entityId });
    },
  });

  return {
    container,
    purchaseEffect,
    buy,
  };
});
