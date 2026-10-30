import {HostApi} from "module_types/HostApi";
import {EntityCreationArguments} from "module_types/dataExpression/Entity";
import {memoize} from "../../util/memoize";

export const getInterestModule = memoize((hostApi:HostApi, ) => {

  const key = "property";

  const max = hostApi.runtime.number.of(1000);

  const entityNumberMap:EntityCreationArguments["numberMap"] = {
    [key] : max,
  }

  const tick = hostApi.runtime.setEffect({
    name: hostApi.runtime.string.of("tick"),
    reoccurAfterMs: () => {
      return hostApi.runtime.maybe.of(hostApi.runtime.number.of(1));
    },
    apply: (context) => {
      context.getMutableEntityBy({
        filters: [(e) => e.id.isContainingExactly(hostApi.runtime.string.of("game"))],
      }).forEach((entity) => {
        entity.number_map.put(key, (current) =>
          current.map(v => v.isGreaterThan(hostApi.runtime.number.of(0))
            .ifTrue(v => v.sum(hostApi.runtime.number.random(hostApi.runtime.number.of(-10), hostApi.runtime.number.of(-1))))
            .or(()=> hostApi.runtime.number.of(0))
          ))
      });
    },
  });

  const initialize = () => {
    hostApi.runtime.emitEvent(tick, {});
  }

  return {
    propertyKey: key,
    entityNumberMap: entityNumberMap,
    effect: tick,
    maxValue: max,
    initialize
  }
})