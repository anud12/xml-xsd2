import { memoize } from "../../util/memoize";
import { landGenerator } from "./landGenerator";
export const getLandModule = memoize((hostApi) => {
    const maxCount = hostApi.runtime.number.of(4);
    const landContainer = hostApi.runtime.setEntity({
        id: hostApi.runtime.string.of("lands"),
        entityListMap: { members: [] },
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
            let poolCount = context.getMutableEntityBy({
                filters: [(c) => c.id.isContainingExactly(hostApi.runtime.string.of("lands"))],
            }).map((container) => container.members("members").count())
                .exactlyOne()
                .orElse(hostApi.runtime.number.of(0));
            poolCount.isLessThan(maxCount).ifTrue((count) => {
                const land = landGenerator(hostApi).generateValue();
                context.createEntity(land);
                context.getMutableEntityBy({
                    filters: [(c) => c.id.isContainingExactly(hostApi.runtime.string.of("lands"))],
                }).forEach((container) => {
                    container.addMember("members", land);
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
        maxCount: maxCount,
        effect: tick,
        initialize,
    };
});
