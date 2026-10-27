import { memoize } from "../../util/memoize";
export const getInventoryModule = memoize((hostApi) => {
    const container = hostApi.runtime.setContainer({
        id: hostApi.runtime.string.of("inventory"),
        entities: [],
    });
    const purchaseEffect = hostApi.runtime.setEffect({
        name: hostApi.runtime.string.of("inventory-purchase"),
        // The emit payload (carried by the action paths into `__effectInputs`)
        // arrives as `input`; returning the land id makes `apply` receive it as
        // `output`.
        prepare: (_context, input) => input?.entityId,
        apply: (context, landId) => {
            if (landId == null)
                return;
            const idStr = String(landId);
            // Deduct the land's cost from the game's value. `.exactlyOne()` yields the
            // land's cost when exactly one land matches (it always does by id), and
            // an absent value otherwise — so no captured mutable is needed.
            const cost = context.getMutableEntityBy({
                filters: [(e) => e.id.isContainingExactly(hostApi.runtime.string.of(idStr))],
            })
                .exactlyOne()
                .map((land) => land.number_map.get("cost").orElse(hostApi.runtime.number.of(0)))
                .orElse(hostApi.runtime.number.of(0));
            context.getMutableEntityBy({
                filters: [(e) => e.id.isContainingExactly(hostApi.runtime.string.of("game"))],
            }).map((game) => {
                game.number_map.put("value", (current) => current.map((v) => v.subtract(cost)));
            });
            // Move the land from the "lands" container into the inventory container.
            context.getMutableContainersBy({
                filters: [(c) => c.id.isContainingExactly(hostApi.runtime.string.of("lands"))],
            }).map((landsContainer) => {
                landsContainer.removeEntity(hostApi.runtime.string.of(idStr));
            });
            const landRef = hostApi.runtime.setEntity({ id: hostApi.runtime.string.of(idStr) });
            context.getMutableContainersBy({
                filters: [(c) => c.id.isContainingExactly(hostApi.runtime.string.of("inventory"))],
            }).map((invContainer) => {
                invContainer.addEntity(landRef);
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
