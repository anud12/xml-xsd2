import {HostApi} from "suite/types/HostApi";
import {EntityCreationArguments} from "../../../../../../suite/types/dataExpression/Entity";

export const interestModule = (hostApi:HostApi, ) => {
  
  const key = "property";
  
  const entityNumberMap:EntityCreationArguments["numberMap"] = {
    [key] : hostApi.runtime.number.of(100)
  } 
  
  const tick = hostApi.runtime.setEffect({
    name: hostApi.runtime.string.of("tick"),
    reoccurAfterMs: () => {
      return hostApi.runtime.maybe.of(hostApi.runtime.number.of(1));
    },
    apply: (context) => {
      context.getMutableEntityBy({
        filters: [(e) => e.id.isContainingExactly(hostApi.runtime.string.of("game"))],
      }).map((entity) => {
        entity.number_map.put(key, (current) =>
          current.map(v => v.isGreaterThan(hostApi.runtime.number.of(0))
            .ifTrue(v => v.sum(hostApi.runtime.number.of(-1)))
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
    initialize
  }
}