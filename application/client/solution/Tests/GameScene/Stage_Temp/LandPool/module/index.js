/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const { number, string } = hostApi.runtime;

  // Faithful copy of Modules/TestTickSource land-tick logic: the "lands"
  // container is topped up to maxCount (4) by a reoccurring tick that reads
  // the pool's live size and creates a fresh land (monotonic id) when below the
  // cap.
  const genKey = "landGen";
  const maxCount = number.of(4);
  const one = number.of(1);
  const zero = number.of(0);
  const landNumberMap = { cost: number.of(10) };

  hostApi.runtime.setEntity({
    id: string.of("game"),
    numberMap: { [genKey]: number.of(0) },
  });

  hostApi.runtime.setContainer({ id: string.of("lands"), entities: [] });

  const tick = hostApi.runtime.setEffect({
    name: string.of("land-tick"),
    reoccurAfterMs: () => hostApi.runtime.maybe.of(number.of(10)),
    apply: (context) => {
      let poolCount = context.getMutableContainersBy({
        filters: [(c) => c.id.isContainingExactly(string.of("lands"))],
      }).map((container) => container.entities.length())
        .exactlyOne()
        .orElse(number.of(0));

      poolCount.isLessThan(maxCount).ifTrue((count) => {
        context.getMutableEntityBy({
          filters: [(e) => e.id.isContainingExactly(string.of("game"))],
        }).forEach((game) => {
          const gen = game.number_map.get(genKey).orElse(zero);
          const next = gen.sum(one);
          const land = hostApi.runtime.setEntity({
            id: next.toStringExpression(),
            textMap: { name: string.of("Land") },
            numberMap: landNumberMap,
          });
          context.createEntity(land);
          context.getMutableContainersBy({
            filters: [(c) => c.id.isContainingExactly(string.of("lands"))],
          }).forEach((container) => container.addEntity(land));
          game.number_map.put(genKey, () => hostApi.runtime.maybe.of(next));
        });
        return hostApi.runtime.condition.of(true);
      }).or(() => hostApi.runtime.condition.of(false));
    },
  });

  hostApi.runtime.emitEvent(tick, {});
}
