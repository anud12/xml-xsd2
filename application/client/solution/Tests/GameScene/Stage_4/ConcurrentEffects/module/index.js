/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const { string, number, maybe } = hostApi.runtime;

  hostApi.runtime.setEntity({
    id: string.of("game"),
    numberMap: { counter: number.of(0) },
  });

  // Effect A: adds 1 every tick, stops after 5 applications.
  let aCount = 0;
  const effectA = hostApi.runtime.setEffect({
    name: string.of("effect-a"),
    reoccurAfterMs: () => {
      if (aCount >= 5) return maybe.none();
      return maybe.of(number.of(1));
    },
    apply: (context) => {
      aCount++;
      context.getMutableEntityBy({
        filters: [(e) => e.id.isContainingExactly(string.of("game"))],
      }).map((entity) => {
        entity.number_map.put("counter", (current) =>
          current.map(v => v.sum(number.of(1)))
        );
      });
    },
  });

  // Effect B: adds 10 every tick, stops after 5 applications.
  let bCount = 0;
  const effectB = hostApi.runtime.setEffect({
    name: string.of("effect-b"),
    reoccurAfterMs: () => {
      if (bCount >= 5) return maybe.none();
      return maybe.of(number.of(1));
    },
    apply: (context) => {
      bCount++;
      context.getMutableEntityBy({
        filters: [(e) => e.id.isContainingExactly(string.of("game"))],
      }).map((entity) => {
        entity.number_map.put("counter", (current) =>
          current.map(v => v.sum(number.of(10)))
        );
      });
    },
  });

  hostApi.runtime.emitEvent(effectA, {});
  hostApi.runtime.emitEvent(effectB, {});
};
