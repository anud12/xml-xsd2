import { memoize } from "../../util/memoize";
/**
 * A "value" resource used to buy lands. It starts at 0 and grows by 1 each
 * turn, capped at 100.
 */
export const getValueModule = memoize((hostApi) => {
    const key = "value";
    const start = hostApi.runtime.number.of(0);
    const max = hostApi.runtime.number.of(100);
    const step = hostApi.runtime.number.of(1);
    const entityNumberMap = {
        [key]: start,
    };
    const tick = hostApi.runtime.setEffect({
        name: hostApi.runtime.string.of("value-tick"),
        reoccurAfterMs: () => {
            return hostApi.runtime.maybe.of(hostApi.runtime.number.of(1));
        },
        apply: (context) => {
            context.getMutableEntityBy({
                filters: [(e) => e.id.isContainingExactly(hostApi.runtime.string.of("game"))],
            }).forEach((entity) => {
                entity.number_map.put(key, (current) => current.map(v => v.isGreaterOrEqualTo(max)
                    .ifTrue(() => v)
                    .or(() => v.sum(step))));
            });
        },
    });
    const initialize = () => {
        hostApi.runtime.emitEvent(tick, {});
    };
    return {
        valueKey: key,
        entityNumberMap,
        effect: tick,
        startValue: start,
        maxValue: max,
        initialize,
    };
});
