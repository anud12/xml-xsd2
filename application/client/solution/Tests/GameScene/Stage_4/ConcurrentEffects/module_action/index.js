/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const { string, number, maybe } = hostApi.runtime;

  hostApi.runtime.setEntity({
    id: string.of("game"),
    numberMap: { counter: number.of(0) },
  });

  // Reoccurring effect: adds 1 every tick, stops after 5 applications.
  let rCount = 0;
  const recurEffect = hostApi.runtime.setEffect({
    name: string.of("recur"),
    reoccurAfterMs: () => {
      if (rCount >= 5) return maybe.none();
      return maybe.of(number.of(1));
    },
    apply: (context) => {
      rCount++;
      context.getMutableEntityBy({
        filters: [(e) => e.id.isContainingExactly(string.of("game"))],
      }).map((entity) => {
        entity.number_map.put("counter", (current) =>
          current.map(v => v.sum(number.of(1)))
        );
      });
    },
  });

  // Action-triggered effect: adds 10 each time it fires.
  const actionEffect = hostApi.runtime.setEffect({
    name: string.of("action-effect"),
    apply: (context) => {
      context.getMutableEntityBy({
        filters: [(e) => e.id.isContainingExactly(string.of("game"))],
      }).map((entity) => {
        entity.number_map.put("counter", (current) =>
          current.map(v => v.sum(number.of(10)))
        );
      });
    },
  });

  // Action that triggers the action-effect.
  hostApi.runtime.setAction({
    name: string.of("action-effect"),
    apply: (context) => {
      context.emitEffect(actionEffect, {});
    },
  });

  hostApi.runtime.emitEvent(recurEffect, {});
};
