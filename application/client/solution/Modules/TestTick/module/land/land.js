import { memoize } from "../../util/memoize";
export const getLandModule = memoize((hostApi) => {
    const countKey = "landCount";
    const costKey = "cost";
    const maxCount = hostApi.runtime.number.of(4);
    const landNumberMap = {
        [costKey]: hostApi.runtime.number.of(10),
    };
    const landContainer = hostApi.runtime.setContainer({
        id: hostApi.runtime.string.of("lands"),
        entities: [],
    });
    const tick = hostApi.runtime.setEffect({
        name: hostApi.runtime.string.of("land-tick"),
        reoccurAfterMs: (context) => {
            let atLimit = hostApi.runtime.condition.of(false);
            context.getEntityBy({
                filters: [(e) => e.id.isContainingExactly(hostApi.runtime.string.of("game"))],
            }).map((game) => {
                atLimit = game.number_map.get(countKey)
                    .map((v) => v.isGreaterOrEqualTo(maxCount))
                    .orElse(hostApi.runtime.condition.of(false));
            });
            let interval = hostApi.runtime.maybe.of(hostApi.runtime.number.of(10));
            atLimit.ifTrue(() => {
                interval = hostApi.runtime.maybe.none();
                return hostApi.runtime.number.of(0);
            });
            return interval;
        },
        apply: (context) => {
            context.getMutableEntityBy({
                filters: [(e) => e.id.isContainingExactly(hostApi.runtime.string.of("game"))],
            }).map((game) => {
                const count = game.number_map.get(countKey).orElse(hostApi.runtime.number.of(0));
                const next = count.sum(hostApi.runtime.number.of(1));
                const land = hostApi.runtime.setEntity({
                    id: next.toStringExpression(),
                    textMap: {
                        name: hostApi.runtime.string.of("Land"),
                    },
                    numberMap: landNumberMap,
                });
                context.createEntity(land);
                context.getMutableContainersBy({
                    filters: [(c) => c.id.isContainingExactly(hostApi.runtime.string.of("lands"))],
                }).map((container) => {
                    container.addEntity(land);
                });
                game.number_map.put(countKey, () => hostApi.runtime.maybe.of(next));
            });
        },
    });
    const initialize = () => {
        hostApi.runtime.emitEvent(tick, {});
    };
    return {
        container: landContainer,
        countKey: countKey,
        maxCount: maxCount,
        effect: tick,
        initialize,
    };
});
