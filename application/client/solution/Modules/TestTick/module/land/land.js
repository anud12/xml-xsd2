import { memoize } from "../../util/memoize";
export const getLandModule = memoize((hostApi) => {
    const costKey = "cost";
    // Monotonic id source for unique land ids (createEntity does not dedupe).
    const genKey = "landGen";
    const maxCount = hostApi.runtime.number.of(4);
    const one = hostApi.runtime.number.of(1);
    const zero = hostApi.runtime.number.of(0);
    const landNumberMap = {
        [costKey]: hostApi.runtime.number.of(10),
    };
    const landContainer = hostApi.runtime.setContainer({
        id: hostApi.runtime.string.of("lands"),
        entities: [],
    });
    // Keep the land pool topped up to exactly `maxCount`. Each tick, read the
    // live size of the "lands" container directly and, if it has fewer than
    // `maxCount` lands (e.g. one was bought away), create a fresh one to refill
    // it. The pool never exceeds the cap.
    const tick = hostApi.runtime.setEffect({
        name: hostApi.runtime.string.of("land-tick"),
        reoccurAfterMs: () => {
            return hostApi.runtime.maybe.of(hostApi.runtime.number.of(10));
        },
        apply: (context) => {
            // Read the pool's live size straight from the container's members.
            let poolCount = context.getMutableContainersBy({
                filters: [(c) => c.id.isContainingExactly(hostApi.runtime.string.of("lands"))],
            }).map((container) => container.entities.length())
                .exactlyOne()
                .orElse(hostApi.runtime.number.of(0));
            poolCount.isLessThan(maxCount).ifTrue((count) => {
                context.getMutableEntityBy({
                    filters: [(e) => e.id.isContainingExactly(hostApi.runtime.string.of("game"))],
                }).forEach((game) => {
                    const gen = game.number_map.get(genKey).orElse(zero);
                    const next = gen.sum(one);
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
                    }).forEach((container) => {
                        container.addEntity(land);
                    });
                    game.number_map.put(genKey, () => hostApi.runtime.maybe.of(next));
                });
                return hostApi.runtime.condition.of(true);
            }).or(() => hostApi.runtime.condition.of(false));
        },
    });
    const initialize = () => {
        hostApi.runtime.emitEvent(tick, {});
    };
    return {
        container: landContainer,
        genKey: genKey,
        maxCount: maxCount,
        effect: tick,
        initialize,
    };
});
